using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
using System.Collections.Generic;
using static PhotoOrganizer.DB;

namespace PhotoOrganizerScripts {
    class NFaces : BaseScript, IFilter {
        enum Id { NFaces, CompareMode }
        public NFaces() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.CompareMode, ObjectType = typeof (CompareMode_e), Label = "Compare mode",
                    Description = "Select a compare mode", DefaultValue = CompareMode_e.LessThan},
                new Parameter {
                    Id = Id.NFaces, ObjectType = typeof (int), Label = "Number of faces",
                    Description = "Input a size limit in units of mega byte", DefaultValue = 5 }
            };

            Description = "Compares the number of faces in an image against the given number of faces using the given compare mode.";
        }
        public void Initialize() {
            NFaceLimit = GetObject<int>(Id.NFaces);
            CompareMode = GetObject<CompareMode_e>(Id.CompareMode);
        }
        public bool IsValid(DB.Image image) {
            // Tailor to your need, this checks greater than the NLimit
            var nFaces = image.Faces != null ? image.Faces.Count : 0;
            return Compare(nFaces, NFaceLimit, CompareMode);
        }

        string IFilter.Name { get; set; }
        private int NFaceLimit { get; set; } = 0;
        private CompareMode_e CompareMode { get; set; } = CompareMode_e.LessThan;
    }
}
