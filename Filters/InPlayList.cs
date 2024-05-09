using System.Collections.Generic;
using System.Linq;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    class InPlayList : BaseScript, IFilter {
        private HashSet<string> _validFileNames = new HashSet<string>();
        private DB.PlayList _playList;
        enum Id { PlayList }

        public InPlayList() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.PlayList, ObjectType = typeof (DB.PlayList), Label = "Play list",
                    Description = "Drag/drop a playlist from the play list panel"}
            };
            Description = "Check if the image is contained by the current selected playlist.";
        }
        public void Initialize() {
            // Gets the current playlist and reads the images contained by it
            PlayList = GetObject<DB.PlayList>(Id.PlayList);
        }
        public bool IsValid(DB.Image image) {
            return _validFileNames.Contains(image.FileName);
        }

        string IFilter.Name { get; set; }
        private DB.PlayList PlayList {
            get { return _playList; }
            set {
                _playList = value;
                _validFileNames = _playList != null ? _playList.GetFileNames().ToHashSet() : new HashSet<string>();
            }
        }
       
       

    }
}
