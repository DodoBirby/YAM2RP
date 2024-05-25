using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using System.Drawing;
using System.Collections;
using System.Text.RegularExpressions;
using UndertaleModLib.Util;
using System.Text.Json;

const int CollisionEventIndex = 4;

EnsureDataLoaded();

ScriptMessage("Select source folder");
string sourceFolder = PromptChooseDirectory();
if (sourceFolder == null)
    throw new ScriptException("The source folder was not set.");

string soundPath = Path.Combine(sourceFolder, "Sounds");
string graphicsPath = Path.Combine(sourceFolder, "Graphics");
string maskPath = Path.Combine(sourceFolder, "Masks");
string objectPath = Path.Combine(sourceFolder, "Objects");
string roomPath = Path.Combine(sourceFolder, "Rooms");
string scriptPath = Path.Combine(sourceFolder, "Code");

string nameReplacePath = Path.Combine(sourceFolder, "Replace.txt");
string spriteOptionsPath = Path.Combine(sourceFolder, "SpriteOptions.txt");

HashSet<uint> usedInstanceIDs = new HashSet<uint>();
HashSet<uint> usedTileIDs = new HashSet<uint>();

if (File.Exists(nameReplacePath)) {
    ReplaceNames(File.ReadAllLines(nameReplacePath));
    ScriptMessage("Replaced resource names");
}

if (Directory.Exists(graphicsPath))
{
    ImportGraphics();
    ScriptMessage("Imported graphics");
}

if (Directory.Exists(maskPath)) {
    ImportMasks();
    ScriptMessage("Imported collision masks");
}

if (File.Exists(spriteOptionsPath)) {
    ImportSpriteOptions(File.ReadAllLines(spriteOptionsPath));
    ScriptMessage("Changed Sprite Options");
}

if (Directory.Exists(soundPath)) {
    foreach (string file in Directory.GetFiles(soundPath, "*", SearchOption.AllDirectories)) {
        ImportSound(file);
    }
    ScriptMessage("Imported sounds");
}

// Objects and rooms can be referenced by code so they need to be imported before code is compiled
// But rooms and objects can reference code by name in their data so we just add entries without data here
// This also allows object parent references between objects to work since all new objects are in Data before parent references are filled in
if (Directory.Exists(objectPath)) {
    foreach (string file in Directory.GetFiles(objectPath, "*.json", SearchOption.AllDirectories)) {
        ReadObjectName(file);
    }
    ScriptMessage("Imported object names");
}
if (Directory.Exists(roomPath)) {
    foreach (string file in Directory.GetFiles(roomPath, "*.json", SearchOption.AllDirectories)) {
        ReadRoomName(file);
    }
    ScriptMessage("Imported room names");
}
if (Directory.Exists(scriptPath))
{
    ImportScripts();
    ScriptMessage("Imported code");
}
// Fill in object and room data now that code is compiled with proper references
if (Directory.Exists(objectPath)) {
    foreach (string file in Directory.GetFiles(objectPath, "*.json", SearchOption.AllDirectories)) {
        ImportObject(file);
        
    }
    ScriptMessage("Imported objects");
}
if (Directory.Exists(roomPath)) {
    foreach (string file in Directory.GetFiles(roomPath, "*.json", SearchOption.AllDirectories)) {
        ImportRoom(file);
    }
    ScriptMessage("Imported rooms");
}
ScriptMessage("Patching Complete!");


public class TextureInfo
{
    public string Source;
    public int Width;
    public int Height;
}

public enum SpriteType
{
    Sprite,
    Background,
    Font,
    Unknown
}


public enum SplitType
{
    Horizontal,
    Vertical,
}

public enum BestFitHeuristic
{
    Area,
    MaxOneAxis,
}

public class Node
{
    public Rectangle Bounds;
    public TextureInfo Texture;
    public SplitType SplitType;
}

public class Atlas
{
    public int Width;
    public int Height;
    public List<Node> Nodes;
}
public class Packer
{
    public List<TextureInfo> SourceTextures;
    public StringWriter Log;
    public StringWriter Error;
    public int Padding;
    public int AtlasSize;
    public bool DebugMode;
    public BestFitHeuristic FitHeuristic;
    public List<Atlas> Atlasses;

    public Packer()
    {
        SourceTextures = new List<TextureInfo>();
        Log = new StringWriter();
        Error = new StringWriter();
    }

    public void Process(string _SourceDir, string _Pattern, int _AtlasSize, int _Padding, bool _DebugMode)
    {
        Padding = _Padding;
        AtlasSize = _AtlasSize;
        DebugMode = _DebugMode;
        //1: scan for all the textures we need to pack
        ScanForTextures(_SourceDir, _Pattern);
        List<TextureInfo> textures = new List<TextureInfo>();
        textures = SourceTextures.ToList();
        //2: generate as many atlasses as needed (with the latest one as small as possible)
        Atlasses = new List<Atlas>();
        while (textures.Count > 0)
        {
            Atlas atlas = new Atlas();
            atlas.Width = _AtlasSize;
            atlas.Height = _AtlasSize;
            List<TextureInfo> leftovers = LayoutAtlas(textures, atlas);
            if (leftovers.Count == 0)
            {
                // we reached the last atlas. Check if this last atlas could have been twice smaller
                while (leftovers.Count == 0)
                {
                    atlas.Width /= 2;
                    atlas.Height /= 2;
                    leftovers = LayoutAtlas(textures, atlas);
                }
                // we need to go 1 step larger as we found the first size that is to small
                atlas.Width *= 2;
                atlas.Height *= 2;
                leftovers = LayoutAtlas(textures, atlas);
            }
            Atlasses.Add(atlas);
            textures = leftovers;
        }
    }

    public void SaveAtlasses(string _Destination)
    {
        int atlasCount = 0;
        string prefix = _Destination.Replace(Path.GetExtension(_Destination), "");
        string descFile = _Destination;
        StreamWriter tw = new StreamWriter(_Destination);
        tw.WriteLine("source_tex, atlas_tex, x, y, width, height");
        foreach (Atlas atlas in Atlasses)
        {
            string atlasName = String.Format(prefix + "{0:000}" + ".png", atlasCount);
            //1: Save images
            Image img = CreateAtlasImage(atlas);
            img.Save(atlasName, System.Drawing.Imaging.ImageFormat.Png);
            //2: save description in file
            foreach (Node n in atlas.Nodes)
            {
                if (n.Texture != null)
                {
                    tw.Write(n.Texture.Source + ", ");
                    tw.Write(atlasName + ", ");
                    tw.Write((n.Bounds.X).ToString() + ", ");
                    tw.Write((n.Bounds.Y).ToString() + ", ");
                    tw.Write((n.Bounds.Width).ToString() + ", ");
                    tw.WriteLine((n.Bounds.Height).ToString());
                }
            }
            ++atlasCount;
        }
        tw.Close();
        tw = new StreamWriter(prefix + ".log");
        tw.WriteLine("--- LOG -------------------------------------------");
        tw.WriteLine(Log.ToString());
        tw.WriteLine("--- ERROR -----------------------------------------");
        tw.WriteLine(Error.ToString());
        tw.Close();
    }

    private void ScanForTextures(string _Path, string _Wildcard)
    {
        DirectoryInfo di = new DirectoryInfo(_Path);
        FileInfo[] files = di.GetFiles(_Wildcard, SearchOption.AllDirectories);
        foreach (FileInfo fi in files)
        {
            Image img = Image.FromFile(fi.FullName);
            if (img != null)
            {
                if (img.Width <= AtlasSize && img.Height <= AtlasSize)
                {
                    TextureInfo ti = new TextureInfo();

                    ti.Source = fi.FullName;
                    ti.Width = img.Width;
                    ti.Height = img.Height;

                    SourceTextures.Add(ti);

                    Log.WriteLine("Added " + fi.FullName);
                }
                else
                {
                    Error.WriteLine(fi.FullName + " is too large to fix in the atlas. Skipping!");
                }
            }
        }
    }

    private void HorizontalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
    {
        Node n1 = new Node();
        n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
        n1.Bounds.Y = _ToSplit.Bounds.Y;
        n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
        n1.Bounds.Height = _Height;
        n1.SplitType = SplitType.Vertical;
        Node n2 = new Node();
        n2.Bounds.X = _ToSplit.Bounds.X;
        n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
        n2.Bounds.Width = _ToSplit.Bounds.Width;
        n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
        n2.SplitType = SplitType.Horizontal;
        if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
            _List.Add(n1);
        if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
            _List.Add(n2);
    }

