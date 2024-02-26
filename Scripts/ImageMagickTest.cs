// css_ref System.Memory.dll, Magick.NET.Core.dll
using System.Collections.Generic;
using ImageMagick;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    public class ImageMagickTest : BaseScript {
        enum Id { InputImage};
        public ImageMagickTest() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputImage, ObjectType = typeof (DB.Image), Label = "Input image",
                    Mandatory = true
                }
            };
            Description = "Processes an image using ImageMagick.NET";
        }

 
        protected override IEnumerable<DB.Image> GetInputImages() {
            return new List<DB.Image>() { GetObject<DB.Image>(Id.InputImage) };
        }

        protected override bool ProcessImage(DB.Image image) {
            var ok = true;
            try {
                // Uses sRGB.icm, eps/pdf produce better result when you set this before loading.
                var settings = new MagickReadSettings {
                    ColorSpace = ColorSpace.sRGB
                };

                // Create empty image
                using var eps = new MagickImage();


                // Read image from file
                using var png = new MagickImage(image.FileName);

                // Will use the CMYK profile if the image does not contain a color profile.
                // The second profile will transform the colorspace from CMYK to RGB
                png.TransformColorSpace(ColorProfile.USWebCoatedSWOP, ColorProfile.SRGB);

                // Save image as png
                var outputFile = "C:\\Tmp\\Snakeware.png";
                png.Write(outputFile);
                Display(outputFile);
            }
            catch {
                ok = false;
            }
            return ok;
        }
    }
}
