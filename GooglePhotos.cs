// css_ref System.Net.Primitives.dll,  System.Net.Http
// css_ref Microsoft.Extensions.Logging.dll, Microsoft.Extensions.Logging.Abstractions.dll, Microsoft.Extensions.Options.dll
// css_ref CasCap.Apis.GooglePhotos.dll, CasCap.Common.Net.dll

// These are needed due to compiling outside VS (automatically added in VS)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using CasCap.Models;
using CasCap.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
using System.Diagnostics;
using System.Net;


namespace PhotoOrganizerScripts {
    public class GooglePhotos : BaseScript {
        enum Id { User, ClientId, ClientSecret, PlayList };

        public GooglePhotos() {
            InputObjects = new List<Parameter>();
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.User, ObjectType = typeof (string), Label = "User", DefaultValue = "reidardmidtun@gmail.com",
                    Description = "Input a user", Mandatory = true
                },
                new Parameter {
                    Id = Id.ClientId, ObjectType = typeof (string), Label = "Client Id", DefaultValue = "858389142974-ams85ups1mivtsmr6bqjhlfa16g69jpk.apps.googleusercontent.com",
                    Description = "Input a color ", Mandatory = true
                },
                new Parameter {
                    Id = Id.ClientSecret, ObjectType = typeof (string), Label = "Client Secret", DefaultValue = "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",
                    Description = "Input a color ", Mandatory = true
                },
                new Parameter {
                    Id = Id.PlayList, ObjectType = typeof (string), Label = "Playlist", 
                    Description = "Input a color ", Mandatory = true
                }
            };
            Description = "Upload a play list to google photo";
        }

        // This function is called after input is verified
        protected override object Execute() {
            Test();
            return null;
        }


        public static async void Test() {
            string _user = "reidardmidtun@gmail.com";
            string _clientId = "858389142974-ams85ups1mivtsmr6bqjhlfa16g69jpk.apps.googleusercontent.com";
            string _clientSecret = "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";
            const string _testFolder = "c:/Tmp/GooglePhotos/";

            if (new[] { _user, _clientId, _clientSecret }.Any(p => string.IsNullOrWhiteSpace(p))) {
                LogView.AppendText("Please populate authentication details to continue...");
                Debugger.Break();
                return;
            }
            if (!Directory.Exists(_testFolder)) {
                LogView.AppendText($"Cannot find folder '{_testFolder}'");
                Debugger.Break();
                return;
            }

            //1) new-up some basic logging (if using appsettings.json you could load logging configuration from there)
            //var configuration = new ConfigurationBuilder().Build();
            var loggerFactory = LoggerFactory.Create(builder => {
                //builder.AddConfiguration(configuration.GetSection("Logging")).AddDebug().AddConsole();
            });
            var logger = loggerFactory.CreateLogger<GooglePhotosService>();

            //2) create a configuration object
            var options = new GooglePhotosOptions {
                User = _user,
                ClientId = _clientId,
                ClientSecret = _clientSecret,
                FileDataStoreFullPathOverride = _testFolder,
                Scopes = new[] { GooglePhotosScope.Access, GooglePhotosScope.Sharing },//Access+Sharing == full access
            };

            //3) (Optional) display local OAuth 2.0 JSON file(s);
            var path = options.FileDataStoreFullPathOverride is null ? options.FileDataStoreFullPathDefault : options.FileDataStoreFullPathOverride;
            LogView.AppendText($"{nameof(options.FileDataStoreFullPathOverride)}:\t{path}");
            var files = Directory.GetFiles(path);
            if (files.Length == 0)
                LogView.AppendText($"\t- n/a this is probably the first time we have authenticated...");
            else {
                LogView.AppendText($"Files;");
                foreach (var file in files)
                    LogView.AppendText($"\t- {Path.GetFileName(file)}");
            }
            
            //4) create a single HttpClient which will be pooled and re-used by GooglePhotosService
            var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            var client = new HttpClient(handler) { BaseAddress = new Uri(options.BaseAddress) };

            //5) new-up the GooglePhotosService passing in the previous references (in lieu of dependency injection)
            var _googlePhotosSvc = new GooglePhotosService(logger, Options.Create(options), client);

            //6) log-in
            if (!await _googlePhotosSvc.LoginAsync()) throw new Exception($"login failed!");

            //get existing/create new album
            var albumTitle = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-{Guid.NewGuid()}";//make-up a random title
            var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle);
            if (album is null) throw new Exception("album creation failed!");
            LogView.AppendText($"{nameof(album)} '{album.title}' id is '{album.id}'");

            //upload single media item and assign to album
            var mediaItem = await _googlePhotosSvc.UploadSingle($"{_testFolder}IMG_0732.JPG", album.id);
            if (mediaItem is null) throw new Exception("media item upload failed!");
            LogView.AppendText($"{nameof(mediaItem)} '{mediaItem.mediaItem.filename}' id is '{mediaItem.mediaItem.id}'");

            //retrieve all media items in the album
            var i = 0;
            await foreach (var item in _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id)) {
                i++;
                LogView.AppendText($"{i}\t{item.filename}\t{item.mediaMetadata.width}x{item.mediaMetadata.height}");
            }
            if (i == 0) throw new Exception("retrieve media items by album id failed!");
        }
    }
}
