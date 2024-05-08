using System.Collections.Generic;
using System.IO;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
namespace PhotoOrganizerScripts {
    internal class FileSize : BaseScript, IFilter {
        enum Id {MBLimit, CompareMode}
       
        private const float OneMB = 1000000.0f;
        public FileSize() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.CompareMode, ObjectType = typeof (CompareMode_e), Label = "Compare mode",
                    Description = "Select a compare mode", DefaultValue = CompareMode_e.LessThan},
                new Parameter {
                    Id = Id.MBLimit, ObjectType = typeof (float), Label = "Mega byte size limit",
                    Description = "Input a size limit in units of mega byte", DefaultValue = 0.9f }
            };
            Description = "Compares the filesize of an image against the given number of mega bytes using the given compare mode.";
        }

        public void Initialize() {
            MBLimit = GetObject<float>(Id.MBLimit);
            CompareMode = GetObject<CompareMode_e>(Id.CompareMode);
        }

        public bool IsValid(DB.Image image) {
            var fi = new FileInfo(image.FileName);
            var mb = fi.Length / OneMB;
            return Compare(mb, MBLimit, CompareMode); 
        }

        private float MBLimit { get; set; } = 0.5f;
        private CompareMode_e CompareMode { get; set; } = CompareMode_e.LessThan;
        string IFilter.Name { get; set;}
    }
}
