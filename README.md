# YAM2RP
## Yet Another Metroid 2 Remake Patcher ##
This [Undertale Mod Tool](https://github.com/UnderminersTeam/UndertaleModTool) script is designed to support a modding workflow where changes are made to exported source files that are then patched into a data.win instead of the usual UTMT workflow of making changes directly in the data.win. This workflow makes it easier for multiple modders to collaborate on one project since everyone on the team can get the same data.win by supplying the patcher with the same base file.

To download the script the recommended method is to clone this repo using git. Alternatively you can just download the code as a zip.  
To use the script see the [How to Use](#how-to-use) section.

Most import scripts are not written by me, credits are in comments above the corresponding function.
### Folder Structure ###
All Folders are optional  
- Code: Contains all the gml files, they should follow GMS naming conventions like gml_Object_testObject_Step_0.gml or gml_Script_testscript.gml otherwise they might not import properly
- Objects: Contains json files exported by ExportAllGameObjects.csx (in the ExportScripts folder of this repo)
- Rooms: Contains json files exported by ExportRoomsWithCC (in the ExportScripts folder of this repo)
- Graphics: Contains backgrounds and sprites, folder structure can be whatever you want but the backgrounds/tilesets should be placed in a folder named 'backgrounds' somewhere in the tree
- Masks: Contains any custom sprite collision masks, should be named the same as the sprite it is a mask of NOTE: You must always append '_#' where # is the frame number of the sprite to mask names, if your sprite only has one frame then append '_0'
- Sounds: Contains .wav and .ogg sound files, note that YAM2RP will add entries for .ogg files in the data.win but the .ogg file still needs to be in the am2r.exe folder for it to be read by the game

### Code Exports ###
Code exported from UTMT that references sprites, rooms, or objects will likely use the index number of the asset to reference it since it is decompiled code (e.g exported code might look like `var playersprite = 10` where 10 is the index number of sPlayerSprite). Importing with YAM2RP doesn't guarantee that these indexes will be constant. To deal with this, all of these "magic number" references should be changed to the name of the asset in question (e.g the above code `var playersprite = 10` should be manually changed to `var playersprite = sPlayerSprite` after export), this will ensure that UTMT compiles these to the correct index when importing. This mostly concerns magic number references to assets that are added by import, references to assets which exist in the base file should be ok but it's a good habit for code readability to fix the magic numbers regardless.

For this reason the recommended way to use YAM2RP is to make code edits in an external code editor like VS code.

### Options Files ###
There are currently 2 optional text files you can include in your source folder to get some extra functionality.  
Replace.txt allows you to designate assets in the base file which have been replaced by assets in the result file, the base assets will be renamed 
before any importing occurs.  
SpriteOptions.txt allows you to customise the margins, flags, sepMasks, and Origin positions of sprites.

### How to Use ###
In UTMT you can run scripts using Scripts > Run other script... then to run YAM2RP select YAM2RP.csx from the file select.