    private void VerticalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
    {
        Node n1 = new Node();
        n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
        n1.Bounds.Y = _ToSplit.Bounds.Y;
        n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
        n1.Bounds.Height = _ToSplit.Bounds.Height;
        n1.SplitType = SplitType.Vertical;
        Node n2 = new Node();
        n2.Bounds.X = _ToSplit.Bounds.X;
        n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
        n2.Bounds.Width = _Width;
        n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
        n2.SplitType = SplitType.Horizontal;
        if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
            _List.Add(n1);
        if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
            _List.Add(n2);
    }

    private TextureInfo FindBestFitForNode(Node _Node, List<TextureInfo> _Textures)
    {
        TextureInfo bestFit = null;
        float nodeArea = _Node.Bounds.Width * _Node.Bounds.Height;
        float maxCriteria = 0.0f;
        foreach (TextureInfo ti in _Textures)
        {
            switch (FitHeuristic)
            {
                // Max of Width and Height ratios
                case BestFitHeuristic.MaxOneAxis:
                    if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height)
                    {
                        float wRatio = (float)ti.Width / (float)_Node.Bounds.Width;
                        float hRatio = (float)ti.Height / (float)_Node.Bounds.Height;
                        float ratio = wRatio > hRatio ? wRatio : hRatio;
                        if (ratio > maxCriteria)
                        {
                            maxCriteria = ratio;
                            bestFit = ti;
                        }
                    }
                    break;
                // Maximize Area coverage
                case BestFitHeuristic.Area:
                    if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height)
                    {
                        float textureArea = ti.Width * ti.Height;
                        float coverage = textureArea / nodeArea;
                        if (coverage > maxCriteria)
                        {
                            maxCriteria = coverage;
                            bestFit = ti;
                        }
                    }
                    break;
            }
        }
        return bestFit;
    }

    private List<TextureInfo> LayoutAtlas(List<TextureInfo> _Textures, Atlas _Atlas)
    {
        List<Node> freeList = new List<Node>();
        List<TextureInfo> textures = new List<TextureInfo>();
        _Atlas.Nodes = new List<Node>();
        textures = _Textures.ToList();
        Node root = new Node();
        root.Bounds.Size = new Size(_Atlas.Width, _Atlas.Height);
        root.SplitType = SplitType.Horizontal;
        freeList.Add(root);
        while (freeList.Count > 0 && textures.Count > 0)
        {
            Node node = freeList[0];
            freeList.RemoveAt(0);
            TextureInfo bestFit = FindBestFitForNode(node, textures);
            if (bestFit != null)
            {
                if (node.SplitType == SplitType.Horizontal)
                {
                    HorizontalSplit(node, bestFit.Width, bestFit.Height, freeList);
                }
                else
                {
                    VerticalSplit(node, bestFit.Width, bestFit.Height, freeList);
                }
                node.Texture = bestFit;
                node.Bounds.Width = bestFit.Width;
                node.Bounds.Height = bestFit.Height;
                textures.Remove(bestFit);
            }
            _Atlas.Nodes.Add(node);
        }
        return textures;
    }

    private Image CreateAtlasImage(Atlas _Atlas)
    {
        Image img = new Bitmap(_Atlas.Width, _Atlas.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        Graphics g = Graphics.FromImage(img);
        foreach (Node n in _Atlas.Nodes)
        {
            if (n.Texture != null)
            {
                Image sourceImg = Image.FromFile(n.Texture.Source);
                g.DrawImage(sourceImg, n.Bounds);
            }
        }
        // DPI FIX START
        Bitmap ResolutionFix = new Bitmap(img);
        ResolutionFix.SetResolution(96.0F, 96.0F);
        Image img2 = ResolutionFix;
        return img2;
        // DPI FIX END
    }
}


