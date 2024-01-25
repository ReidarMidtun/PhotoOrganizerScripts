//
// These are added by default when compiling from VisualStudio
// When compiling manually it must be stated explicitly if in use.
using System.Collections.Generic;
using System.Linq;

namespace PhotoOrganizer.ScriptController {
    class InCurrentPlayList : BaseScript, IFilter {
        private HashSet<string> _validFileNames = new HashSet<string>();
        public InCurrentPlayList() {
            Description = "Check if the image is contained by the current selected playlist.";
        }
        public void Initialize() {
            var pl  = SelectedPlayList;
            _validFileNames = pl != null ? pl.FileNames.ToHashSet() : new HashSet<string>();
        }
        public bool IsValid(DB.Image image) {
            return _validFileNames.Contains(image.FileName);
        }
    }
}
