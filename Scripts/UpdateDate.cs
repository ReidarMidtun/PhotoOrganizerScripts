using System.Collections.Generic;
using System.Linq;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    class UpdateDate : BaseScript {
        enum Id { InputMode, Mode, Year, Month, Day, Hour, Minute, Second };
        private List<DB.Image>? _updatedImages;
        private enum Mode_e {
            Add,
            Subtract
        }
        public UpdateDate() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode", 
                    DefaultValue = InputImages_e.CurrentFolderImages,
                    Description = "What images are to be processed", Mandatory = true
                },
                new Parameter {
                    Id = Id.Mode, ObjectType = typeof (Mode_e), Label = "Update mode",
                    DefaultValue = Mode_e.Add,
                    Description = "Add or subtract from current date", Mandatory = true
                },
                new Parameter {
                    Id = Id.Year, ObjectType = typeof (int), Label = "Years",
                    DefaultValue = 0,
                    Description = "Add/subtract years", Mandatory = true
                },
                new Parameter {
                    Id = Id.Month, ObjectType = typeof (int), Label = "Months",
                    DefaultValue = 0,
                    Description = "Add/subtract months", Mandatory = true
                },
                new Parameter {
                    Id = Id.Day, ObjectType = typeof (int), Label = "Days",
                    DefaultValue = 0,
                    Description = "Add/subtract days", Mandatory = true
                },
                new Parameter {
                    Id = Id.Hour, ObjectType = typeof (int), Label = "Hours",
                    DefaultValue = 0,
                    Description = "Add/subtract hours", Mandatory = true
                },
                new Parameter {
                    Id = Id.Minute, ObjectType = typeof (int), Label = "Minutes",
                    DefaultValue = 0,
                    Description = "Add/subtract minutes", Mandatory = true
                },
                new Parameter {
                    Id = Id.Second, ObjectType = typeof (int), Label = "Seconds",
                    DefaultValue = 0,
                    Description = "Add/subtract seconds", Mandatory = true
                }

            };
            Description = "Select which images to apply the process to. Select to add or subtract from current date. Select time difference to add/subtract from current date.";
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
                if (image.DateTime != null) {
                    var date = image.DateTime.Value;
                    var mode = GetObject<Mode_e>(Id.Mode);
                    var sign = mode == Mode_e.Add ? 1 : -1;
                    var years = sign * GetObject<int>(Id.Year);
                    var months = sign * GetObject<int>(Id.Month);
                    var days = sign * GetObject<int>(Id.Day);
                    var hours = sign * GetObject<int>(Id.Hour);
                    var minutes = sign * GetObject<int>(Id.Minute);
                    var seconds = sign * GetObject<int>(Id.Second);
                    date = date.AddYears(years);
                    date = date.AddMonths(months);
                    date = date.AddDays(days);
                    date = date.AddHours(hours);
                    date = date.AddMinutes(minutes);
                    date = date.AddSeconds(seconds);
                    image.DateTime = date;
                    _updatedImages.Add(image);
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
