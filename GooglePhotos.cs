﻿// css_ref System.Net.Primitives.dll, System.Linq.Expressions.dll
// https://github.com/f2calv/CasCap.Apis.GooglePhotos
//

// These are needed due to compiling outside VS (automatically added by VS)
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
using System.Net;
 
namespace PhotoOrganizerScripts {
    public class GooglePhotos : BaseScript {
        enum Id { User, ClientId, ClientSecret, PlayList };

        public GooglePhotos() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.User, ObjectType = typeof (string), Label = "User", DefaultValue = "reidardmidtun@gmail.com",
                    Description = "Input a user", Mandatory = true
                },
                new Parameter {
                    Id = Id.ClientId, ObjectType = typeof (string), Label = "Client Id", DefaultValue = "858389142974-ams85ups1mivtsmr6bqjhlfa16g69jpk.apps.googleusercontent.com",
                    Description = "Input a client id ", Mandatory = true
                },
                new Parameter {
                    Id = Id.ClientSecret, ObjectType = typeof (string), Label = "Client Secret", DefaultValue = "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",
                    Description = "Input a client secret ", Mandatory = true
                },
                new Parameter {
                    Id = Id.PlayList, ObjectType = typeof (string), Label = "Playlist", 
                    Description = "Input a playlist name", Mandatory = true
                }
            };
            Description = "Synchronizes a playlist and a Google Photos album of the same name. The process is done in the background. Visit https://console.cloud.google.com/apis/dashboard to get the authentication credentials, see https://github.com/f2calv/CasCap.Apis.GooglePhotos for a recipe.";
        }

        // This function is called after input is verified
        protected override object Execute() {
            var user = GetObject<string>(Id.User);
            var clientId = GetObject<string>(Id.ClientId);
            var clientSecret = GetObject<string>(Id.ClientSecret);
            var playListName = GetObject<string>(Id.PlayList);
            if (Main.CanStartBackgroundProcess) {
                Main.BackgroundTask = Task.Run(() => Synchronize(user, clientId, clientSecret, playListName, Main.Progress, ProgressIndicator.Token));
            }
            else {
                LogView.AppendText("Wait until running process is finished or cancelled.");
            }
            return null;
        }

        private HashSet<string> GetPlaylistFileNames(string playListName) {
            var playList = DB.GetPlayListCollection(DB.DataBase).FindOne(p => p.Name == playListName);
            return playList != null ? playList.FileNames.ToHashSet() : new HashSet<string>();
        }

        private async Task Synchronize(string user, string clientId,  string clientSecret, string playListName, ProgressIndicator progressIndicator, CancellationToken token) {

            progressIndicator.Text = "Uploading images to Google Photos";

            var service = await GetServiceAsync(user, clientId, clientSecret);
            var album = await service.GetOrCreateAlbumAsync(playListName);
            if (album is null) throw new Exception("album creation failed!");
            var pathNames = GetPlaylistFileNames(playListName);
            var fileNames = pathNames.Select(pn => Path.GetFileName(pn));
            var toRemoveIds = new HashSet<string>();
            var alreadyUploaded = new HashSet<string>();

            await foreach (var item in service.GetMediaItemsByAlbumAsync(album.id)) {
                if (item is null) continue;
                alreadyUploaded.Add(item.filename);
                if (!fileNames.Contains(item.filename)) { // If already uploaded not contained in the playlist => remove it
                    toRemoveIds.Add(item.id);
                }
            }

            if (toRemoveIds.Any()) {
                var ok = await service.RemoveMediaItemsFromAlbumAsync(album.id, toRemoveIds.ToArray());
                if (!ok) {
                    LogView.AppendText($"Failed to remove items from the album.");
                }
            }
            progressIndicator.Maximum = pathNames.Count();
            try {
                foreach (var pathName in pathNames) {
                    var fileName = Path.GetFileName(pathName);
                    if (!alreadyUploaded.Contains(fileName)) { // Upload if not already done
                        var mediaItem = await service.UploadSingle(pathName, album.id);
                        if (mediaItem != null) {
                            LogView.AppendText($"Uploaded: {mediaItem.mediaItem.filename}");
                        } else {
                            LogView.AppendText($"Uploaded failed of: {fileName}");
                        }
                    }
                    token.ThrowIfCancellationRequested();
                    progressIndicator.PerformStep();
                } 
                var message = $"Sucessfully uploaded {playListName} to google photos";
                progressIndicator.Text = message;
                progressIndicator.Reset();
                LogView.AppendText(message);
            }
            catch (Exception ex) {
                progressIndicator.Text = "Upload process cancelled.";
                progressIndicator.Reset();
            }
        } 

        private static async Task<GooglePhotosService> GetServiceAsync(string user, string clientId, string clientSecret) {
            //1) new-up some basic logging (if using appsettings.json you could load logging configuration from there)
            //var configuration = new ConfigurationBuilder().Build();
            var loggerFactory = LoggerFactory.Create(builder => {
                //builder.AddConfiguration(configuration.GetSection("Logging")).AddDebug().AddConsole();
            });
            var logger = loggerFactory.CreateLogger<GooglePhotosService>();

            //2) create a configuration object
            var options = new GooglePhotosOptions {
                User = user,
                ClientId = clientId,
                ClientSecret = clientSecret,
                // FileDataStoreFullPathOverride = _testFolder,
                Scopes = new[] { GooglePhotosScope.Access, GooglePhotosScope.Sharing },//Access+Sharing == full access
            };
            //4) create a single HttpClient which will be pooled and re-used by GooglePhotosService
            var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            var client = new HttpClient(handler) { BaseAddress = new Uri(options.BaseAddress) };

            //5) new-up the GooglePhotosService passing in the previous references (in lieu of dependency injection)
            var googlePhotosSvc = new GooglePhotosService(logger, Options.Create(options), client);

            //6) log-in
            if (!await googlePhotosSvc.LoginAsync()) throw new Exception($"login failed!");

            return googlePhotosSvc;
        }
         
    }
}
