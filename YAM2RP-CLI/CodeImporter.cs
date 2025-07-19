using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace YAM2RP;

public class CodeImporter
{
	public static void ImportCodeNames(UndertaleData data, string scriptPath)
	{
		foreach (var file in Directory.EnumerateFiles(scriptPath, "gml_Script*.gml", SearchOption.AllDirectories))
		{
			var fileName = Path.GetFileNameWithoutExtension(file);
			var scriptName = fileName[11..];
			var script = data.Scripts.ByName(scriptName);
			if (script == null)
			{
				var scr = new UndertaleScript
				{
					Name = data.Strings.MakeString(scriptName)
				};
				data.Scripts.Add(scr);
			}
		}
		foreach (var file in Directory.EnumerateFiles(scriptPath, "*.gml", SearchOption.AllDirectories))
		{
			var fileName = Path.GetFileNameWithoutExtension(file);
			GetOrCreateCode(data, fileName);
		}
	}

	static UndertaleCode GetOrCreateCode(UndertaleData data, string codeName)
	{
		var code = data.Code.ByName(codeName);
		if (code == null)
		{
			code = new UndertaleCode();
			code.Name = data.Strings.MakeString(codeName);
			data.Code.Add(code);
		}
		CreateCodeLocalIfNeeded(data, codeName, code);
		return code;
	}

	static void CreateCodeLocalIfNeeded(UndertaleData data, string codeName, UndertaleCode code)
	{
		if (data.GeneralInfo.BytecodeVersion <= 14 || data.CodeLocals.ByName(codeName) != null)
		{
			return;
		}
		var locals = new UndertaleCodeLocals();
		locals.Name = data.Strings.MakeString(codeName);

		var argsLocal = new UndertaleCodeLocals.LocalVar();
		argsLocal.Name = data.Strings.MakeString("arguments");
		argsLocal.Index = 0;

		locals.Locals.Add(argsLocal);

		code.LocalsCount = 1;
		data.CodeLocals.Add(locals);
	}

	static void LinkScript(UndertaleData data, string codeName, UndertaleCode code)
	{
		var script = data.Scripts.ByName(codeName[11..]);
		if (script == null)
		{
			throw new Exception("This shouldn't be possible to hit");
		}
		script.Code = code;
	}

	static void ImportGMLFile(CompileGroup group, UndertaleData data, string fileName)
	{
		var codeName = Path.GetFileNameWithoutExtension(fileName);
		var codeText = File.ReadAllText(fileName);
		var code = GetOrCreateCode(data, codeName);
		if (codeName.StartsWith("gml_Script"))
		{
			LinkScript(data, codeName, code);
		}
		group.QueueCodeReplace(code, codeText);
		
    }

	public static void ImportCode(UndertaleData data, string codePath)
	{
		var group = new CompileGroup(data);
		foreach (var file in Directory.EnumerateFiles(codePath, "*.gml", SearchOption.AllDirectories))
		{
			ImportGMLFile(group, data, file);
		}
		group.Compile();
	}
}
