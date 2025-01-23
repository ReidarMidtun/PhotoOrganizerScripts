// css_ref System.Memory.dll, Magick.NET.Core.dll
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Newtonsoft.Json.Linq;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    public class FileConvert : BaseScript {
        enum Id { InputFolder, FilePattern, SubFolders, RemoveOriginal, Quality };
        private List<string> _filesToDelete;
        private DirectoryInfo _inputFolder;
        private string _filePattern;
        private bool _subFolders;
        private bool _removeOriginal;
        private uint _quality;
        public FileConvert() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputFolder, ObjectType = typeof (DirectoryInfo), Label = "Output folder",
                    Description = "Select an output folder",Mandatory = true
                },
                new Parameter {
                    Id = Id.SubFolders, ObjectType = typeof (bool), Label = "Include sub folders",
                    Description = "Include sub folders", DefaultValue = false, Mandatory = true
                },
                new Parameter {
                    Id = Id.RemoveOriginal, ObjectType = typeof (bool), Label = "Remove original",
                    Description = "Toggle on to delete the original file", DefaultValue = false, Mandatory = true
                },
                new Parameter {
                    Id = Id.FilePattern, ObjectType = typeof (string), Label = "File pattern", DefaultValue = "*.heic",
                    Description = "Select a file pattern", Mandatory = true
                },
                new Parameter {
                    Id = Id.Quality, ObjectType = typeof (int), Label = "Quality", DefaultValue = 75,
                    Description = "Quality/compression level [1-100]", Mandatory = true
                }
            };
            Description = "Image files in the input folder complying to the file pattern is converted to JPG if a JPG file is not already existing. If include sub folders is toggled on, files in subfolders below the input folder is also searched. If remove original file is toggled on, the input files to the conversion is deleted. NOTE that it might take a minute after the conversion before the new JPG images appears in PhotoOrgz.";
        }

        protected override void PrepareExecution() {
            base.PrepareExecution();
            _filesToDelete = new List<string>();
            _inputFolder = GetObject<DirectoryInfo>(Id.InputFolder);
            _filePattern = GetObject<string>(Id.FilePattern);
            _subFolders = GetObject<bool>(Id.SubFolders);
            _removeOriginal = GetObject<bool>(Id.RemoveOriginal);
            _quality = (uint) GetObject<int>(Id.Quality);
        }


        protected override void EndExecution() {
            base.EndExecution();
            var toDelete = new List<DB.Image>();
            foreach (var file in _filesToDelete) {
                var image = DB.Image.GetImage(file);
                if (image != null) {
                    toDelete.Add(image);
                }
                if (File.Exists(file)) {
                    File.Delete(file);
                }
            }
            if (toDelete.Any()) {
                DB.Delete(toDelete);
            }
        }

        protected override async Task DoExecution() {
            Main.BackgroundTask = Task.Run(() => DoExecutionTask(Main.Progress, ProgressIndicator.Token));
            await Main.BackgroundTask;
        }

        protected  async Task DoExecutionTask(ProgressIndicator pi, CancellationToken ct) {
            var opt = _subFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var inputFiles = _inputFolder.GetFiles(_filePattern, opt);
            pi.Text = $"Executing script {Name}";
            pi.Maximum = inputFiles.Count();
            var cancelled = false;
            foreach (var file in inputFiles) {
                try {
                    var outputFile = Path.Combine(_inputFolder.FullName, Path.GetFileNameWithoutExtension(file.Name) + ".jpg");
                    if (File.Exists(outputFile)) {
                        LogView.AppendText($"File {outputFile} is already existing.");
                        continue;
                    }
                    using (var mi = new MagickImage(file)) {
                        mi.Format = MagickFormat.Jpg;
                        mi.Quality = _quality;
                        mi.Write(outputFile, MagickFormat.Jpg);
                    }
                    if (_removeOriginal) {
                        _filesToDelete.Add(file.FullName);
                    }
                    LogView.AppendText($"Converted {file.FullName} to {outputFile}.");
                    if (ct.IsCancellationRequested) {
                        cancelled = true;
                        break;
                    }
                }
                catch (Exception ex) {
                    LogView.AppendText($"Unable to convert {file.FullName} to jpg.\n{ex.Message}");
                }
                pi.PerformStep();
            }
            pi.Text = cancelled ? "File conversion was cancelled." : "File conversion sucessfully executed.";

            pi.Reset();
        }
    }
}
