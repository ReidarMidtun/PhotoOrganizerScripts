// These are needed due to compiling outside VS (automatically added by VS)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using PhotoOrganizer.ScriptController;
using PhotoOrganizer;


namespace PhotoOrganizerScripts {
    class FixRelFileName : BaseScript {

        public FixRelFileName() {
            Description = "Update relative file name.";
        }

        protected override object Execute() {
            var config = DB.GetConfiguration();
            config.Sources[1] = "C:\\Tmp\\TestBilder";
            DB.Update(new List<DB.Configuration> { config });
            // Make a relative filename from the original absolute full file name
            //var config = DB.GetConfiguration();
            //var sources = config.Sources;
            //var images = DB.Image.GetAllImages();
            //var toDelete = new List<DB.Image>();
            //foreach (var image in images) {
            //    var sourceFolder = image.Source <= sources.Count ? sources[image.Source] : null;
            //    if (!sourceFolder.EndsWith("\\")) {
            //        sourceFolder += "\\";
            //    }
            //    if (sourceFolder != null) {
            //        if (image.FileName.StartsWith(sourceFolder)) {
            //            image.RelFileName = image.FileName.Substring(sourceFolder.Length);
            //        } else { // Invalid source
            //            toDelete.Add(image);
            //        }
            //    } else {
            //        LogView.AppendText($"Error image: {image.FileName}");
            //        break;
            //    }
            //}
            //DB.Update(images);
            //DB.Delete(toDelete);

            // Remove a field/column from a collection
            //var db = DB.DataBase;
            //var col = db.GetCollection("Images");
            //var docs = col.FindAll().ToList();
            //foreach (var doc in docs) {
            //    doc.Remove("FileName");
            //    col.Update(doc);
            //}

            return null;
        }
    }
}
