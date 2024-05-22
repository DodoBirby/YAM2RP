# YAM2RP
## Yet Another Metroid 2 Remake Patcher ##
This [Undertale Mod Tool](https://github.com/UnderminersTeam/UndertaleModTool) script is designed to support a modding workflow where changes are made to exported source files that are then patched into a data.win instead of the usual UTMT workflow of making changes directly in the data.win. This workflow makes it easier for multiple modders to collaborate on one project since everyone on the team can get the same data.win by supplying the patcher with the same base file.

Most import scripts are not written by me, credits are in comments above the corresponding function.
### Folder Structure ###
All Folders are optional  
Code (Contains all the gml files)  
Objects (Contains json files exported by ExportAllGameObjects)  
Rooms (Contains json files exported by ExportRoomsWithCC)  
Graphics (Contains backgrounds and sprites, folder structure can be whatever you want but the backgrounds/tilesets should be placed in a folder named backgrounds somewhere in the tree)  
Masks (Contains any custom sprite collision masks, should be named the same as the sprite it is a mask of NOTE: You must always append '_#' where # is the frame number of the sprite to mask names, if your sprite only has one frame then append '_0')  
Sounds (Contains .wav and .ogg sound files)  

### How to Use ###
In UTMT you can run scripts using Scripts > Run other script... then to run YAM2RP select YAM2RP.csx from the file select.
