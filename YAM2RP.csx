EnsureDataLoaded();
string sourcePath = PromptChooseDirectory();
if (sourcePath == null)
{
    throw new ScriptException("The source code path was not set.");
}
string gmlPath = Path.Combine(sourcePath, "ReplacementScripts");
foreach (string file in Directory.GetFiles(gmlPath))
{
    ImportGMLFile(file, true, false, true);
}