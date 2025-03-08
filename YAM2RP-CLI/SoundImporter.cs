using UndertaleModLib;
using UndertaleModLib.Models;

namespace YAM2RP;

public class SoundImporter
{
	public static void ImportSounds(UndertaleData data, string soundPath)
	{
		foreach (var file in Directory.EnumerateFiles(soundPath, "*", SearchOption.AllDirectories))
		{
			var soundName = Path.GetFileNameWithoutExtension(file);
			var extension = Path.GetExtension(file);
			var fileName = Path.GetFileName(file);
			if (extension != ".ogg" && extension != ".wav")
			{
				continue;
			}
			var embedSound = extension == ".wav";
			var existingSound = data.Sounds.ByName(soundName);
			if (embedSound)
			{
				var soundData = new UndertaleEmbeddedAudio()
				{
					Data = File.ReadAllBytes(file)
				};
				data.EmbeddedAudio.Add(soundData);
				if (existingSound != null)
				{
					data.EmbeddedAudio.Remove(existingSound.AudioFile);
				}
			}
			if (existingSound != null)
			{
				existingSound.AudioFile = data.EmbeddedAudio.Last();
				existingSound.AudioID = data.EmbeddedAudio.Count - 1;
				return;
			}
			var newSound = new UndertaleSound()
			{
				Name = data.Strings.MakeString(soundName),
				Flags = UndertaleSound.AudioEntryFlags.Regular | (embedSound ? UndertaleSound.AudioEntryFlags.IsEmbedded : 0),
				Type = data.Strings.MakeString(extension),
				File = data.Strings.MakeString(fileName),
				Effects = 0,
				Volume = 1.0f,
				Pitch = 1.0f,
				AudioID = embedSound ? data.EmbeddedAudio.Count - 1 : -1,
				AudioFile = embedSound ? data.EmbeddedAudio.Last() : null,
				AudioGroup = null,
				GroupID = data.GetBuiltinSoundGroupID()
			};
			data.Sounds.Add(newSound);
		}
	}
}