// Taken from ImportGraphics by Samuel Roy
public void ImportGraphics() {

    // "(.+?)" - match everything; "?" = match as few characters as possible.
    // "(?:_(-*\d+))*" - an underscore + (optional minus + several digits);
    // "?:" = don't make a separate group for the whole part, "*" = make this part optional.
    Regex sprFrameRegex = new(@"^(.+?)(?:_(-*\d+))*$", RegexOptions.Compiled);
    string importFolder = CheckValidity();
    string packDir = Path.Combine(ExePath, "Packager");
    Directory.CreateDirectory(packDir);

    string sourcePath = importFolder;
    string searchPattern = "*.png";
    string outName = Path.Combine(packDir, "atlas.txt");
    int textureSize = 2048;
    int PaddingValue = 2;
    bool debug = false;
    Packer packer = new Packer();
    packer.Process(sourcePath, searchPattern, textureSize, PaddingValue, debug);
    packer.SaveAtlasses(outName);

    int lastTextPage = Data.EmbeddedTextures.Count - 1;
    int lastTextPageItem = Data.TexturePageItems.Count - 1;

    // Import everything into UMT
    string prefix = outName.Replace(Path.GetExtension(outName), "");
    int atlasCount = 0;
    foreach (Atlas atlas in packer.Atlasses)
    {
        string atlasName = Path.Combine(packDir, String.Format(prefix + "{0:000}" + ".png", atlasCount));
        Bitmap atlasBitmap = new Bitmap(atlasName);
        UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
        texture.Name = new UndertaleString("Texture " + ++lastTextPage);
        texture.TextureData.TextureBlob = File.ReadAllBytes(atlasName);
        Data.EmbeddedTextures.Add(texture);
        foreach (Node n in atlas.Nodes)
        {
            if (n.Texture != null)
            {
                // Initalize values of this texture
                UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem();
                texturePageItem.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
                texturePageItem.SourceX = (ushort)n.Bounds.X;
                texturePageItem.SourceY = (ushort)n.Bounds.Y;
                texturePageItem.SourceWidth = (ushort)n.Bounds.Width;
                texturePageItem.SourceHeight = (ushort)n.Bounds.Height;
                texturePageItem.TargetX = 0;
                texturePageItem.TargetY = 0;
                texturePageItem.TargetWidth = (ushort)n.Bounds.Width;
                texturePageItem.TargetHeight = (ushort)n.Bounds.Height;
                texturePageItem.BoundingWidth = (ushort)n.Bounds.Width;
                texturePageItem.BoundingHeight = (ushort)n.Bounds.Height;
                texturePageItem.TexturePage = texture;

                // Add this texture to UMT
                Data.TexturePageItems.Add(texturePageItem);

                // String processing
                string stripped = Path.GetFileNameWithoutExtension(n.Texture.Source);

                SpriteType spriteType = GetSpriteType(n.Texture.Source);

                if ((spriteType == SpriteType.Unknown) || (spriteType == SpriteType.Font))
                {
                    spriteType = SpriteType.Sprite;
                }

                setTextureTargetBounds(texturePageItem, stripped, n);


                if (spriteType == SpriteType.Background)
                {
                    UndertaleBackground background = Data.Backgrounds.ByName(stripped);
                    if (background != null)
                    {
                        background.Texture = texturePageItem;
                    }
                    else
                    {
                        // No background found, let's make one
                        UndertaleString backgroundUTString = Data.Strings.MakeString(stripped);
                        UndertaleBackground newBackground = new UndertaleBackground();
                        newBackground.Name = backgroundUTString;
                        newBackground.Transparent = false;
                        newBackground.Preload = false;
                        newBackground.Texture = texturePageItem;
                        Data.Backgrounds.Add(newBackground);
                    }
                }
                else if (spriteType == SpriteType.Sprite)
                {
                    // Get sprite to add this texture to
                    string spriteName;
                    int frame = 0;
                    try
                    {
                        var spriteParts = sprFrameRegex.Match(stripped);
                        spriteName = spriteParts.Groups[1].Value;
                        Int32.TryParse(spriteParts.Groups[2].Value, out frame);
                    }
                    catch (Exception e)
                    {
                        ScriptMessage("Error: Image " + stripped + " has an invalid name. Skipping...");
                        continue;
                    }
                    UndertaleSprite sprite = null;
                    sprite = Data.Sprites.ByName(spriteName);

                    // Create TextureEntry object
                    UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
                    texentry.Texture = texturePageItem;

                    // Set values for new sprites
                    if (sprite == null)
                    {
                        UndertaleString spriteUTString = Data.Strings.MakeString(spriteName);
                        UndertaleSprite newSprite = new UndertaleSprite();
                        newSprite.Name = spriteUTString;
                        newSprite.Width = (uint)n.Bounds.Width;
                        newSprite.Height = (uint)n.Bounds.Height;
                        newSprite.MarginLeft = 0;
                        newSprite.MarginRight = n.Bounds.Width - 1;
                        newSprite.MarginTop = 0;
                        newSprite.MarginBottom = n.Bounds.Height - 1;
                        newSprite.OriginX = 0;
                        newSprite.OriginY = 0;
                        if (frame > 0)
                        {
                            for (int i = 0; i < frame; i++)
                                newSprite.Textures.Add(null);
                        }
                        newSprite.CollisionMasks.Add(newSprite.NewMaskEntry());
                        Rectangle bmpRect = new Rectangle(n.Bounds.X, n.Bounds.Y, n.Bounds.Width, n.Bounds.Height);
                        System.Drawing.Imaging.PixelFormat format = atlasBitmap.PixelFormat;
                        Bitmap cloneBitmap = atlasBitmap.Clone(bmpRect, format);
                        int width = ((n.Bounds.Width + 7) / 8) * 8;
                        BitArray maskingBitArray = new BitArray(width * n.Bounds.Height);
                        for (int y = 0; y < n.Bounds.Height; y++)
                        {
                            for (int x = 0; x < n.Bounds.Width; x++)
                            {
                                Color pixelColor = cloneBitmap.GetPixel(x, y);
                                maskingBitArray[y * width + x] = (pixelColor.A > 0);
                            }
                        }
                        BitArray tempBitArray = new BitArray(width * n.Bounds.Height);
                        for (int i = 0; i < maskingBitArray.Length; i += 8)
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                tempBitArray[j + i] = maskingBitArray[-(j - 7) + i];
                            }
                        }
                        int numBytes;
                        numBytes = maskingBitArray.Length / 8;
                        byte[] bytes = new byte[numBytes];
                        tempBitArray.CopyTo(bytes, 0);
                        for (int i = 0; i < bytes.Length; i++)
                            newSprite.CollisionMasks[0].Data[i] = bytes[i];
                        newSprite.Textures.Add(texentry);
                        Data.Sprites.Add(newSprite);
                        continue;
                    }
                    if (frame > sprite.Textures.Count - 1)
                    {
                        while (frame > sprite.Textures.Count - 1)
                        {
                            sprite.Textures.Add(texentry);
                        }
                        continue;
                    }
                    sprite.Textures[frame] = texentry;
                }
            }
        }
        // Increment atlas
        atlasCount++;
    }

    void setTextureTargetBounds(UndertaleTexturePageItem tex, string textureName, Node n)
    {
        tex.TargetX = 0;
        tex.TargetY = 0;
        tex.TargetWidth = (ushort)n.Bounds.Width;
        tex.TargetHeight = (ushort)n.Bounds.Height;
    }

    SpriteType GetSpriteType(string path)
    {
        string folderPath = Path.GetDirectoryName(path);
        string folderName = new DirectoryInfo(folderPath).Name;
        string lowerName = folderName.ToLower();

        if (lowerName == "backgrounds" || lowerName == "background")
        {
            return SpriteType.Background;
        }
        else if (lowerName == "fonts" || lowerName == "font")
        {
            return SpriteType.Font;
        }
        else if (lowerName == "sprites" || lowerName == "sprite")
        {
            return SpriteType.Sprite;
        }
        return SpriteType.Unknown;
    }
    string CheckValidity()
    {
        /*bool recursiveCheck = ScriptQuestion(@"This script imports all sprites in all subdirectories recursively.
If an image file is in a folder named ""Backgrounds"", then the image will be imported as a background.
Otherwise, the image will be imported as a sprite.
Do you want to continue?");
        if (!recursiveCheck)
            throw new ScriptException("Script cancelled.");*/

        // Get import folder
        string importFolder = graphicsPath;
        if (importFolder == null)
            throw new ScriptException("The import folder was not set.");

        //Stop the script if there's missing sprite entries or w/e.
        bool hadMessage = false;
        string currSpriteName = null;
        string[] dirFiles = Directory.GetFiles(importFolder, "*.png", SearchOption.AllDirectories);
        foreach (string file in dirFiles)
        {
            string FileNameWithExtension = Path.GetFileName(file);
            string stripped = Path.GetFileNameWithoutExtension(file);
            string spriteName = "";

            SpriteType spriteType = GetSpriteType(file);

            // Dodo: Previously there was a question to ask if the user wanted to import unknowns as sprites, I've just defaulted to yes
            if ((spriteType != SpriteType.Sprite) && (spriteType != SpriteType.Background))
            {
                spriteType = SpriteType.Sprite;
            }

            // Check for duplicate filenames
            string[] dupFiles = Directory.GetFiles(importFolder, FileNameWithExtension, SearchOption.AllDirectories);
            if (dupFiles.Length > 1)
                throw new ScriptException("Duplicate file detected. There are " + dupFiles.Length + " files named: " + FileNameWithExtension);

            // Sprites can have multiple frames! Do some sprite-specific checking.
            if (spriteType == SpriteType.Sprite)
            {
                var spriteParts = sprFrameRegex.Match(stripped);
                // Allow sprites without underscores
                if (!spriteParts.Groups[2].Success)
                    continue;

                spriteName = spriteParts.Groups[1].Value;

                if (!Int32.TryParse(spriteParts.Groups[2].Value, out int frame))
                    throw new ScriptException(spriteName + " has an invalid frame index.");
                if (frame < 0)
                    throw new ScriptException(spriteName + " is using an invalid numbering scheme. The script has stopped for your own protection.");

                // If it's not a first frame of the sprite
                if (spriteName == currSpriteName)
                    continue;

                string[][] spriteFrames = Directory.GetFiles(importFolder, $"{spriteName}_*.png", SearchOption.AllDirectories)
                                                    .Select(x =>
                                                    {
                                                        var match = sprFrameRegex.Match(Path.GetFileNameWithoutExtension(x));
                                                        if (match.Groups[2].Success)
                                                            return new string[] { match.Groups[1].Value, match.Groups[2].Value };
                                                        else
                                                            return null;
                                                    })
                                                    .OfType<string[]>().ToArray();
                if (spriteFrames.Length == 1)
                {
                    currSpriteName = null;
                    continue;
                }

                int[] frameIndexes = spriteFrames.Select(x =>
                {
                    if (Int32.TryParse(x[1], out int frame))
                        return (int?)frame;
                    else
                        return null;
                }).OfType<int?>().Cast<int>().OrderBy(x => x).ToArray();
                if (frameIndexes.Length == 1)
                {
                    currSpriteName = null;
                    continue;
                }

                for (int i = 0; i < frameIndexes.Length - 1; i++)
                {
                    int num = frameIndexes[i];
                    int nextNum = frameIndexes[i + 1];

                    if (nextNum - num > 1)
                        throw new ScriptException(spriteName + " is missing one or more indexes.\nThe detected missing index is: " + (num + 1));
                }

                currSpriteName = spriteName;
            }
        }
        return importFolder;
    }
}

// Taken from ImportGML 
// Credits:
// Script by Jockeholm based off of a script by Kneesnap.
// Major help and edited by Samuel Roy
public void ImportScripts() {
    string[] dirFiles = Directory.GetFiles(scriptPath, "*.gml", SearchOption.AllDirectories);
    if (dirFiles.Length == 0)
        throw new ScriptException("The selected folder is empty.");
    else if (!dirFiles.Any(x => x.EndsWith(".gml")))
        throw new ScriptException("The scripts folder doesn't contain any GML files.");

    foreach (string file in dirFiles)
    {
        IncrementProgress();
        if (file.StartsWith("gml_Script")) {
            ImportGMLFile(file, throwOnError: true);
        } else {
            ImportGMLFile(file, doParse: false, throwOnError: true);
        }
    }
}

