// css_ref System.Diagnostics.Process.dll
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PhotoOrganizer;
using PhotoOrganizer.ScriptController;


namespace CSharpScripting {
    public class ScriptDocumenter : BaseScript {
        //
        // Call this from a CSharpScript to document the BaseScript instances in this dll.
        // Each script will be documented in a html file in a subfolder called "Doc".
        // An index.html file with links to the generated html files is added to the rootfolder of the scripts. 
        // 
        enum Id { SourceFolder };
        public ScriptDocumenter() {
            InputVariables = new List<Parameter> {
                new Parameter {
                    Id = Id.SourceFolder,
                    ObjectType = typeof (DirectoryInfo),
                    Label = "Source folder",
                    DefaultValue = @"C:\Users\ThinkPad P50\source\repos\ReidarMidtun\PhotoOrganizerScripts\Scripts",
                    Description = "Folder with source code",
                    Mandatory = true
                }
            };
            Description = "Documents all scripts found in subfolders below the source folder.";
        }
        protected override async Task Execute() {
            string indexFile = null;
            try {
                var sFolder = GetObject<DirectoryInfo>(Id.SourceFolder);
                var textile = new StringBuilder($"h1. {sFolder.Name}\n");
                const string linkFmt = "* \"{0}\":{1}\n";
                var dir = GetObject<DirectoryInfo>(Id.SourceFolder);
                var n = 0;
                indexFile = Path.Combine(dir.FullName, "index.html");
                var refDlls = ScriptView.GetReferenceDlls();
                var docFolder = Path.Combine(sFolder.FullName, "Doc");
                if (!Directory.Exists(docFolder)) {
                    Directory.CreateDirectory(docFolder);
                }
                foreach (var csFile in sFolder.EnumerateFiles("*.cs").OrderBy(f => f.Name)) {
                    var className = Path.GetFileNameWithoutExtension(csFile.FullName);
                    string errors;
                    var assembly = typeof(BaseScript).Assembly;
                    Roslyn.Compiler.PreferredLibraryPath = Path.GetDirectoryName(assembly.Location);
                    var compileResult = Roslyn.Compiler.GetClassInstance(csFile, typeof(BaseScript), refDlls);
                    if (compileResult.ClassInstance == null) {
                        LogView.AppendText($"Error compiling: {csFile.FullName}");
                        LogView.AppendText(compileResult.ErrorMessage);
                        continue;
                    }

                    var scriptInstance = compileResult.ClassInstance as BaseScript;
                    if (scriptInstance != null && scriptInstance is BaseScript script) {
                        var docFile = Path.Combine(docFolder,
                            Path.GetFileNameWithoutExtension(csFile.FullName) + ".html");
                        var docGenerator = new Documenter(script, csFile.FullName, docFile);
                        File.WriteAllText(docFile, docGenerator.Html);
                        var link = Documenter.GetRelativePath(indexFile, docFile);
                        textile.Append(string.Format(linkFmt, Path.GetFileNameWithoutExtension(csFile.FullName), link));
                        n++;
                    } else {
                    }
                }

                File.WriteAllText(indexFile, Documenter.Textile2Html(textile.ToString()));
                if (indexFile != null) {
                    var options = new ProcessStartInfo(indexFile);
                    options.UseShellExecute = true;
                    Process.Start(options);
                }
            }
            catch(Exception ex) {
                LogView.AppendText($"The script {Name} failed.+n{ex.Message}");
            }

        }

    }
}
