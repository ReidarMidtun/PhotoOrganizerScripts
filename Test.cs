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
    class Test : BaseScript {

        public Test() {
            Description = "Update relative file name.";
        }

        protected override object Execute() {
            var config = DB.GetConfiguration();
            var col = DB.GetFaceSimilarityCollection(DB.DataBase);

            var fs = new DB.FaceSimilarity(Guid.NewGuid(), Guid.NewGuid(), 0.777f);

            DB.Insert(new[] { fs });

            // No idea why this fails!!!!!!!!!!!!!!!!!!!!
            var allSimilarities = col.FindAll().ToList();
            foreach (var similarity in allSimilarities) {
                LogView.AppendText($"{similarity.Name}, {similarity.FaceAId}, {similarity.FaceBId}, {similarity.Similarity}");
            }

            return null;
        }
    }
}