// Taken from ImportMasks by Grossley
public void ImportMasks() {
    string importFolder = maskPath;
    if (importFolder == null)
        throw new ScriptException("The import folder was not set.");

    string[] dirFiles = Directory.GetFiles(importFolder, "*.png", SearchOption.AllDirectories);

    //Stop the script if there's missing sprite entries or w/e.
    foreach (string file in dirFiles)
    {
        string FileNameWithExtension = Path.GetFileName(file);
        if (!FileNameWithExtension.EndsWith(".png"))
            continue; // Restarts loop if file is not a valid mask asset.
        string stripped = Path.GetFileNameWithoutExtension(file);
        int lastUnderscore = stripped.LastIndexOf('_');
        string spriteName = "";
        try
        {
            spriteName = stripped.Substring(0, lastUnderscore);
        }
        catch
        {
            throw new ScriptException("Getting the sprite name of " + FileNameWithExtension + " failed.");
        }
        if (Data.Sprites.ByName(spriteName) == null) // Reject non-existing sprites
        {
            throw new ScriptException(FileNameWithExtension + " could not be imported as the sprite " + spriteName + " does not exist.");
        }
        using (Image img = Image.FromFile(file))
        {
            if ((Data.Sprites.ByName(spriteName).Width != (uint)img.Width) || (Data.Sprites.ByName(spriteName).Height != (uint)img.Height))
                throw new ScriptException(FileNameWithExtension + " is not the proper size to be imported! Please correct this before importing! The proper dimensions are width: " + Data.Sprites.ByName(spriteName).Width.ToString() + " px, height: " + Data.Sprites.ByName(spriteName).Height.ToString() + " px.");
        }

        Int32 validFrameNumber = 0;
        try
        {
            validFrameNumber = Int32.Parse(stripped.Substring(lastUnderscore + 1));
        }
        catch
        {
            throw new ScriptException("The index of " + FileNameWithExtension + " could not be determined.");
        }
        int frame = 0;
        try
        {
            frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
        }
        catch
        {
            throw new ScriptException(FileNameWithExtension + " is using letters instead of numbers. The script has stopped for your own protection.");
        }
        int prevframe = 0;
        if (frame != 0)
        {
            prevframe = (frame - 1);
        }
        if (frame < 0)
        {
            throw new ScriptException(spriteName + " is using an invalid numbering scheme. The script has stopped for your own protection.");
        }
        var prevFrameName = spriteName + "_" + prevframe.ToString() + ".png";
        string[] previousFrameFiles = Directory.GetFiles(importFolder, prevFrameName);
        if (previousFrameFiles.Length < 1)
            throw new ScriptException(spriteName + " is missing one or more indexes. The detected missing index is: " + prevFrameName);
    }

    foreach (string file in dirFiles)
    {

        string FileNameWithExtension = Path.GetFileName(file);
        if (!FileNameWithExtension.EndsWith(".png"))
            continue; // Restarts loop if file is not a valid mask asset.
        string stripped = Path.GetFileNameWithoutExtension(file);
        int lastUnderscore = stripped.LastIndexOf('_');
        string spriteName = stripped.Substring(0, lastUnderscore);
        int frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
        UndertaleSprite sprite = Data.Sprites.ByName(spriteName);
        int collision_mask_count = sprite.CollisionMasks.Count;
        while (collision_mask_count <= frame)
        {
            sprite.CollisionMasks.Add(sprite.NewMaskEntry());
            collision_mask_count += 1;
        }
        try
        {
            sprite.CollisionMasks[frame].Data = TextureWorker.ReadMaskData(file);
        }
        catch
        {
            throw new ScriptException(FileNameWithExtension + " has an error that prevents its import and so the operation has been aborted! Please correct this before trying again!");
        }
    }
}

// Taken from ImportGameObject by SolventMercury
public void ReadObjectName(string filePath) {
    FileStream stream = File.OpenRead(filePath);
    byte[] jsonUtf8Bytes = new byte[stream.Length];

    stream.Read(jsonUtf8Bytes, 0, jsonUtf8Bytes.Length);
    stream.Close();

    JsonReaderOptions options = new JsonReaderOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    Utf8JsonReader reader = new Utf8JsonReader(jsonUtf8Bytes, options);

    ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
    AddObjectIfNewName(ref reader);

    void ReadAnticipateJSONObject(ref Utf8JsonReader reader, JsonTokenType allowedTokenType)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
                continue;
            if (reader.TokenType == allowedTokenType)
                return;
            throw new ScriptException($"ERROR: Unexpected token type. Expected {allowedTokenType} - found {reader.TokenType}");
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
    }

    string ReadString(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.String: return reader.GetString();
                case JsonTokenType.Null: return null;
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected String - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
    }

    void AddObjectIfNewName(ref Utf8JsonReader reader)
    {
        string name = ReadString(ref reader);
        if (name == null) throw new ScriptException("ERROR: Object name was null - object name must be defined!");
        if (Data.GameObjects.ByName(name) != null)
        {
            return;
        }
        else
        {
            UndertaleGameObject newGameObject = new UndertaleGameObject();
            newGameObject.Name = new UndertaleString(name);
            Data.Strings.Add(newGameObject.Name);
            Data.GameObjects.Add(newGameObject);
        }
    }
}

// Taken from ImportRoom_v3 by SolventMercury and AwfulNasty
public void ReadRoomName(string filePath) {
    FileStream stream = File.OpenRead(filePath);
    byte[] jsonUtf8Bytes = new byte[stream.Length];

    stream.Read(jsonUtf8Bytes, 0, jsonUtf8Bytes.Length);
    stream.Close();

    JsonReaderOptions options = new JsonReaderOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    Utf8JsonReader reader = new Utf8JsonReader(jsonUtf8Bytes, options);

    ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);

    AddRoomIfNewName(ref reader);

    void AddRoomIfNewName(ref Utf8JsonReader reader)
    {
        string name = ReadString(ref reader);
        if (name == null)
            throw new ScriptException("ERROR: Object name was null - object name must be defined!");

        if (Data.Rooms.ByName(name) != null)
        {
            return;
        }
        else
        {
            UndertaleRoom newRoom = new UndertaleRoom();
            newRoom.Name = new UndertaleString(name);
            Data.Strings.Add(newRoom.Name);
            Data.Rooms.Add(newRoom);
        }
    }

    string ReadString(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.String: return reader.GetString();
                case JsonTokenType.Null: return null;
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected String - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
    }

    void ReadAnticipateJSONObject(ref Utf8JsonReader reader, JsonTokenType allowedTokenType)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
                continue;
            if (reader.TokenType == allowedTokenType)
                return;
            throw new ScriptException($"ERROR: Unexpected token type. Expected {allowedTokenType} - found {reader.TokenType}");
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
    }
}

