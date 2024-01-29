//
// These are added by default when compiling from VisualStudio
// When compiling manually it must be stated explicitly if in use.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PhotoOrganizer.ScriptController {
    class UserInputExample : BaseScript {
        // Used to address the explicit input when using generic functions
        // Tailor to your need
        enum Id { InputMode, MyImage, MyTag, MyFace, MyTable, MyDouble, MyDate, MyText, MyColor, MyFolder, MyBool, MyEnum };
        enum Mode_e { OptionA, OptionB, OptionC };
        public UserInputExample() {
            // Define your input variables with types and descriptions here
            // Remove/extend according to your needs
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode",
                    DefaultValue = InputImages_e.CurrentFolderImages,
                    Description = "What images are to be processed", Mandatory = true
                },
                new Parameter {
                    Id = Id.MyTag, ObjectType = typeof (DB.Tag), Label = "A Tag",
                    Description = "Drag/drop a tag to select",  Mandatory = false
                },
                new Parameter {
                    Id = Id.MyImage, ObjectType = typeof (DB.Image), Label = "An Image",
                    Description = "Drag/drop an image to select",  Mandatory = false
                },
                new Parameter {
                    Id = Id.MyFace, ObjectType = typeof (DB.Face), Label = "A Face",
                    Description = "Drag/drop a face to select",  Mandatory = false
                },
                new ParameterTable {
                    Id = Id.MyTable, Label = "My table",
                    Description = "Enter the table content by opening the table editor",  Mandatory = false,
                    Columns = new [] {
                        new ParameterTable.TableColumn {Name = "Image", ObjectType = typeof(DB.Image)},
                        new ParameterTable.TableColumn {Name = "Tag", ObjectType = typeof(DB.Tag)},
                        new ParameterTable.TableColumn {Name = "Text", ObjectType = typeof(string), DefaultValue = "XXX"},
                        new ParameterTable.TableColumn {Name = "Number", ObjectType = typeof(double), DefaultValue = 999.99},
                        new ParameterTable.TableColumn {Name = "Date", ObjectType = typeof(DateTime), DefaultValue = new DateTime(1970,1,1)},
                        new ParameterTable.TableColumn {Name = "Bool", ObjectType = typeof(bool), DefaultValue = true},
                        new ParameterTable.TableColumn {Name = "Enum", ObjectType = typeof(Mode_e), DefaultValue = Mode_e.OptionB},
                        new ParameterTable.TableColumn {Name = "Int", ObjectType = typeof(int), DefaultValue = 5},
                        new ParameterTable.TableColumn {Name = "Color", ObjectType = typeof(Color), DefaultValue = Color.Red},
                        new ParameterTable.TableColumn {Name = "File", ObjectType = typeof(FileInfo)},
                        new ParameterTable.TableColumn {Name = "Folder", ObjectType = typeof(DirectoryInfo)},
                        new ParameterTable.TableColumn {Name = "Float", ObjectType = typeof(float), DefaultValue = 888.88}
                    }
                },
                new Parameter {
                    Id = Id.MyDate, ObjectType = typeof (DateTime), Label = "Date",  DefaultValue = new DateTime(1970,1,1),
                    Description = "Give me a date",  Mandatory = false
                },
                new Parameter {
                    Id = Id.MyDouble,ObjectType = typeof (double), Label = "Float", DefaultValue = 99.99f,
                    Description = "Enter a floating point number", Mandatory = false
                },
                new Parameter {
                    Id = Id.MyColor, ObjectType = typeof (Color), Label = "Color", DefaultValue = "What ever",
                    Description = "Input a color ", Mandatory = false
                },
                new Parameter {
                    Id = Id.MyText, ObjectType = typeof (string), Label = "Text", DefaultValue = Color.Green,
                    Description = "Input a text ", Mandatory = false
                },
                new Parameter {
                    Id = Id.MyFolder, ObjectType = typeof (DirectoryInfo), Label = "Folder", DefaultValue = new DirectoryInfo("C:\\Temp"),
                    Description = "Input a folder ", Mandatory = false
                },
                new Parameter {
                    Id = Id.MyBool, ObjectType = typeof (bool), Label = "Boolean", DefaultValue = true,
                    Description = "A boolean ", Mandatory = false
                },
                new Parameter {
                    Id = Id.MyEnum, ObjectType = typeof (Mode_e), Label = "Option", DefaultValue = Mode_e.OptionB,
                    Description = "Select an option ", Mandatory = false
                }
            };
            Description = "This script is made to show how to receive user input to a script.";
        }



        protected override void PrepareExecution() {
            base.PrepareExecution(); // Uses the GetInputImages function to cache images to be processed
            //
            // This is how you get to the user input.
            // In this example the info is stored in function variables. If you need the info
            // for image processing, it must ne stored in class memebrs to be able to access it from the ProcessImage function
            // 
            //
            var inputImage = GetObject<DB.Image>(Id.MyImage);
            var aTag = GetObject<DB.Tag>(Id.MyTag);
            var aFace = GetObject<DB.Face>(Id.MyFace);
            var aDate = GetObject<DateTime>(Id.MyDate);
            var aNumber = GetObject<double>(Id.MyDouble);
            var aString = GetObject<string>(Id.MyText);
            var aColor = GetObject<Color>(Id.MyColor);
            var aFolder = GetObject<DirectoryInfo>(Id.MyFolder);
            var aBool = GetObject<bool>(Id.MyBool);
            var anEnum = GetObject<Mode_e>(Id.MyEnum);

            var inputObjects = new Dictionary<Enum, object> { { Id.MyImage, inputImage }, { Id.MyTag, aTag }, { Id.MyFace, aFace}, { Id.MyDate, aDate },
                {Id.MyDouble, aNumber }, {Id.MyText , aString }, {Id.MyColor, aColor }, {Id.MyFolder, aFolder },
                {Id.MyBool, aBool }, {Id.MyEnum, anEnum} };
            foreach (var o in inputObjects) {
                LogView.AppendText($"{o.Key}: {o.Value}");
            }

            // Table input
            var table = GetTable(Id.MyTable);
            LogView.AppendText($"The input table  contains {table.NRows} rows.");

            if (table != null) {
                foreach (var col in table.Columns) {
                    foreach (var row in table.Rows) {
                        // Since you have designed the table you also know the coloumn object type
                        // You should use the GetObject<SpecificType> to get to the needed info
                        var obj = table.GetObject<object>(col, row);
                        var msg = obj != null ? obj.ToString() : "Not set";
                        LogView.AppendText($"[{col.Name}, {row}]: {msg}");
                    }
                }
            }

            // This is how you get to elements from the DB
            var images = AllImages; // All images in DB
            LogView.AppendText($"The data base contains {images.Count} images.");

            var filteredImages = FilteredImages;
            LogView.AppendText($"There are currently {filteredImages.Count} images satisfying the current filter settings.");

            var filteredFolderImages = FilteredImagesCurrentFolder;
            LogView.AppendText($"There are currently {filteredFolderImages.Count} images satisfying the current filter settings in the currently selected folder.");
            // How to traverse result
            LogView.AppendText($"Filtered folder image filenames");
            foreach (var image in filteredFolderImages) {
                LogView.AppendText($"\t{image.FileName}");
            }

            var persons = AllPersons;
            LogView.AppendText($"The data base contains {persons.Count} number of persons.");

            var scenes = AllScenes;
            LogView.AppendText($"The data base contains {scenes.Count} number of scenes.");

            var tags = AllTags;
            LogView.AppendText($"The data base contains {tags.Count} number of tags.");

            var objects = AllObjects;
            LogView.AppendText($"The data base contains {objects.Count} number of objects.");

            var playLists = AllPlayLists;
            LogView.AppendText($"The data base contains {playLists.Count} number of playlist.");

        }


        protected override IEnumerable<DB.Image> GetInputImages() {
            return GetInputImages(GetObject<InputImages_e>(Id.InputMode));
        }

        protected override bool ProcessImage(DB.Image image) {
            var ok = true;
            try {
                LogView.AppendText($"Processing {image.Name}");
            }
            catch {
                ok = false;
            }
            return ok;
        }

    }
}
