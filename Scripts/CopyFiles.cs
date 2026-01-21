// css_ref System.Memory.dll, Magick.NET.Core.dll, System.Drawing.Common
using ImageMagick;
using Newtonsoft.Json.Linq;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoOrganizerScripts {
    public class CopyFiles : BaseScript {
        enum Id { FileListFile, SourceRoot, TargetRoot};
        private FileInfo _inputFile;
        private string _sourceRoot;
        private string _targetRoot;

        public CopyFiles() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.FileListFile, ObjectType = typeof (FileInfo), Label = "File list file",
                    Description = "Select a file with file names to copy",Mandatory = true
                },
                new Parameter {
                    Id = Id.SourceRoot, ObjectType = typeof (string), Label = "Source root",
                    Description = "Select a source root",Mandatory = true
                },
                new Parameter {
                    Id = Id.TargetRoot, ObjectType = typeof (string), Label = "Target root",
                    Description = "Select a source root",Mandatory = true
                }
            };
            Description = "Target image file names are read from the file name list. Source file names are made by substituting the target root by the source root. If the source files are existing and not corrupt they are copied to the target file name.";
        }

        protected override void PrepareExecution() {
            _inputFile = GetObject<FileInfo>(Id.FileListFile);
            _targetRoot = GetObject<string>(Id.TargetRoot);
            _sourceRoot = GetObject<string>(Id.SourceRoot);
        }


        protected override async Task DoExecution() {
            Main.BackgroundTask = Task.Run(() => DoExecutionTask(Main.Progress, ProgressIndicator.Token));
            await Main.BackgroundTask;
        }

        protected async Task DoExecutionTask(ProgressIndicator pi, CancellationToken ct) {
            if (!_inputFile.Exists) {
                LogView.AppendText("Missing input file name list file.", Color.Red);
                return;
            }
            var targetFiles = File.ReadLines(_inputFile.FullName);
            pi.Text = $"Executing script {Name}";
            pi.Maximum = targetFiles.Count();
            var cancelled = false;
            foreach (var targetFile in targetFiles) {
                var sourceFile = _sourceRoot + targetFile.Substring(_targetRoot.Length);
                if (File.Exists(sourceFile)) {
                    var im = FormsUtilities.GetDrawingImage(sourceFile);
                    if (im != null) {
                        try {
                            File.Copy(sourceFile, targetFile, true);
                            LogView.AppendText($"Copied {sourceFile} to {targetFile}.");
                        }
                        catch (Exception ex) {
                            LogView.AppendText($"Unable to copy {sourceFile} to jpg.\n{ex.Message}");
                        }
                    } else {
                        LogView.AppendText($"Source file {sourceFile} is corrupt, skipping.", Color.Red);
                    }
                } else {
                    LogView.AppendText($"Source file {sourceFile} is missing, skipping.", Color.Red);
                }
                if (ct.IsCancellationRequested) {
                    cancelled = true;
                    break;
                }
                pi.PerformStep();
            }
            pi.Text = cancelled ? "File copy was cancelled." : "File copy sucessfully executed.";

            pi.Reset();
        }
    }
}