public void ImportObject(string filePath) {
    FileStream stream = File.OpenRead(filePath);
    byte[] jsonUtf8Bytes = new byte[stream.Length];

    stream.Read(jsonUtf8Bytes, 0, jsonUtf8Bytes.Length);
    stream.Close();

    JsonReaderOptions options = new JsonReaderOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    UndertaleGameObject newGameObject = new UndertaleGameObject();

    Utf8JsonReader reader = new Utf8JsonReader(jsonUtf8Bytes, options);

    ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
    ReadName(ref reader);
    ReadMainValues(ref reader);
    ReadPhysicsVerts(ref reader);
    ReadAllEvents(ref reader);
    ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
    if (Data.GameObjects.ByName(newGameObject.Name.Content) == null) Data.GameObjects.Add(newGameObject);
    
    void ReadMainValues(ref Utf8JsonReader reader)
    {
        string spriteName = ReadString(ref reader);

        newGameObject.Visible = ReadBool(ref reader);
        newGameObject.Solid = ReadBool(ref reader);
        newGameObject.Depth = (int) ReadNum(ref reader);
        newGameObject.Persistent = ReadBool(ref reader);

        string parentName = ReadString(ref reader);
        string texMaskName = ReadString(ref reader);

        newGameObject.UsesPhysics = ReadBool(ref reader);
        newGameObject.IsSensor = ReadBool(ref reader);
        newGameObject.CollisionShape = (CollisionShapeFlags) ReadNum(ref reader);
        newGameObject.Density = ReadFloat(ref reader);
        newGameObject.Restitution = ReadFloat(ref reader);
        newGameObject.Group = (uint) ReadNum(ref reader);
        newGameObject.LinearDamping = ReadFloat(ref reader);
        newGameObject.AngularDamping = ReadFloat(ref reader);
        newGameObject.Friction = ReadFloat(ref reader);
        newGameObject.Awake = ReadBool(ref reader);
        newGameObject.Kinematic = ReadBool(ref reader);

        newGameObject.Sprite = (spriteName == null) ? null : Data.Sprites.ByName(spriteName);

        newGameObject.ParentId = (parentName == null) ? null : Data.GameObjects.ByName(parentName);

        newGameObject.TextureMaskId = (texMaskName == null) ? null : Data.Sprites.ByName(texMaskName);
    }

    void ReadPhysicsVerts(ref Utf8JsonReader reader)
    {
        newGameObject.PhysicsVertices.Clear();
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleGameObject.UndertalePhysicsVertex physVert = new UndertaleGameObject.UndertalePhysicsVertex();
                physVert.X = ReadNum(ref reader);
                physVert.Y = ReadNum(ref reader);
                newGameObject.PhysicsVertices.Add(physVert);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndObject) continue;
            if (reader.TokenType == JsonTokenType.EndArray) break;

            throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
        }
    }

    void ReadAllEvents(ref Utf8JsonReader reader)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        int eventListIndex = -1;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                eventListIndex++;
                newGameObject.Events[eventListIndex].Clear();
                foreach (UndertaleGameObject.Event eventToAdd in ReadEvents(ref reader, eventListIndex)) newGameObject.Events[eventListIndex].Add(eventToAdd);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndObject) continue;
            if (reader.TokenType == JsonTokenType.EndArray) break;

            throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
        }
    }

    List<UndertaleGameObject.Event> ReadEvents(ref Utf8JsonReader reader, int index)
    {
        List<UndertaleGameObject.Event> eventsToReturn = new List<UndertaleGameObject.Event>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName) continue;
            if (reader.TokenType == JsonTokenType.EndArray) return eventsToReturn;
            if (reader.TokenType != JsonTokenType.StartObject) continue;

            UndertaleGameObject.Event newEvent = new UndertaleGameObject.Event();
            if (index == CollisionEventIndex) {
                string name = ReadString(ref reader);
                newEvent.EventSubtype = (uint) Data.GameObjects.IndexOf(Data.GameObjects.ByName(name));
            } else {
                newEvent.EventSubtype = (uint) ReadNum(ref reader);
            }
            newEvent.Actions.Clear();
            foreach (UndertaleGameObject.EventAction action in ReadActions(ref reader)) newEvent.Actions.Add(action);
            eventsToReturn.Add(newEvent);
            ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
        }

        throw new ScriptException("ERROR: Could not find end of array token - Events.");
    }

    List<UndertaleGameObject.EventAction> ReadActions(ref Utf8JsonReader reader)
    {
        List<UndertaleGameObject.EventAction> actionsToReturn = new List<UndertaleGameObject.EventAction>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName) continue;
            if (reader.TokenType == JsonTokenType.EndArray) return actionsToReturn;
            if (reader.TokenType != JsonTokenType.StartObject) continue;

            UndertaleGameObject.EventAction newAction = ReadAction(ref reader);
            actionsToReturn.Add(newAction);
        }

        throw new ScriptException("ERROR: Could not find end of array token - Actions.");
    }

    UndertaleGameObject.EventAction ReadAction(ref Utf8JsonReader reader)
    {
        UndertaleGameObject.EventAction newAction = new UndertaleGameObject.EventAction();
        newAction.LibID = (uint) ReadNum(ref reader);
        newAction.ID = (uint) ReadNum(ref reader);
        newAction.Kind = (uint) ReadNum(ref reader);
        newAction.UseRelative = ReadBool(ref reader);
        newAction.IsQuestion = ReadBool(ref reader);
        newAction.UseApplyTo = ReadBool(ref reader);
        newAction.ExeType = (uint) ReadNum(ref reader);
        string actionName = ReadString(ref reader);
        string codeId = ReadString(ref reader);
        newAction.ArgumentCount = (uint) ReadNum(ref reader);
        newAction.Who = (int) ReadNum(ref reader);
        newAction.Relative = ReadBool(ref reader);
        newAction.IsNot = ReadBool(ref reader);

        if (actionName == null)
        {
            newAction.ActionName = null;
        }
        else
        {
            newAction.ActionName = new UndertaleString(actionName);

            if (!Data.Strings.Any(s => s == newAction.ActionName))
                Data.Strings.Add(newAction.ActionName);
        }

        if (codeId == null)
            newAction.CodeId = null;
        else
            newAction.CodeId = Data.Code.ByName(codeId);

        ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
        return newAction;
    }

    void ReadName(ref Utf8JsonReader reader)
    {
        string name = ReadString(ref reader);
        if (name == null) throw new ScriptException("ERROR: Object name was null - object name must be defined!");
        if (Data.GameObjects.ByName(name) != null)
        {
            newGameObject = Data.GameObjects.ByName(name);
        }
        else
        {
            newGameObject = new UndertaleGameObject();
            newGameObject.Name = new UndertaleString(name);
            Data.Strings.Add(newGameObject.Name);
        }
    }

    // Read tokens of specified type

    bool ReadBool(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.True: return true;
                case JsonTokenType.False: return false;
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected Boolean - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected Boolean.");
    }

    long ReadNum(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.Number: return reader.GetInt64();
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected Integer.");
    }

    float ReadFloat(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.Number: return reader.GetSingle();
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected Decimal - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected Decimal.");
    }

    string ReadString(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.String: return reader.GetString();
                case JsonTokenType.Null: return null;
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected String - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
    }

    // Watch for certain meta-tokens

    void ReadAnticipateJSONObject(ref Utf8JsonReader reader, JsonTokenType allowedTokenType)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
                continue;
            if (reader.TokenType == allowedTokenType)
                return;
            throw new ScriptException($"ERROR: Unexpected token type. Expected {allowedTokenType} - found {reader.TokenType}");
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
    }
}

