// css_ref System.Memory.dll, Magick.NET.Core.dll
using System;
using System.Collections.Generic;
using System.IO;
using ImageMagick;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    class WaterMark : BaseScript {
        enum Id { InputMode, WaterMark, OutputFolder };
        private MagickImage _waterMark;
        private DirectoryInfo _outputFolder;
        public WaterMark() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode", 
                    DefaultValue = InputImages_e.CurrentFolderImages,
                    Description = "What images are to be processed", Mandatory = true
                },
                new Parameter {
                    Id = Id.WaterMark, ObjectType = typeof (FileInfo), Label = "Watermark file",
                    Description = "Image file containing the watermark.", Mandatory = true
                },
                new Parameter {
                    Id = Id.OutputFolder, ObjectType = typeof (DirectoryInfo), Label = "Output folder",
                    DefaultValue = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)), // Your default value
                    Description = "Folder for resulting watermarked images.", Mandatory = true
                }
            };
            Description = "Select which images to apply the watermark to.";
        }

       
        protected override void PrepareExecution() {
            base.PrepareExecution();
            try {
                var waterMarkFile = GetObject<FileInfo>(Id.WaterMark);
                _waterMark = new MagickImage(GetObject<FileInfo>(Id.WaterMark));
                _outputFolder = GetObject<DirectoryInfo>(Id.OutputFolder);
            }
            catch {
                _waterMark = null;
            }
        }
        protected override IEnumerable<DB.Image> GetInputImages() {
            return GetInputImages(GetObject<InputImages_e>(Id.InputMode));
        }

        protected override bool ProcessImage(DB.Image inputImage) {
            var ok = true;
            try {
                // Read image that needs a watermark
                using var image = new MagickImage(inputImage.FileName);

                // Draw the watermark in the bottom right corner
                image.Composite(_waterMark, Gravity.Southeast, CompositeOperator.Over);

                // Optionally make the watermark more transparent
                _waterMark.Evaluate(Channels.Alpha, EvaluateOperator.Divide, 4);

                // Or draw the watermark at a specific location
                image.Composite(_waterMark, 200, 50, CompositeOperator.Over);

                // Save the result
                var resultFile = Path.Combine(_outputFolder.FullName, Path.GetFileName(inputImage.FileName));
                image.Write(resultFile);
                LogView.AppendText($"Made: {resultFile}");
            }
            catch {
                ok = false;
            }
            return ok;
        }
        protected override void EndExecution() {
            base.EndExecution();
            if (_waterMark != null) {
                _waterMark.Dispose();
            }
        }

    }
}
