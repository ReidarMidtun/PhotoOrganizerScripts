//
// These are added by default when compiling from VisualStudio
// When compiling manually it must be stated explicitly if in use.
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
    class NewScript : BaseScript {
        // Used to address the explicit input when using generic functions
        // Tailor to your need
        enum Id { MyImage, MyTag, MyTable, MyDouble, MyDate, MyText, MyColor, MyFolder, MyBool, MyEnum };
        enum Mode_e { OptionA, OptionB, OptionC};
        public NewScript()  {
            // Define your input objects with type and descriptions here
            // Define your input variables with types and descriptions here
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.MyTag, ObjectType = typeof (DB.Tag), Label = "A Tag",
                    Description = "Input tag to process",  Mandatory = false
                },
                new Parameter {
                    Id = Id.MyImage, ObjectType = typeof (DB.Image), Label = "An Image",
                    Description = "Input image to process",  Mandatory = false
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
            // Describe your result object produced in the Execute function. This is mainly used for documentation
            Result = new ResultParameter(typeof(DB.Image), "The updated image");
            Description = "Describe to the user what your script is doing here";
        }

        // This function is called after input is verified
        protected override object Execute() {
            PrintInput();

            // Get a handle to the objects specified by the user
            var image = GetObject<DB.Image>(Id.MyImage);
            var tag = GetObject<DB.Tag>(Id.MyTag);
            var aDate = GetObject<DateTime>(Id.MyDate);
            var aNumber = GetObject<double>(Id.MyDouble);
            var aString = GetObject<string>(Id.MyText);
            var aColor = GetObject<Color>(Id.MyColor);
            var aFolder = GetObject<DirectoryInfo>(Id.MyFolder);
            var aBool = GetObject<bool>(Id.MyBool);
            var mode = GetObject<Mode_e>(Id.MyEnum);
            var table = GetTable(Id.MyTable);
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

            LogView.AppendText($"Processing: {(image != null ? image.Name : "Not selected")} using following input arguments:");
            PrintInputVariables();

            Result.Object = new ResultParameter(typeof(DB.Image), "The processed image");
            return image;
        }

    }
}
