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
    class InCurrentPlayList : BaseScript, IFilter {
        private HashSet<string> _validFileNames = new HashSet<string>();
        public InCurrentPlayList() {
            Description = "Check if the image is contained by the current selected playlist.";
        }
        public void Initialize() {
            var pl  = PlayListView.Instance.SelectedPlayListObject;
            _validFileNames = pl != null ? pl.FileNames.ToHashSet() : new HashSet<string>();
        }
        public bool IsValid(DB.Image image) {
            return _validFileNames.Contains(image.FileName);
        }
    }
}
