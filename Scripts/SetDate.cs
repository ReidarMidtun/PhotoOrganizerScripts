using System;
using System.Collections.Generic;
using System.Linq;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    class SetDate : BaseScript {
        enum Id { InputMode, Date };
        private DateTime? _date;
        private List<DB.Image>? _updatedImages;

        public SetDate() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode", 
                    DefaultValue = InputImages_e.CurrentFolderImages,
                    Description = "What images are to be processed", Mandatory = true
                },
                new Parameter {
                    Id = Id.Date, ObjectType = typeof (DateTime), Label = "A date",
                    DefaultValue = DateTime.Now, // Your default value
                    Description = "The date to the images.", Mandatory = true
                }
            };
            Description = "Select which images to aply the process to. If the image date is not set, the date is set to the value given by the user.";
        }

        protected override IEnumerable<DB.Image> GetInputImages() {
            return GetInputImages(GetObject<InputImages_e>(Id.InputMode));
        }

        protected override void PrepareExecution() {
            base.PrepareExecution(); // Caches the input images before starting the processing of images
            _date = GetObject<DateTime>(Id.Date);
            _updatedImages = new List<DB.Image>();
        }

        protected override bool ProcessImage(DB.Image image) {
            var ok = true;
            try {
                MyImageProcessing(image, _date);
            }
            catch {
                ok = false;
            }
            return ok;
        }

        protected override void EndExecution() {
            if (_updatedImages != null && _updatedImages.Any()) {
                DB.Update(_updatedImages);
                LogView.AppendText($"Updated {_updatedImages.Count} with date {_date.Value.ToShortDateString()}.");
            } 
        }

        private void MyImageProcessing(DB.Image image, DateTime? date) {
            if (date != null && _updatedImages != null && IsUpdateNeeded(image)) {
                image.DateTime = date;
                _updatedImages.Add(image);
            }
        }

        // If the date is not set for the image, true is returned
        private bool IsUpdateNeeded(DB.Image image) {
            return image.DateTime == null; // Tailor to your need! 
        }

    }
}
