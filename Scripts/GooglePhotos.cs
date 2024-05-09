// css_ref System.Net.Primitives.dll, System.Linq.Expressions.dll
// https://github.com/f2calv/CasCap.Apis.GooglePhotos
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
                    Id = Id.User, ObjectType = typeof (string), Label = "User", 
                    Description = "Input a user", Mandatory = true
                },
                new Parameter {
                    Id = Id.ClientId, ObjectType = typeof (string), Label = "Client Id", 
                    Description = "Input a client id ", Mandatory = true
                },
                new Parameter {
                    Id = Id.ClientSecret, ObjectType = typeof (string), Label = "Client Secret",
                    Description = "Input a client secret ", Mandatory = true
                },
                new Parameter {
                    Id = Id.PlayList, ObjectType = typeof (DB.PlayList), Label = "Playlist", 
                    Description = "Drop a playlist to select", Mandatory = true
                }
            };
            Description = "Synchronizes a playlist and a Google Photos album of the same name. The process is done in the background. Visit https://console.cloud.google.com/apis/dashboard to get the authentication credentials, see https://github.com/f2calv/CasCap.Apis.GooglePhotos for a recipe.";
        }

        // This function is called after input is verified
        protected override async Task Execute() {
            var user = GetObject<string>(Id.User);
            var clientId = GetObject<string>(Id.ClientId);
            var clientSecret = GetObject<string>(Id.ClientSecret);
            var playList = GetObject<DB.PlayList>(Id.PlayList);
            if (Main.CanStartBackgroundProcess) {
                Main.BackgroundTask = Task.Run(() => Synchronize(user, clientId, clientSecret, playList.Name, Main.Progress, ProgressIndicator.Token));
            }
            else {
                LogView.AppendText("Wait until running process is finished or cancelled.");
            }
            return;
        }

        private HashSet<string> GetPlaylistFileNames(string playListName) {
            var playList = DB.GetPlayListCollection(DB.DataBase).FindOne(p => p.Name == playListName);
            return playList != null ? playList.GetFileNames().ToHashSet() : new HashSet<string>();
        }

        private async Task Synchronize(string user, string clientId, string clientSecret, string playListName, ProgressIndicator progressIndicator, CancellationToken token) {
            try {
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
                var toUpload = pathNames.Where(pn => !alreadyUploaded.Contains(Path.GetFileName(pn))).ToList();
                progressIndicator.Maximum = toUpload.Count();
                foreach (var pathName in toUpload) {
                    var mediaItem = await service.UploadSingle(pathName, album.id);
                    if (mediaItem != null) {
                        LogView.AppendText($"Uploaded: {mediaItem.mediaItem.filename}");
                    } else {
                        LogView.AppendText($"Uploaded failed for: {pathName}");
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
                progressIndicator.Text = "Upload process failed or cancelled.";
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
