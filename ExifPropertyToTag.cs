using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks; 
using System.Windows.Forms;
using GMap.NET.MapProviders;
using MetadataExtractor;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    public class ExifPropertyToTag : BaseScript {
        enum Id { InputMode, PropertyName, PropertyValue, Tag };
        private string _propertyName;
        private string _propertyValue;
        private DB.Tag _tag;
        private List<DB.Image> _tagedImages;
        public ExifPropertyToTag() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode", 
                    DefaultValue = InputImages_e.CurrentFolderImages,
                    Description = "What images are to be processed", Mandatory = true
                },
                new Parameter {
                    Id = Id.PropertyName, ObjectType = typeof (string), Label = "Exif property name",
                    Description = "Property label", Mandatory = true
                },
                new Parameter {
                    Id = Id.PropertyValue, ObjectType = typeof (string), Label = "Exif property value",
                    Description = "Property value", Mandatory = true
                },
                new Parameter {
                    Id = Id.Tag, ObjectType = typeof (DB.Tag), Label = "Tag",
                    Description = "Drag/drop a tag to select", Mandatory = true
                }
            };
            Description = "Tags an image with selected tag if the exif property name has the given value";
        }

        protected override void PrepareExecution() {
            base.PrepareExecution();
            _propertyName = GetObject<string>(Id.PropertyName);
            _propertyValue = GetObject<string>(Id.PropertyValue);
            _tag = GetObject<DB.Tag>(Id.Tag);
            _tagedImages = new List<DB.Image>();
        }

        protected override IEnumerable<DB.Image> GetInputImages() {
            return GetInputImages(GetObject<InputImages_e>(Id.InputMode));
        }

        protected override bool ProcessImage(DB.Image image) {
            var ok = true;   
            try {
                var directories = ImageMetadataReader.ReadMetadata(image.FileName);
                foreach (var directory in directories) {
                    foreach (var tag in directory.Tags) {
                        var propertyName = $"{directory.Name} - {tag.Name}";
                        var propertyValue = tag.Description != null ? tag.Description : "";
                        if (propertyName == _propertyName && propertyValue == _propertyValue) {
                            if (!image.Tags.Contains(tag.Name)) {
                                image.Tags.Add(_tag.Name);
                                _tagedImages.Add(image);
                            }
                        }
                    }
                }
            }
            catch {     
                ok = false; 
            }
            return ok;
        }

        protected override void EndExecution() {
            DB.Update(_tagedImages);
        }
    }
}