// Taken from ImportRoom_v3 by SolventMercury and AwfulNasty
public void ImportRoom(string filePath) {
    FileStream stream = File.OpenRead(filePath);
    byte[] jsonUtf8Bytes = new byte[stream.Length];

    stream.Read(jsonUtf8Bytes, 0, jsonUtf8Bytes.Length);
    stream.Close();

    UndertaleRoom newRoom = new UndertaleRoom();

    JsonReaderOptions options = new JsonReaderOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    Utf8JsonReader reader = new Utf8JsonReader(jsonUtf8Bytes, options);

    ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);

    ReadName(ref reader);
    ReadMainValues(ref reader);
    ClearRoomLists();

    ReadBackgrounds(ref reader);
    ReadViews(ref reader);
    ReadGameObjects(ref reader);
    ReadTiles(ref reader);
    ReadLayers(ref reader);
    // Adds room to data file, if it doesn't exist.
    if (Data.Rooms.ByName(newRoom.Name.Content) == null)
        Data.Rooms.Add(newRoom);

    void ReadMainValues(ref Utf8JsonReader reader)
    {
        string caption = ReadString(ref reader);

        newRoom.Width = (uint) ReadNum(ref reader);
        newRoom.Height = (uint) ReadNum(ref reader);
        newRoom.Speed = (uint) ReadNum(ref reader);
        newRoom.Persistent = ReadBool(ref reader);
        newRoom.BackgroundColor = (uint) (0xFF000000 | ReadNum(ref reader)); // make alpha 255 (BG color doesn't have alpha)
        newRoom.DrawBackgroundColor = ReadBool(ref reader);

        string ccIdName = ReadString(ref reader);

        newRoom.Flags = (UndertaleRoom.RoomEntryFlags) ReadNum(ref reader);
        newRoom.World = ReadBool(ref reader);
        newRoom.Top = (uint) ReadNum(ref reader);
        newRoom.Left = (uint) ReadNum(ref reader);
        newRoom.Right = (uint) ReadNum(ref reader);
        newRoom.Bottom = (uint) ReadNum(ref reader);
        newRoom.GravityX = ReadFloat(ref reader);
        newRoom.GravityY = ReadFloat(ref reader);
        newRoom.MetersPerPixel = ReadFloat(ref reader);

        newRoom.Caption = (caption == null) ? null : new UndertaleString(caption);

        if ((newRoom.Caption != null) && !Data.Strings.Any(s => s == newRoom.Caption))
            Data.Strings.Add(newRoom.Caption);

        newRoom.CreationCodeId = (ccIdName == null) ? null : Data.Code.ByName(ccIdName);
    }

    void ReadName(ref Utf8JsonReader reader)
    {
        string name = ReadString(ref reader);
        if (name == null)
            throw new ScriptException("ERROR: Object name was null - object name must be defined!");

        if (Data.Rooms.ByName(name) != null)
        {
            newRoom = Data.Rooms.ByName(name);
        }
        else
        {
            newRoom = new UndertaleRoom();
            newRoom.Name = new UndertaleString(name);
            Data.Strings.Add(newRoom.Name);
        }
    }

    void ClearRoomLists()
    {
        newRoom.Backgrounds.Clear();
        newRoom.Views.Clear();
        newRoom.GameObjects.Clear();
        newRoom.Tiles.Clear();
        newRoom.Layers.Clear();
    }

    void ReadBackgrounds(ref Utf8JsonReader reader)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleRoom.Background newBg = new UndertaleRoom.Background();

                newBg.ParentRoom = newRoom;

                newBg.Enabled = ReadBool(ref reader);
                newBg.Foreground = ReadBool(ref reader);
                string bgDefName = ReadString(ref reader);
                newBg.X = (int) ReadNum(ref reader);
                newBg.Y = (int) ReadNum(ref reader);
                newBg.TiledHorizontally = ReadBool(ref reader);
                newBg.TiledVertically = ReadBool(ref reader);
                newBg.SpeedX = (int) ReadNum(ref reader);
                newBg.SpeedY = (int) ReadNum(ref reader);
                newBg.Stretch = ReadBool(ref reader);

                newBg.BackgroundDefinition = (bgDefName == null) ? null : Data.Backgrounds.ByName(bgDefName);

                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

                newRoom.Backgrounds.Add(newBg);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
        }
    }

    void ReadViews(ref Utf8JsonReader reader)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleRoom.View newView = new UndertaleRoom.View();

                newView.Enabled = ReadBool(ref reader);
                newView.ViewX = (int) ReadNum(ref reader);
                newView.ViewY = (int) ReadNum(ref reader);
                newView.ViewWidth = (int) ReadNum(ref reader);
                newView.ViewHeight = (int) ReadNum(ref reader);
                newView.PortX = (int) ReadNum(ref reader);
                newView.PortY = (int) ReadNum(ref reader);
                newView.PortWidth = (int) ReadNum(ref reader);
                newView.PortHeight = (int) ReadNum(ref reader);
                newView.BorderX = (uint) ReadNum(ref reader);
                newView.BorderY = (uint) ReadNum(ref reader);
                newView.SpeedX = (int) ReadNum(ref reader);
                newView.SpeedY = (int) ReadNum(ref reader);
                string objIdName = ReadString(ref reader);

                newView.ObjectId = (objIdName == null) ? null : Data.GameObjects.ByName(objIdName);

                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

                newRoom.Views.Add(newView);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
        }
    }

    void ReadGameObjects(ref Utf8JsonReader reader)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleRoom.GameObject newObj = new UndertaleRoom.GameObject();

                newObj.X = (int) ReadNum(ref reader);
                newObj.Y = (int) ReadNum(ref reader);

                string objDefName = ReadString(ref reader);

                uint InstanceID = (uint) ReadNum(ref reader);
                if (usedInstanceIDs.Contains(InstanceID)) {
                    InstanceID = ++Data.GeneralInfo.LastObj;
                }
                if (InstanceID >= 10000000) {
                    throw new ScriptException("Instance IDs too large, overflowing into Tile IDs");
                }
                Data.GeneralInfo.LastObj = Math.Max(InstanceID, Data.GeneralInfo.LastObj);
                usedInstanceIDs.Add(InstanceID);
                newObj.InstanceID = InstanceID;

                string ccIdName = ReadString(ref reader);

                newObj.ScaleX = ReadFloat(ref reader);
                newObj.ScaleY = ReadFloat(ref reader);
                newObj.Color = (uint) ReadNum(ref reader);
                newObj.Rotation = ReadFloat(ref reader);

                string preCcIdName = ReadString(ref reader);

                newObj.ImageSpeed = ReadFloat(ref reader);
                newObj.ImageIndex = (int) ReadNum(ref reader);

                newObj.ObjectDefinition = (objDefName == null) ? null : Data.GameObjects.ByName(objDefName);
                newObj.CreationCode = (ccIdName == null) ? null : Data.Code.ByName(ccIdName);
                newObj.PreCreateCode = (preCcIdName == null) ? null : Data.Code.ByName(preCcIdName);
                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
                //remove empty instances
                if (!Data.IsGameMaker2() && newObj.ObjectDefinition != null && Data.GameObjects.ByName(objDefName) != null)
                    newRoom.GameObjects.Add(newObj);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
        }
    }

    void ReadTiles(ref Utf8JsonReader reader)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleRoom.Tile newTile = new UndertaleRoom.Tile();

                newTile.spriteMode = ReadBool(ref reader);
                newTile.X = (int) ReadNum(ref reader);
                newTile.Y = (int) ReadNum(ref reader);

                string bgDefName = ReadString(ref reader);
                string sprDefName = ReadString(ref reader);

                newTile.SourceX = (uint) ReadNum(ref reader);
                newTile.SourceY = (uint) ReadNum(ref reader);
                newTile.Width = (uint) ReadNum(ref reader);
                newTile.Height = (uint) ReadNum(ref reader);
                newTile.TileDepth = (int) ReadNum(ref reader);
                uint InstanceID = (uint) ReadNum(ref reader);
                if (usedTileIDs.Contains(InstanceID)) {
                    InstanceID = ++Data.GeneralInfo.LastTile;
                }
                usedTileIDs.Add(InstanceID);
                Data.GeneralInfo.LastTile = Math.Max(InstanceID, Data.GeneralInfo.LastTile);
                newTile.InstanceID = InstanceID;
                newTile.ScaleX = ReadFloat(ref reader);
                newTile.ScaleY = ReadFloat(ref reader);
                newTile.Color = (uint) ReadNum(ref reader);

                newTile.BackgroundDefinition = (bgDefName == null) ? null : Data.Backgrounds.ByName(bgDefName);
                newTile.SpriteDefinition = (sprDefName == null) ? null : Data.Sprites.ByName(sprDefName);

                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

                newRoom.Tiles.Add(newTile);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
        }
    }

    void ReadLayers(ref Utf8JsonReader reader)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleRoom.Layer newLayer = new UndertaleRoom.Layer();

                string layerName = ReadString(ref reader);

                newLayer.LayerId = (uint) ReadNum(ref reader);
                newLayer.LayerType = (UndertaleRoom.LayerType) ReadNum(ref reader);
                newLayer.LayerDepth = (int) ReadNum(ref reader);
                newLayer.XOffset = ReadFloat(ref reader);
                newLayer.YOffset = ReadFloat(ref reader);
                newLayer.HSpeed = ReadFloat(ref reader);
                newLayer.VSpeed = ReadFloat(ref reader);
                newLayer.IsVisible = ReadBool(ref reader);


                newLayer.LayerName = (layerName == null) ? null : new UndertaleString(layerName);

                if ((layerName != null) && !Data.Strings.Any(s => s == newLayer.LayerName))
                    Data.Strings.Add(newLayer.LayerName);

                switch (newLayer.LayerType)
                {
                    case UndertaleRoom.LayerType.Background:
                        ReadBackgroundLayer(ref reader, newLayer);
                        break;
                    case UndertaleRoom.LayerType.Instances:
                        ReadInstancesLayer(ref reader, newLayer);
                        break;
                    case UndertaleRoom.LayerType.Assets:
                        ReadAssetsLayer(ref reader, newLayer);
                        break;
                    case UndertaleRoom.LayerType.Tiles:
                        ReadTilesLayer(ref reader, newLayer);
                        break;
                    default:
                        throw new ScriptException("ERROR: Invalid value for layer data type.");
                }

                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

                newRoom.Layers.Add(newLayer);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
        }
    }

    void ReadBackgroundLayer(ref Utf8JsonReader reader, UndertaleRoom.Layer newLayer)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
        //Layer needs to have a Parent Room otherwise a sprite update will cause a null refrence.
        newLayer.ParentRoom = newRoom;
        UndertaleRoom.Layer.LayerBackgroundData newLayerData = new UndertaleRoom.Layer.LayerBackgroundData();
        newLayerData.Visible = ReadBool(ref reader);
        newLayerData.Foreground = ReadBool(ref reader);
        string sprite = ReadString(ref reader);
        newLayerData.TiledHorizontally = ReadBool(ref reader);
        newLayerData.TiledVertically = ReadBool(ref reader);
        newLayerData.Stretch = ReadBool(ref reader);
        newLayerData.Color = (uint) ReadNum(ref reader);
        newLayerData.FirstFrame = ReadFloat(ref reader);
        newLayerData.AnimationSpeed = ReadFloat(ref reader);
        newLayerData.AnimationSpeedType = (AnimationSpeedType) ReadNum(ref reader);
        newLayerData.Sprite = null;
        newLayerData.ParentLayer = newLayer;
        UndertaleSprite bgsprite = Data.Sprites.ByName(sprite);
        if (bgsprite is not null)
            newLayerData.Sprite = bgsprite;
        ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
        newLayer.Data = newLayerData;

    }

    void ReadInstancesLayer(ref Utf8JsonReader reader, UndertaleRoom.Layer newLayer)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
                continue;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new ScriptException("ERROR: Did not correctly stop reading instances layer");

            UndertaleRoom.Layer.LayerInstancesData newLayerData = new UndertaleRoom.Layer.LayerInstancesData();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                    continue;

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    UndertaleRoom.GameObject newObj = new UndertaleRoom.GameObject();

                    newObj.X = (int) ReadNum(ref reader);
                    newObj.Y = (int) ReadNum(ref reader);

                    string objDefName = ReadString(ref reader);

                    newObj.InstanceID = (uint) ReadNum(ref reader);

                    string ccIdName = ReadString(ref reader);

                    newObj.ScaleX = ReadFloat(ref reader);
                    newObj.ScaleY = ReadFloat(ref reader);
                    newObj.Color = (uint) ReadNum(ref reader);
                    newObj.Rotation = ReadFloat(ref reader);

                    string preCcIdName = ReadString(ref reader);

                    newObj.ImageSpeed = ReadFloat(ref reader);
                    newObj.ImageIndex = (int) ReadNum(ref reader);

                    newObj.ObjectDefinition = (objDefName == null) ? null : Data.GameObjects.ByName(objDefName);

                    newObj.CreationCode = (ccIdName == null) ? null : Data.Code.ByName(ccIdName);

                    newObj.PreCreateCode = (preCcIdName == null) ? null : Data.Code.ByName(preCcIdName);

                    ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
                    //get rid of those nasty empty instances
                    if (newObj.ObjectDefinition != null && Data.GameObjects.ByName(objDefName) != null)
                    {
                        newLayerData.Instances.Add(newObj);
                        newRoom.GameObjects.Add(newObj);
                    }
                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                throw new ScriptException("ERROR: Did not correctly stop reading instances in instance layer");
            }

            ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

            newLayer.Data = newLayerData;

            return;

        }
    }

    void ReadAssetsLayer(ref Utf8JsonReader reader, UndertaleRoom.Layer newLayer)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        UndertaleRoom.Layer.LayerAssetsData newLayerData = new UndertaleRoom.Layer.LayerAssetsData();

        newLayerData.LegacyTiles = new UndertalePointerList<UndertaleRoom.Tile>();
        newLayerData.Sprites = new UndertalePointerList<UndertaleRoom.SpriteInstance>();
        newLayerData.Sequences = new UndertalePointerList<UndertaleRoom.SequenceInstance>();
        newLayerData.NineSlices = new UndertalePointerList<UndertaleRoom.SpriteInstance>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleRoom.Tile newTile = new UndertaleRoom.Tile();

                newTile.spriteMode = ReadBool(ref reader);
                newTile.X = (int) ReadNum(ref reader);
                newTile.Y = (int) ReadNum(ref reader);

                string bgDefName = ReadString(ref reader);
                string sprDefName = ReadString(ref reader);

                newTile.SourceX = (uint) ReadNum(ref reader);
                newTile.SourceY = (uint) ReadNum(ref reader);
                newTile.Width = (uint) ReadNum(ref reader);
                newTile.Height = (uint) ReadNum(ref reader);
                newTile.TileDepth = (int) ReadNum(ref reader);
                newTile.InstanceID = (uint) ReadNum(ref reader);
                newTile.ScaleX = ReadFloat(ref reader);
                newTile.ScaleY = ReadFloat(ref reader);
                newTile.Color = (uint) ReadNum(ref reader);

                newTile.BackgroundDefinition = (bgDefName == null) ? null : Data.Backgrounds.ByName(bgDefName);

                newTile.SpriteDefinition = (sprDefName == null) ? null : Data.Sprites.ByName(sprDefName);

                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

                newLayerData.LegacyTiles.Add(newTile);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
        }

        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
                continue;

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleRoom.SpriteInstance newSpr = new UndertaleRoom.SpriteInstance();

                string name = ReadString(ref reader);
                string spriteName = ReadString(ref reader);

                newSpr.X = (int) ReadNum(ref reader);
                newSpr.Y = (int) ReadNum(ref reader);
                newSpr.ScaleX = ReadFloat(ref reader);
                newSpr.ScaleY = ReadFloat(ref reader);
                newSpr.Color = (uint) ReadNum(ref reader);
                newSpr.AnimationSpeed = ReadFloat(ref reader);
                newSpr.AnimationSpeedType = (AnimationSpeedType) ReadNum(ref reader);
                newSpr.FrameIndex = ReadFloat(ref reader);
                newSpr.Rotation = ReadFloat(ref reader);

                newSpr.Name = (name == null) ? null : new UndertaleString(name);

                if ((name != null) && !Data.Strings.Any(s => s == newSpr.Name))
                    Data.Strings.Add(newSpr.Name);

                newSpr.Sprite = (spriteName == null) ? null : Data.Sprites.ByName(spriteName);

                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

                newLayerData.Sprites.Add(newSpr);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            throw new ScriptException("ERROR: Did not correctly stop reading instances in instance layer");
        }

        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
                continue;

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleRoom.SequenceInstance newSeq = new UndertaleRoom.SequenceInstance();

                string name = ReadString(ref reader);
                string sequenceName = ReadString(ref reader);

                newSeq.X = (int) ReadNum(ref reader);
                newSeq.Y = (int) ReadNum(ref reader);
                newSeq.ScaleX = ReadFloat(ref reader);
                newSeq.ScaleY = ReadFloat(ref reader);
                newSeq.Color = (uint) ReadNum(ref reader);
                newSeq.AnimationSpeed = ReadFloat(ref reader);
                newSeq.AnimationSpeedType = (AnimationSpeedType) ReadNum(ref reader);
                newSeq.FrameIndex = ReadFloat(ref reader);
                newSeq.Rotation = ReadFloat(ref reader);


                newSeq.Name = (name == null) ? null : new UndertaleString(name);

                if ((name != null) && !Data.Strings.Any(s => s == newSeq.Name))
                    Data.Strings.Add(newSeq.Name);

                newSeq.Sequence = (sequenceName == null) ? null : Data.Sequences.ByName(sequenceName);

                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

                newLayerData.Sequences.Add(newSeq);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            throw new ScriptException("ERROR: Did not correctly stop reading instances in instance layer");
        }

        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
                continue;

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                UndertaleRoom.SpriteInstance newSpr = new UndertaleRoom.SpriteInstance();

                string name = ReadString(ref reader);
                string spriteName = ReadString(ref reader);

                newSpr.X = (int) ReadNum(ref reader);
                newSpr.Y = (int) ReadNum(ref reader);
                newSpr.ScaleX = ReadFloat(ref reader);
                newSpr.ScaleY = ReadFloat(ref reader);
                newSpr.Color = (uint) ReadNum(ref reader);
                newSpr.AnimationSpeed = ReadFloat(ref reader);
                newSpr.AnimationSpeedType = (AnimationSpeedType) ReadNum(ref reader);
                newSpr.FrameIndex = ReadFloat(ref reader);
                newSpr.Rotation = ReadFloat(ref reader);

                newSpr.Name = (name == null) ? null : new UndertaleString(name);

                if ((name != null) && !Data.Strings.Any(s => s == newSpr.Name))
                    Data.Strings.Add(newSpr.Name);

                newSpr.Sprite = spriteName == null ? null : Data.Sprites.ByName(spriteName);

                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

                newLayerData.NineSlices.Add(newSpr);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            throw new ScriptException("ERROR: Did not correctly stop reading instances in instance layer");
        }

        newLayer.Data = newLayerData;
        ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
    }

    void ReadTilesLayer(ref Utf8JsonReader reader, UndertaleRoom.Layer newLayer)
    {
        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
        UndertaleRoom.Layer.LayerTilesData newLayerData = new UndertaleRoom.Layer.LayerTilesData();

        string backgroundName = ReadString(ref reader);
        
        newLayerData.TilesX = (uint) ReadNum(ref reader);
        newLayerData.TilesY = (uint) ReadNum(ref reader);

        newLayerData.Background = (backgroundName == null) ? null : Data.Backgrounds.ByName(backgroundName);

        uint[][] tileIds = new uint[newLayerData.TilesY][];
        for (int i = 0; i < newLayerData.TilesY; i++)
        {
            tileIds[i] = new uint[newLayerData.TilesX];
        }

        ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
        for (int y = 0; y < newLayerData.TilesY; y++)
        {
            ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
            for (int x = 0; x < newLayerData.TilesX; x++)
            {
                ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
                (tileIds[y])[x] = (uint) ReadNum(ref reader);
                ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
            }

            ReadAnticipateJSONObject(ref reader, JsonTokenType.EndArray);
        }

        newLayerData.TileData = tileIds;
        ReadAnticipateJSONObject(ref reader, JsonTokenType.EndArray);
        ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

        newLayer.Data = newLayerData;
    }

    // Read tokens of specified type

    bool ReadBool(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.True: return true;
                case JsonTokenType.False: return false;
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected Boolean - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected Boolean.");
    }

    long ReadNum(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.Number: return reader.GetInt64();
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected Integer.");
    }

    float ReadFloat(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.Number: return reader.GetSingle();
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected Decimal - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected Decimal.");
    }

    string ReadString(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName: continue;
                case JsonTokenType.String: return reader.GetString();
                case JsonTokenType.Null: return null;
                default: throw new ScriptException($"ERROR: Unexpected token type. Expected String - found {reader.TokenType}");
            }
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
    }

    // Watch for certain meta-tokens

    void ReadAnticipateJSONObject(ref Utf8JsonReader reader, JsonTokenType allowedTokenType)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
                continue;
            if (reader.TokenType == allowedTokenType)
                return;
            throw new ScriptException($"ERROR: Unexpected token type. Expected {allowedTokenType} - found {reader.TokenType}");
        }

        throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
    }
}


