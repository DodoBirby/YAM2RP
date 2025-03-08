using UndertaleModLib;
using UndertaleModLib.Models;

namespace YAM2RP;

public class RoomJSON
{
	public string Name { get; set; } = "";
	public string? Caption { get; set; }
	public uint Width { get; set; }
	public uint Height { get; set; }
	public uint Speed { get; set; }
	public bool Persistent { get; set; }
	public uint BackgroundColor { get; set; }
	public bool DrawBackgroundColor { get; set; }
	public string? CreationCodeId { get; set; }
	public int Flags { get; set; }
	public bool World {  get; set; }
	public uint Top { get; set; }
	public uint Left { get; set; }
	public uint Right { get; set; }
	public uint Bottom { get; set; }
	public float GravityX { get; set; }
	public float GravityY { get; set; }
	public float MetersPerPixel { get; set; }
	public List<Background> Backgrounds { get; set; } = [];
	public List<View> Views { get; set; } = [];
	public List<RoomGameObject> GameObjects { get; set; } = [];
	public List<RoomTile> Tiles { get; set; } = [];
}

public class Background
{
	public bool Enabled { get; set; }
	public bool Foreground { get; set; }
	public string? BackgroundDefinition { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
	public bool TiledVertically { get; set; }
	public bool TiledHorizontally { get; set; }
	public int SpeedX { get; set; }
	public int SpeedY { get; set; }
	public bool Stretch { get; set; }

	public UndertaleRoom.Background ConvertToUnderBackground(UndertaleData data, UndertaleRoom room)
	{
		var newBackground = new UndertaleRoom.Background()
		{
			ParentRoom = room,
			Enabled = Enabled,
			Foreground = Foreground,
			X = X,
			Y = Y,
			TiledHorizontally = TiledHorizontally,
			TiledVertically = TiledVertically,
			SpeedX = SpeedX,
			SpeedY = SpeedY,
			Stretch = Stretch,
			BackgroundDefinition = data.Backgrounds.NameLookupIfNotNull(BackgroundDefinition)
		};
		return newBackground;
	}
}

public class View
{
	public bool Enabled { get; set; }
	public int ViewX { get; set; }
	public int ViewY { get; set; }
	public int ViewWidth { get; set; }
	public int ViewHeight { get; set; }
	public int PortX { get; set; }
	public int PortY { get; set; }
	public int PortWidth { get; set; }
	public int PortHeight { get; set; }
	public uint BorderX { get; set; }
	public uint BorderY { get; set; }
	public int SpeedX { get; set; }
	public int SpeedY { get; set; }
	public string? ObjectId { get; set; }

	public UndertaleRoom.View ConvertToUnderView(UndertaleData data)
	{
		var newView = new UndertaleRoom.View()
		{
			Enabled = Enabled,
			ViewX = ViewX,
			ViewY = ViewY,
			ViewWidth = ViewWidth,
			ViewHeight = ViewHeight,
			PortX = PortX,
			PortY = PortY,
			PortWidth = PortWidth,
			PortHeight = PortHeight,
			BorderX = BorderX,
			BorderY = BorderY,
			SpeedX = SpeedX,
			SpeedY = SpeedY,
			ObjectId = data.GameObjects.NameLookupIfNotNull(ObjectId)
		};
		return newView;
	}
}

public class RoomGameObject
{
	public int X { get; set; }
	public int Y { get; set; }
	public string? ObjectDefinition { get; set; }
	public uint InstanceId { get; set; }
	public string? CreationCode { get; set; }
	public int ScaleX { get; set; }
	public int ScaleY { get; set; }
	public uint Color {  get; set; }
	public float Rotation {  get; set; }
	public string? PreCreateCode { get; set; }
	public float ImageSpeed { get; set; }
	public int ImageIndex { get; set; }

	public UndertaleRoom.GameObject ConvertToUnderObject(UndertaleData data)
	{
		var newObj = new UndertaleRoom.GameObject()
		{
			X = X,
			Y = Y,
			ScaleX = ScaleX,
			ScaleY = ScaleY,
			Color = Color,
			Rotation = Rotation,
			ImageSpeed = ImageSpeed,
			ImageIndex = ImageIndex,
			ObjectDefinition = data.GameObjects.NameLookupIfNotNull(ObjectDefinition),
			CreationCode = data.Code.NameLookupIfNotNull(CreationCode),
			PreCreateCode = data.Code.NameLookupIfNotNull(PreCreateCode),
			InstanceID = data.GeneralInfo.LastObj++
		};
		if (newObj.InstanceID >= 10_000_000)
		{
			throw new Exception("Instance IDs are too large, overflowing into Tile IDs");
		}
		return newObj;
	}
}

public class RoomTile
{
	public bool SpriteMode { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
	public string? BackgroundDefinition { get; set; }
	public string? SpriteDefinition { get; set; }
	public uint SourceX { get; set; }
	public uint SourceY { get; set; }
	public uint Width { get; set; }
	public uint Height { get; set; }
	public int TileDepth { get; set; }
	public int InstanceId { get; set; }
	public int ScaleX { get; set; }
	public int ScaleY { get; set; }
	public uint Color { get; set; }

	public UndertaleRoom.Tile ConvertToUnderTile(UndertaleData data)
	{
		var newTile = new UndertaleRoom.Tile()
		{
			spriteMode = SpriteMode,
			X = X,
			Y = Y,
			SourceX = SourceX,
			SourceY = SourceY,
			Width = Width,
			Height = Height,
			TileDepth = TileDepth,
			InstanceID = data.GeneralInfo.LastTile++,
			ScaleX = ScaleX,
			ScaleY = ScaleY,
			Color = Color,
			BackgroundDefinition = data.Backgrounds.NameLookupIfNotNull(BackgroundDefinition),
			SpriteDefinition = data.Sprites.NameLookupIfNotNull(SpriteDefinition),
		};
		return newTile;
	}
}

public class Layer
{
	// Seems like this is only in GMS2, can add this later
}
