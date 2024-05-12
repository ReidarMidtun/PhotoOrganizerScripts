using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
using System.Collections.Generic;
using static PhotoOrganizer.DB;

namespace PhotoOrganizerScripts {
    class MissingDate : BaseScript, IFilter {
        public MissingDate() {
            Description = "Filter on images with missing date.";
        }
        public void Initialize() {
        }
        public bool IsValid(DB.Image image) {
            return image.DateTime == null;
        }
        string IFilter.Name { get; set; }
    }
}
