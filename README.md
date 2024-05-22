# YAM2RP
## Yet Another Metroid 2 Remake Patcher ##
This tool is designed to support a modding workflow where changes are made to exported source files that are then patched into a data.win instead of the usual UTMT workflow of making changes directly in the data.win. This workflow makes it easier for multiple modders to collaborate on one project since everyone on the team can get the same data.win by supplying the patcher with the same base file.

Most import scripts are not written by me, credits are in comments above the corresponding function.
### Folder Structure ###
All Folders are optional
Code (Contains all the gml files)
Objects (Contains json files exported by ExportAllGameObjects)
Rooms (Contains json files exported by ExportRoomsWithCC)
Graphics (Contains backgrounds and sprites, folder structure can be whatever you want but the backgrounds/tilesets should be placed in a folder named backgrounds somewhere in the tree)
Masks (Contains any custom sprite collision masks, should be named the same as the sprite it is a mask of)
Sounds (Contains .wav and .ogg sound files)
