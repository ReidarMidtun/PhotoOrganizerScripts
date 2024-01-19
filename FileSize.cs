//
// These are added by default when compiling from VisualStudio
// When compiling manually it must be stated explicitly if in use.
using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhotoOrganizer.ScriptController {
    class FileSize : BaseScript, IFilter {
        public FileSize() {
            Description = "Checks the file size of an image to be smaller than a given size.";
        }
        public void Initialize() {
        }
        public bool IsValid(DB.Image image) {
            var fi = new FileInfo(image.FileName);
            return (fi.Length / 1000000) == 0; // If less than a MB
        }
    }
}
