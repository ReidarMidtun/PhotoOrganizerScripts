using System.IO;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
namespace PhotoOrganizerScripts {
    class FileSize : BaseScript, IFilter {
        private const float MBLimit = 0.5f;
        private const float OneMB = 1000000.0f;
        public FileSize() {
            Description = "Checks the file size of an image according to a MB limt. Tailor the limit and the test in respect to equal, less than, greater than etc.";
        }
        public void Initialize() {
        }
        public bool IsValid(DB.Image image) {
            var fi = new FileInfo(image.FileName);
            // Tailor to your need, current checks if less than MBLimit
            return fi.Length / OneMB < MBLimit; // If less than a MB
        }
    }
}