// Does not handle audio groups because AM2R doesn't use them and I can't be bothered
// Also for whatever reason putting this logic in an async thread crashes UTMT
// Taken from ImportSoundsBulk By Jockeholm & Nik the Neko & Grossley
public void ImportSound(string filePath) {
    int embAudioID = -1;
    int audioID = -1;
    string fname = Path.GetFileName(filePath);
    string temp = fname.ToLower();
    if (!temp.EndsWith(".ogg") && !temp.EndsWith(".wav"))
    {
        return;
    }
    string sound_name = Path.GetFileNameWithoutExtension(filePath);
    bool isOGG = Path.GetExtension(fname) == ".ogg";
    bool embedSound = !isOGG;

    bool soundExists = false;
    
    UndertaleSound existing_snd = null;
    
    existing_snd = Data.Sounds.FirstOrDefault(x => x.Name.Content == sound_name);
    soundExists = existing_snd != null;

    UndertaleEmbeddedAudio soundData = null;

    if (embedSound) {
        soundData = new UndertaleEmbeddedAudio() { Data = File.ReadAllBytes(filePath) };
        Data.EmbeddedAudio.Add(soundData);
        if (soundExists)
            Data.EmbeddedAudio.Remove(existing_snd.AudioFile);
        embAudioID = Data.EmbeddedAudio.Count - 1;
    }

    UndertaleSound.AudioEntryFlags flags = UndertaleSound.AudioEntryFlags.Regular;
    
    if (!isOGG)                                // WAV, always embed.
        flags = UndertaleSound.AudioEntryFlags.IsEmbedded   | UndertaleSound.AudioEntryFlags.Regular;
    else                // OGG, external.
    {
        flags = UndertaleSound.AudioEntryFlags.Regular;
        audioID = -1;
    }

    UndertaleEmbeddedAudio RaudioFile = null;
    if (!embedSound)             
        RaudioFile = null;
    if (embedSound) 
        RaudioFile = Data.EmbeddedAudio[embAudioID];
    string soundfname = sound_name;

    if (!soundExists)
    {
        var snd_to_add = new UndertaleSound()
        {
            Name        = Data.Strings.MakeString(soundfname),
            Flags       = flags,
            Type        = (isOGG      ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav")               ),
            File        = Data.Strings.MakeString(fname),
            Effects     = 0,
            Volume      = 1.0F,
            Pitch       = 1.0F,
            AudioID     = audioID,
            AudioFile   = RaudioFile,
            AudioGroup  = null,
            GroupID     = Data.GetBuiltinSoundGroupID()
        };
        
        Data.Sounds.Add(snd_to_add);
    }
    else
    {
        existing_snd.AudioFile = RaudioFile;
        existing_snd.AudioID   = audioID;
    }
}

public void ReplaceNames(string[] lines) {
    int lineCount = 0;
    foreach (string line in lines)
    {
        lineCount++;
        string trimmedLine = line.Trim();
        if (trimmedLine == "" || trimmedLine[0] == '#')
        {
            continue;
        }
        string[] splitLine = trimmedLine.Split("->", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (splitLine.Length != 2)
        {
            throw new ScriptException($"Syntax error in Replace.txt line {lineCount}: Failed to split line on '->'. Correct syntax is [OriginalName] -> [NewName]");
        }
        string originalName = splitLine[0];
        string newName = splitLine[1];
        UndertaleNamedResource res = Data.ByName(originalName);
        if (res == null)
        {
            throw new ScriptException($"Error in Replace.txt line {lineCount}: Couldn't find asset with name {originalName} to replace. Are you sure you are using the right base file?");
        }
        UndertaleNamedResource newRes = Data.ByName(newName);
        if (newRes != null)
        {
            throw new ScriptException($"Error in Replace.txt line {lineCount}: Asset with name {newName} already exists but attempted to rename {originalName} to {newName}");
        }
        res.Name = new UndertaleString(newName);
        Data.Strings.Add(res.Name);
    }
}

// TODO: Use regex or something here to allow for keyword args, users should probably not have to type all 11 args just to change one thing
public void ImportSpriteOptions(string[] lines) {
    int lineCount = 0;
    foreach (string line in lines)
    {
        lineCount++;
        string trimmedLine = line.Trim();
        if (trimmedLine == "" || trimmedLine[0] == '#')
        {
            continue;
        }
        string[] splitLine = trimmedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (splitLine.Length != 11)
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: " +
                "Incorrect amount of arguments. Correct syntax is [SpriteName] [MarginLeft] [MarginRight] [MarginBottom] [MarginTop] [Transparent] [Smooth] [Preload] [SepMasks] [OriginX] [OriginY]");
        }
        string spriteName = splitLine[0];
        UndertaleSprite sprite = Data.Sprites.ByName(spriteName);
        if (sprite == null)
        {
            throw new ScriptException($"Error in SpriteOptions.txt line {lineCount} Could not find sprite with name {spriteName}");
        }
        if (!int.TryParse(splitLine[1], out int marginLeft))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [MarginLeft] could not be parsed as an integer");
        }
        if (!int.TryParse(splitLine[2], out int marginRight))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [MarginRight] could not be parsed as an integer");
        }
        if (!int.TryParse(splitLine[3], out int marginBottom))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [MarginBottom] could not be parsed as an integer");
        }
        if (!int.TryParse(splitLine[4], out int marginTop))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [MarginTop] could not be parsed as an integer");
        }
        if (!bool.TryParse(splitLine[5], out bool transparent))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [Transparent] could not be parsed as an integer");
        }
        if (!bool.TryParse(splitLine[6], out bool smooth))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [Smooth] could not be parsed as an integer");
        }
        if (!bool.TryParse(splitLine[7], out bool preload))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [Preload] could not be parsed as an integer");
        }
        if (!Enum.TryParse(typeof(UndertaleSprite.SepMaskType), splitLine[8], true, out object result))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [SepMasks] could not be parsed as a sepMaskType");
        }
        UndertaleSprite.SepMaskType sepMaskType = (UndertaleSprite.SepMaskType) result;
        if (!int.TryParse(splitLine[9], out int originX))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [OriginX] could not be parsed as an integer");
        }
        if (!int.TryParse(splitLine[10], out int originY))
        {
            throw new ScriptException($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [OriginY] could not be parsed as an integer");
        }
        sprite.MarginLeft = marginLeft;
        sprite.MarginRight = marginRight;
        sprite.MarginTop = marginTop;
        sprite.MarginBottom = marginBottom;
        sprite.Transparent = transparent;
        sprite.Smooth = smooth;
        sprite.Preload = preload;
        sprite.SepMasks = sepMaskType;
        sprite.OriginX = originX;
        sprite.OriginY = originY;
    }
}
