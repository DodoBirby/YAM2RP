using System.Linq;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();
string sourceFolder = PromptChooseDirectory();
if (sourceFolder == null)
    throw new ScriptException("The import folder was not set.");

string scriptPath = Path.Combine(sourceFolder, "Scripts");
if (Directory.Exists(scriptPath))
{
    string[] dirFiles = Directory.GetFiles(scriptPath);
    if (dirFiles.Length == 0)
        throw new ScriptException("The selected folder is empty.");
    else if (!dirFiles.Any(x => x.EndsWith(".gml")))
        throw new ScriptException("The scripts folder doesn't contain any GML files.");

    SetProgressBar(null, "Importing Scripts...", 0, dirFiles.Length);
    StartProgressBarUpdater();

    SyncBinding("Strings, Code, CodeLocals, Scripts, GlobalInitScripts, GameObjects, Functions, Variables", true);
    await Task.Run(() => {
        foreach (string file in dirFiles)
        {
            IncrementProgress();

            ImportGMLFile(file, true, false, true);
        }
    });
    DisableAllSyncBindings();

    await StopProgressBarUpdater();
    HideProgressBar();
}