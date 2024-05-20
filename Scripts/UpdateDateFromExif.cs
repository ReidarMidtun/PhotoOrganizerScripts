using System;
using System.Collections.Generic;
using System.Linq;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    class UpdateDateFromExif : BaseScript {
        enum Id { InputMode };
        private List<DB.Image>? _updatedImages;

        public UpdateDateFromExif() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode", 
                    DefaultValue = InputImages_e.CurrentFolderImages,
                    Description = "What images are to be processed", Mandatory = true
                }
            };
            Description = "Select which images to apply the process to. If a date is found in the Exif file properties, the date of the image is updated.";
        }

        protected override IEnumerable<DB.Image> GetInputImages() {
            return GetInputImages(GetObject<InputImages_e>(Id.InputMode));
        }

        protected override void PrepareExecution() {
            base.PrepareExecution(); // Caches the input images before starting the processing of images
            _updatedImages = new List<DB.Image>();
        }

        protected override bool ProcessImage(DB.Image image) {
            var ok = true;
            try {
                var date = DB.Image.GetDateFromExif(image.FileName);
                if (date != null) {
                    if (image.DateTime == null || date.Value.CompareTo(image.DateTime) != 0) {
                        image.DateTime = date;
                        _updatedImages.Add(image);
                    }
                }
            }
            catch {
                ok = false;
            }
            return ok;
        }

        protected override void EndExecution() {
            if (_updatedImages != null && _updatedImages.Any()) {
                DB.Update(_updatedImages);
                LogView.AppendText($"Updated {_updatedImages.Count} images.");
            } 
        }    

    }
}
