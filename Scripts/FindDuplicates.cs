// css_ref System.Security.Cryptography.dll, System.Linq.Expressions.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace PhotoOrganizerScripts {
    class FindDuplicates : BaseScript {
        enum Id { InputMode };

        public class FileDetails {
            public string FileName { get; set; }
            public string FileHash { get; set; }
        }

        private List<FileDetails>? _fileInfo;

        public FindDuplicates() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode", 
                    DefaultValue = InputImages_e.CurrentFolderImages,
                    Description = "What images are to be processed", Mandatory = true
                }
            };
            Description = "Finds duplicate files. Duplicates are tagged with the tag \"Duplicate\". After running the script, filter on the Duplicate tag to inspect the result.";
        }

        protected override IEnumerable<DB.Image> GetInputImages() {
            return GetInputImages(GetObject<InputImages_e>(Id.InputMode));
        }

        protected override void PrepareExecution() {
            base.PrepareExecution(); // Caches the input images before starting the processing of images
            _fileInfo = new List<FileDetails>();
        }

        protected override bool ProcessImage(DB.Image image) {
            var ok = true;
            try {
                using (var fs = new FileStream(image.FileName, FileMode.Open, FileAccess.Read)) {
                    _fileInfo.Add(new FileDetails() {
                        FileName = image.FileName,
                        FileHash = BitConverter.ToString(SHA1.Create().ComputeHash(fs)),
                    });
                }
            }
            catch {
                ok = false;
            }
            return ok;
        }

        private DB.Tag GetDuplicateTag() {
            var col = DB.GetTagCollection(DB.DataBase);
            var result = col.FindOne(t => t.Name == "Duplicate");
            if (result == null) {
                result = new DB.Tag("Duplicate");
                DB.Insert(new DB.Tag[] { result });
            }
            return result;
        }

        protected override void EndExecution() {
            if (_fileInfo != null && _fileInfo.Any()) {
                var similarList = _fileInfo.GroupBy(f => f.FileHash).Select(g => new { FileHash = g.Key, Files = g.Select(z => z.FileName).ToList() });
                var duplicates = similarList.Where( s => s.Files.Count > 1 );
                var duplicateTag = GetDuplicateTag();
                var images = InputImages.ToDictionary(k => k.FileName);
                var imagesToUpdate = new List<DB.Image>();
                foreach ( var duplicate in duplicates ) {
                    var fileInfo = duplicate.Files.Select(f => new FileInfo(f)).OrderBy(fi => fi.CreationTime).ToList();
                    foreach (var file in fileInfo) {
                        var image = images[file.FullName];
                        if (!image.Tags.Contains(duplicateTag.Name)) {
                            image.Tags.Add(duplicateTag.Name);
                            imagesToUpdate.Add(image);
                        }
                        if (file == fileInfo[0]) {
                            LogView.AppendText($"Duplicate of file {file.FullName}");
                        } else {
                            LogView.AppendText($"\t{file.FullName}");
                        }
                    }
                }
                if (imagesToUpdate.Any()) {
                    DB.Update(imagesToUpdate);
                }
            }
        }
    }
}
