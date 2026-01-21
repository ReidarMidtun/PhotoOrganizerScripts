// css_ref System.Drawing.Common, Magick.NET.Core.dll, System.Memory
using ImageMagick;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoOrganizerScripts {
    public class FindCorruptFiles : BaseScript {
        enum Id { InputFolder, OutputFile, InDBOnly };
        private FileInfo _outputFile;
        private DirectoryInfo _inputFolder;
        private bool _inDbOnly;

        public FindCorruptFiles() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputFolder, ObjectType = typeof (DirectoryInfo), Label = "Folder",
                    Description = "Folder containing image files",
                    Mandatory = true
                },
                new Parameter {
                    Id = Id.OutputFile, ObjectType = typeof (FileInfo), Label = "Output file",
                    Description = "File that will contain the file name of corrupt image files",
                    Mandatory = false
                },
                new Parameter {
                    Id = Id.InDBOnly, ObjectType = typeof (bool), Label = "In db only ",
                    Description = "Only chcek files already in the database.",
                    Mandatory = false
                },

            };
            Description = "Checks all image files in the input folder and all sub folders below. The file name of corrupt files are written to the log window and to the output file if given.";
        }

        protected override void PrepareExecution() {
            _outputFile = GetObject<FileInfo>(Id.OutputFile);
            _inputFolder = GetObject<DirectoryInfo>(Id.InputFolder);
            _inDbOnly = GetObject<bool>(Id.InDBOnly);
        }

        protected override async Task DoExecution() {
            Main.BackgroundTask = Task.Run(() => DoExecutionTask(Main.Progress, ProgressIndicator.Token));
            await Main.BackgroundTask;
        }

        protected async Task DoExecutionTask(ProgressIndicator pi, CancellationToken ct) {
            if (_inputFolder == null || !_inputFolder.Exists)   {
                LogView.AppendText("Input folder is not set or does not exist.", Color.Red);
                return;
            }
            StreamWriter? outputStream = null;
            if (_outputFile != null) {
                try {
                    outputStream = new StreamWriter(_outputFile.FullName);
                }
                catch (Exception ex) {
                    LogView.AppendText($"Could not open output file {_outputFile.FullName} for writing: {ex.Message}", Color.Red);
                    return;
                }
            }
            var targetFiles = _inputFolder.EnumerateFiles("*", SearchOption.AllDirectories).Where(FilterView.IsImageFile);
            pi.Text = $"Executing script {Name}";
            pi.Maximum = targetFiles.Count();
            var cancelled = false;
            foreach (var targetFile in targetFiles) {
                if (!_inDbOnly || (_inDbOnly && FilterView.Instance.GetImage(targetFile.FullName) != null)) {
                    var ok = CheckFile(targetFile.FullName);
                    if (!ok) {
                        LogView.AppendText(targetFile.FullName);
                        if (outputStream != null) {
                            outputStream.WriteLine(targetFile.FullName);
                        }
                    }
                    if (ct.IsCancellationRequested) {
                        cancelled = true;
                        break;
                    }
                }
                pi.PerformStep();
            }
            if (outputStream != null) {
                outputStream.Close();
            }
            pi.Text = cancelled ? "File check was cancelled." : "File check sucessfully executed.";
            pi.Reset();
        }

        private bool CheckFile(string fileName) {
            try {
                var im = FormsUtilities.GetDrawingImage(fileName);
                if (im == null) {
                    return false; 
                }
            }
            catch (Exception ex) {
                return false;
            }
            return true;
        }
    }
}
