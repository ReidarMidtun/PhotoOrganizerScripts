using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks; 
using System.Windows.Forms;

using PhotoOrganizer;
using PhotoOrganizer.ScriptController;

namespace PhotoOrganizerScripts {
    public class Test : BaseScript {

        public Test() {
            Description = "Test code.";
        }

        protected override async Task<object> Execute() {
            var text = SelectedFolder;

            var pl = SelectedPlayList;

            text = SelectedObject;

            text = SelectedScene;

            text = SelectedTag;

            text = SelectedPerson;

            var prop = SelectedProperty;

            var imageList = ThumbnailViewSelectedImages;

            imageList = FileViewSelectedImages;

            return null;
        }
    }
}
