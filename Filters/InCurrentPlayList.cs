
using System.Collections.Generic;
using System.Linq;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    class InCurrentPlayList : BaseScript, IFilter {
        private HashSet<string> _validFileNames = new HashSet<string>();
        public InCurrentPlayList() {
            Description = "Check if the image is contained by the current selected playlist.";
        }
        public void Initialize() {
            // Gets the current playlist and reads the images contained by it
            var pl  = SelectedPlayList;
            _validFileNames = pl != null ? pl.FileNames.ToHashSet() : new HashSet<string>();
        }
        public bool IsValid(DB.Image image) {
            return _validFileNames.Contains(image.FileName);
        }
    }
}
