// css_ref System.Net.Primitives.dll, System.Drawing.Common.dll, System.Net.Http.Json.dll
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CodeProject.AI.API.Common;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    class PortraitFilter : BaseScript {
        enum Id { InputMode, OutputFolder, Strength };
   
        private DirectoryInfo _ouputFolder;
        private float _strength;
        private int _NImages = 0;

        public PortraitFilter() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode",
                    Description = "What images are to be processed", 
                    DefaultValue = InputImages_e.SelectedImagesInThumbnailPanel, Mandatory = true
                },
                new Parameter {
                    Id = Id.Strength, ObjectType = typeof (float), Label = "Strength",
                    Description = "A value between 0 and 1.0.", DefaultValue = 0.5f, Mandatory = false
                },
                new Parameter {
                    Id = Id.OutputFolder, ObjectType = typeof (DirectoryInfo), Label = "Output folder",
                    Description = "Path for resulting images, if not specified the resulting images are displayed.", Mandatory = false
                }
            };
            Description = "Blurs the background of a portrait. The script is using the CodeProject.AI Portrait Filter sub module to process the images. Select images to process. If the output folder is specified, the resulting images are save to the specified folder. The five first images are automatically displayed.";
        }

        protected override IEnumerable<DB.Image> GetInputImages() {
            return GetInputImages(GetObject<InputImages_e>(Id.InputMode));
        }

        protected override void PrepareExecution() {
            base.PrepareExecution();
            _ouputFolder = GetObject<DirectoryInfo>(Id.OutputFolder);
            _strength = GetObject<float>(Id.Strength);

            _NImages = 0;
        }

        public class ProcessImageResponse : SuccessResponse {
            public string? filtered_image { get; set; }
        }


        protected override bool ProcessImage(DB.Image image) {
            var ok = true;
            try {
                var fileInfo = new FileInfo(image.FileName);
                var request = new MultipartFormDataContent();
                var image_data = fileInfo.OpenRead();
                var content = new StreamContent(image_data);
                var strength = new StringContent(_strength.ToString());
                request.Add(content, "image", image.FileName);
                request.Add(strength, "strength");
                var client = new HttpClient {
                    BaseAddress = new Uri($"http://localhost:{32168}/v1/"),
                    Timeout = TimeSpan.FromSeconds(300)
                };
                string resultingImage = BlurBackground(client, request).GetAwaiter().GetResult();
                if (resultingImage != null) {
                    var outputFile = _ouputFolder != null ? Path.Combine(_ouputFolder.FullName, Path.GetFileName(image.FileName)) : null;
                    resultingImage = SaveResult(resultingImage, outputFile);
                    if (_ouputFolder == null) {
                        if (_NImages < 5) {
                            Display(resultingImage);
                            _NImages++;
                        } else if (_NImages == 5) {
                            LogView.AppendText("Displaying only the 5 first.");
                            LogView.AppendText("Specify an output folder if you want to process more.");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex) {
                LogView.AppendText($"Portrait Filter failed.\n{ex.Message}");
                ok = false;
            }
            return ok;
        }

  
        public System.Drawing.Image Base64ToImage(string base64String) {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length)) {
                var  image = System.Drawing.Image.FromStream(ms, true);
                return image;
            }
        }

        private string SaveResult(string base64String, string fileName=null) {
            var image = Base64ToImage(base64String);
            var result = fileName ?? Path.GetTempFileName();
            image.Save(result, System.Drawing.Imaging.ImageFormat.Jpeg);
            return result;
        }

        private async Task<string> BlurBackground(HttpClient client, MultipartFormDataContent request) {
            using var httpResponse = await client.PostAsync("image/portraitfilter", request);
            httpResponse.EnsureSuccessStatusCode();
            var response = await httpResponse.Content.ReadFromJsonAsync<ProcessImageResponse>();
            if (response != null && response.filtered_image != null) {
                return response.filtered_image;
            }
            return null;
        }


    }
}
