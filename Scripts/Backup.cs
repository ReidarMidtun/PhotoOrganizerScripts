// css_ref System.Drawing.Common.dll, System.Memory.dll, Magick.NET.Core.dll
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;
using ImageMagick;

namespace PhotoOrganizerScripts {
    public class Backup : BaseScript {
        enum Id { OutputFolder, InputMode, ImportExport, PlayList };
        enum Mode { Restore, Store };
        private Mode _mode;
        private List<DB.Image> _importedImages;
        private HashSet<string> _existingTags;
        private HashSet<string> _existingScenes;
        private HashSet<string> _existingObjects;
        private HashSet<string> _existingPersons;
        private HashSet<string> _newTags;
        private HashSet<string> _newScenes;
        private HashSet<string> _newObjects;
        private HashSet<string> _newPersons;
        private int _nProcessed;

        public Backup() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.InputMode, ObjectType = typeof (InputImages_e), Label = "Input mode",
                    Description = "What images are to be processed", DefaultValue = InputImages_e.CurrentFolderAndSubFoldersFiltered, Mandatory = true
                },
                new Parameter {
                    Id = Id.PlayList, ObjectType = typeof (DB.PlayList), Label = "Playlist",
                    Description = "If given, the images of the playlist are copied regardless of the input mode given above",
                    Mandatory = false
                },
                new Parameter {
                    Id = Id.ImportExport, ObjectType = typeof (Mode), Label = "Mode", DefaultValue = Mode.Restore,
                    Description = "Select to store or restore", Mandatory = true
                }
            };
            Description = "Stores/restores image meta data information. On store the meta data are written to json files in the same folder as the image file. When restoring, the meta data from the json file overwrites the meta data in the database.";
        }

        protected override void PrepareExecution() {
            _nProcessed = 0;
            _mode = GetObject<Mode>(Id.ImportExport);
            _importedImages = new List<DB.Image>();
            if (_mode == Mode.Restore) {
                _existingTags = DB.GetTagCollection(DB.DataBase).FindAll().Select(t => t.Name).ToHashSet();
                _existingScenes = DB.GetSceneCollection(DB.DataBase).FindAll().Select(t => t.Name).ToHashSet();
                _existingObjects = DB.GetObjectCollection(DB.DataBase).FindAll().Select(t => t.Name).ToHashSet();
                _existingPersons = DB.GetPersonCollection(DB.DataBase).FindAll().Select(t => t.Name).ToHashSet();
            }

            _newTags = new HashSet<string>();
            _newScenes = new HashSet<string>();
            _newObjects = new HashSet<string>();
            _newPersons = new HashSet<string>();

            base.PrepareExecution();
        }

        protected override IEnumerable<DB.Image> GetInputImages() {
            return GetInputImages(GetObject<InputImages_e>(Id.InputMode), GetObject<DB.PlayList>(Id.PlayList));
        }

        protected override bool ProcessImage(DB.Image image) {
            var ok = true;
            _nProcessed++;
            if (!File.Exists(image.FileName)) {
                LogView.AppendText($"Missing image file {image.FileName}");
                return true; // Report the error and continue
            }
            try {
                if (_mode == Mode.Store) {
                    Store(image);
                } else {
                    var importedImage = Restore(image);
                    if (importedImage != null) {
                        _importedImages.Add(Restore(importedImage));
                    }
                }
            }
            catch {     
                ok = false; 
            }
            return ok; 
        }

        private void Save<T>(HashSet<string> names)  where T : DB.DB_Object {
            if (names.Any()) { // Try to make a generic function to do this
                var items = new List<T>();
                names.ToList().ForEach(n => items.Add(Activator.CreateInstance(typeof(T),new object[] { n }) as T));
                DB.Insert(items);
            }
        }

        protected override void EndExecution() {
            var process = _mode == Mode.Restore ? "restored" : "stored";
            LogView.AppendText($"Meta data for {_nProcessed} images {process}.");
            if (_importedImages.Any()) { 
                DB.Update(_importedImages);
            }
            Save<DB.Tag>(_newTags);
            Save<DB.Scene>(_newScenes);
            Save<DB.Object>(_newObjects);
            Save<DB.Person>(_newPersons);
        }

        private DB.Image Restore(DB.Image image) {
            var jsonFileName = GetJsonFileName(image);
            if (!File.Exists(jsonFileName))
                return null;
            try {
                var myJson = JsonConvert.DeserializeObject<Root>(File.ReadAllText(GetJsonFileName(image)));
                image.Rate = myJson.Rate;
                image.SceneScanDone = myJson.SceneScanDone;
                image.Scene = myJson.Scene;
                if (!string.IsNullOrEmpty(image.Scene) && !_existingScenes.Contains(image.Scene)) {
                    _newScenes.Add(image.Scene);
                }
                image.ObjectScanDone = myJson.ObjectScanDone;
                image.Objects = myJson.ObjectsInImage;
                _newObjects.UnionWith(image.Objects.Where(o => !_existingObjects.Contains(o)));

                image.Tags = myJson.Tags;
                _newTags.UnionWith(image.Tags.Where(t => !_existingTags.Contains(t)));

                image.FaceScanDone = myJson.FaceScanDone;
                if (myJson.RegionInfo != null && myJson.RegionInfo.RegionList != null && myJson.RegionInfo.RegionList.Any()) {
                    var faces = new List<DB.Face>();
                    System.Drawing.Image drawingImage = null;
                    foreach (var r in myJson.RegionInfo.RegionList) {
                        var ltrb = new int[4];
                        ltrb[0] = (int)Math.Round(myJson.RegionInfo.AppliedToDimensions.W * r.Area.X);
                        ltrb[1] = (int)Math.Round(myJson.RegionInfo.AppliedToDimensions.H * r.Area.Y);
                        ltrb[2] = ltrb[0] + (int)Math.Round(myJson.RegionInfo.AppliedToDimensions.W * r.Area.W);
                        ltrb[3] = ltrb[1] + (int)Math.Round(myJson.RegionInfo.AppliedToDimensions.H * r.Area.H);
                        var face = new DB.Face(image.ImageId, ltrb.ToList(), r.FaceConfidence);
                        face.Person = r.Name;
                        var existingFace = GetFace(image.Faces, face);
                        if (existingFace == null) {
                            if (drawingImage == null) {
                                drawingImage = image.GetDrawingImage();
                            }
                            face.CreateFaceImage(drawingImage);
                            face.PersonConfidence = r.PersonConfidence;
                            faces.Add(face);
                            if (face.IsKnown && !_existingPersons.Contains(face.Person)) {
                                _newPersons.Add(face.Person);
                            } 
                        }
                        else {
                            faces.Add(existingFace);
                        }
                    }
                    // Clean up not used faces
                    foreach(var f in image.Faces) { 
                        if (!faces.Contains(f)) { 
                            if (File.Exists(f.FaceFileName)) { 
                                try {
                                    File.Delete(f.FaceFileName);
                                }
                                catch { } //No worry
                            }
                        }
                    }
                    image.Faces = faces;
                }
            }
            catch (Exception ex) {
                LogView.AppendText($"Error deserializing json file for image {image.FileName}.\n{ex.Message}");
                return null;
            }
            return image;
        }

        private DB.Face GetFace(List<DB.Face> existing, DB.Face face) { 
            return existing.Find(f => f != null && f.Name.Equals(face.Name) && f.LTRB.ToHashSet().SetEquals(face.LTRB.ToHashSet())); 
        }

        private static void Store(DB.Image image) {
            var myJson = new Root();
            myJson.Rate = image.Rate;
            if (image.SceneScanDone) {
                myJson.SceneScanDone = true;
                myJson.Scene = image.Scene;
            }
            if (image.FaceScanDone) {
                myJson.FaceScanDone = true;
                if (image.Faces != null && image.Faces.Any()) {
                    myJson.RegionInfo = new RegionInfo();
                    // System.Drawing.Image img = System.Drawing.Image.FromFile(image.FileName);
                    // Using ImageMagick for performance reading the image dimensions
                    var img = new MagickImageInfo(new FileInfo(image.FileName));
                    myJson.RegionInfo.AppliedToDimensions = new AppliedToDimensions();
                    myJson.RegionInfo.AppliedToDimensions.H = img.Height;
                    myJson.RegionInfo.AppliedToDimensions.W = img.Width;
                    myJson.RegionInfo.AppliedToDimensions.Unit = "pixel";
                    myJson.RegionInfo.RegionList = new List<RegionList>();
                    myJson.PersonInImage = new List<string>();
                    foreach (var face in image.Faces) {
                        var region = new RegionList();
                        region.Area = new Area();
                        region.Name = face.Person;
                        region.FaceConfidence = face.FaceConfidence;
                        region.PersonConfidence = face.PersonConfidence;
                        region.Area.X = (double)face.LTRB[0] / img.Width;
                        region.Area.Y = (double)face.LTRB[1] / img.Height;
                        region.Area.W = (double)(face.LTRB[2] - face.LTRB[0]) / img.Width;
                        region.Area.H = (double)(face.LTRB[3] - face.LTRB[1]) / img.Height;
                        myJson.RegionInfo.RegionList.Add(region);
                        myJson.PersonInImage.Add(face.Person);
                    }
                }
            }
            if (image.ObjectScanDone) {
                myJson.ObjectScanDone = true;
                if (image.Objects != null && image.Objects.Any()) {
                    myJson.ObjectsInImage = image.Objects;
                }
            }
            if (image.Tags != null && image.Tags.Any()) {
                myJson.Tags = image.Tags;
            }
            File.WriteAllText(GetJsonFileName(image), JsonConvert.SerializeObject(myJson));
        }


        private static string GetJsonFileName(DB.Image image) {
            var fileName = Path.GetFileNameWithoutExtension(image.FileName);
            var folder = Path.GetDirectoryName(image.FileName);
            return Path.Combine(folder, fileName + ".json");
        }
        #region Json classes
        public class AppliedToDimensions {
            public double H { get; set; }
            public string Unit { get; set; }
            public double W { get; set; }
        }

        public class Area {
            public double H { get; set; }
            public string Unit { get; set; } = "normalized";
            public double W { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        public class RegionInfo {
            public AppliedToDimensions AppliedToDimensions { get; set; }
            public List<RegionList> RegionList { get; set; }
        }

        public class RegionList {
            public Area Area { get; set; }
            public string Name { get; set; }
            public string Type { get; set; } = "Face";
            public float FaceConfidence { get; set; }
            public float PersonConfidence { get; set; }

        }

        public List<string> PersonInImage { get; set; }

        public class Root {
            public RegionInfo RegionInfo { get; set; }
            public List<string> PersonInImage { get; set; }
            public bool FaceScanDone { get; set; } = false;
            public bool SceneScanDone { get; set; } = false;
            public bool ObjectScanDone { get; set; } = false;
            public List<string> ObjectsInImage { get; set; } = new List<string>();
            public string Scene;
            public List<string> Tags { get; set; } = new List<string>();
            public int Rate { get; set; }
        }
        #endregion

    }
}
