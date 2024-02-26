using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    class NFaces : BaseScript, IFilter {
        private const int NLimit = 5; // Tailor to your need
        public NFaces() {
            Description = "Checks the number of faces in the image. Tailor the test to use the number you are looking for and by using equal, less than, greater than etc.";
        }
        public void Initialize() {
        }
        public bool IsValid(DB.Image image) {
            // Tailor to your need, this checks greater than the NLimit
            return image.Faces != null ? image.Faces.Count > NLimit : false;
        }
    }
}
