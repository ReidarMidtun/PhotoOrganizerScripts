// css_ref System.Drawing.Common.dll, System.Memory.dll, Magick.NET.Core.dll
using ImageMagick;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhotoOrganizerScripts.Filters {
    class Portrait : BaseScript, IFilter {
        enum Id { FaceRatio }
        public Portrait() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.FaceRatio, ObjectType = typeof (float), Label = "Face ratio",
                    Description = "The minimum ratio of the face width compared to the image width", DefaultValue = 0.2f }
            };

            Description = "Checks if the maximum face width divided by the total image width is greather than the given face ratio.";
        }
        public void Initialize() => FaceRatio = GetObject<float>(Id.FaceRatio);
        public bool IsValid(DB.Image image) {
            var ok = image.Faces != null && image.Faces.Any();
            if (ok) {
                var img = new MagickImageInfo(new FileInfo(image.FileName));
                var faceWidth = image.Faces.Max(f => f.LTRB[2] - f.LTRB[0]);
                return ((float)faceWidth / img.Width) >= FaceRatio;
            }
            return ok;
        }
        string IFilter.Name { get; set; }
        private float FaceRatio { get; set; } = 0.2f;
    }
}
