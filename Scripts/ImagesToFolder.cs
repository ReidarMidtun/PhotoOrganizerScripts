using System.Collections.Generic;
using System.IO;
using System.Linq;

using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    public class ImagesToFolder : BaseScript {
        enum Id { OutputFolder, InputMode, CopyOrMove, PlayList };
        enum Mode { Copy, Move };
        private DirectoryInfo _outputFolder;
        private Mode _mode;
        private Dictionary<string, DB.Image> _newImages = null;
        private List<DB.Image> _updatedImages = null;
        private List<DB.Image> _deletedImages = null;
        private HashSet<string> _existingImages = null;

        public ImagesToFolder() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode",
                    Description = "What images are to be processed", DefaultValue = InputImages_e.CurrentFolderFilteredImages, Mandatory = true
                },
                new Parameter {
                    Id = Id.PlayList, ObjectType = typeof (DB.PlayList), Label = "Playlist",
                    Description = "If given, the images of the playlist are copied regardless of the input mode given above",
                    Mandatory = false
                },
                new Parameter {
                    Id = Id.OutputFolder, ObjectType = typeof (DirectoryInfo), Label = "Output folder",
                    Description = "Select an output folder for the images",Mandatory = true
                },
                new Parameter {
                    Id = Id.CopyOrMove, ObjectType = typeof (Mode), Label = "Output mode", DefaultValue = Mode.Copy,
                    Description = "Select to copy or move images to the output folder", Mandatory = true
                }
            };
            Description = "Copies images to an output folder. ";
        }

        protected override void PrepareExecution() {
            base.PrepareExecution();
            _outputFolder = GetObject<DirectoryInfo>(Id.OutputFolder);
            _mode = GetObject<Mode>(Id.CopyOrMove);
            _newImages = new Dictionary<string, DB.Image>();
            _updatedImages = new List<DB.Image>();
            _deletedImages = new List<DB.Image>();
            _existingImages = AllImages.Select(image => image.FileName).ToHashSet();
        }

        protected override IEnumerable<DB.Image> GetInputImages() {
            return GetInputImages(GetObject<InputImages_e>(Id.InputMode), GetObject<DB.PlayList>(Id.PlayList));
        }

        protected override bool ProcessImage(DB.Image image) {
            var ok = true;   
            try {
                var targetName = Path.Combine(_outputFolder.FullName, Path.GetFileName(image.FileName));
                if (File.Exists(targetName) || _existingImages.Contains(targetName)) {
                    LogView.AppendText($"File: {targetName} is already existing.");
                    return true;
                }
                DB.Image.GetRelativeFileName(targetName, out var source, out var relFileName);
                if (source >= 0) { // Inside source folders
                    if (_mode == Mode.Copy) {
                        var copiedImage = image.Clone() as DB.Image; // Does a file copy of the original
                        copiedImage.RelFileName = relFileName;
                        copiedImage.Source = source;
                        _newImages[image.FileName] = copiedImage;
                        Thumbnail.MakeThumbnailFile(128, image.FileName, copiedImage.ThumbnailFileName);
                    }
                    else {
                        image.RelFileName = relFileName;
                        image.Source = source;
                        _updatedImages.Add(image);
                    }
                }
                else {
                    if (_mode == Mode.Copy) {
                        File.Copy(image.FileName, targetName);
                    }
                    else {
                        _deletedImages.Add(image); // Moved away from source
                        File.Move(image.FileName, targetName);
                    }
                }
            } 
            catch {     
                ok = false; 
            }
            return ok;
        }

        protected override void EndExecution() {
            if (_newImages.Any()) { // Update database before copying the file !!!!!!!!!!!!!!!
                DB.Insert(_newImages.Values.ToList());
                foreach (var kvp in _newImages) {
                    File.Copy(kvp.Key, kvp.Value.FileName);
                }
            }
            DB.Update(_updatedImages);
            _deletedImages.ForEach(im => DB.Image.DeleteImageFiles(im, false));
            DB.Delete(_deletedImages);
        }
    }
}
