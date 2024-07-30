# UC-MapPainter

## Overview
UC-MapPainter is an application used for creating, modifying, and exporting the textures of Urban Chaos map files (.iam files). It loads map files onto a grid and allows cell-by-cell texture editing. All files required to run the application are included.

## Features
- **Create New Maps**: Generate new maps using a default world template.
- **Load Maps**: Directly load .iam files with automatic texture,height and Prim recognition.
- **Zoom and Coordinate Tracking**: Zoom in/out functionality and real-time cell coordinate tracking.
- **Export Maps**: Export the maps to an 8192 x 8192 pixel image file.
- **Save Maps**: Save maps back to .iam format for use in the Urban Chaos game.

## Getting Started
To get started with UC-MapPainter, simply clone the repository and build the application. You will also need to download the Textures and Map folder and place them in the same directory as your EXE. These are located in the Release Zip file.

1. Clone the repository
2. Build the application
3. Move the Textures, Prims and Map folder from the release Zip to your EXE directory.
4. Run the application

## Usage
- **Create New Map**: Select "New Map" from the File menu to generate a blank map. You will be prompted to select a world (style) for the map. Choose carefully, as this selection will be locked.
- **Load Map**: Use the "Load Map" option from the File menu to load an existing .iam file. The application will automatically detect the world (if available). If a world is not detected, you will be prompted to enter one.
- **Select Textures**: Once the grid is loaded, select textures to paint with from the right-hand side. The selected texture box will update accordingly. Rotate the texture using the buttons underneath the selected texture.
- **Zoom**: Use the Zoom In/Zoom Out buttons to view higher resolution maps (use sparingly to avoid performance issues).
- **Save Map**: When satisfied with the paint job, save back to a .iam file using the "Save Map" option. You can also export the map file to an image.

## Instructions
### Starting a New Map
Select 'File' → 'New Map' to load a blank Urban Chaos Map file into the application. All Urban Chaos map files have a 'World' assigned to them which tells the game Engine what textures to load. Theses texture worlds are visible in the 'server/textures' directory of the game. The blank Map template does not have a world assigned, thus you will be asked to select one from the list. Select one and then Press 'OK' to load the Map.

![image](https://github.com/user-attachments/assets/abf05f79-eb66-4567-af89-45c3d0b2a634)

When the map is successfully loaded, your 'Edit Mode' buttons will become available. You can choose between 'Textures' (to modify the tile textures), 'Height' to modify the height of the bottom right Vertex of the cells and 'Prims' to Add and Remove objects from the map. Note the 'Buildings' tab is currently under development and not functional.

![image](https://github.com/user-attachments/assets/6b5ac982-9421-48a1-b427-3fc7213bbe4a)

Select 'Textures' to open the 'Textures Window' and to begin editing the cells of the blank Map. If you accidentally close the 'Textures Window', you can restore it by selecting 'Window' → 'Texture Selection' from the main menu. 

![image](https://github.com/user-attachments/assets/71be294d-284b-4d27-bd37-175c0638cb2f)

By default, blank tiles use the '000hi.bmp' texture. You will notice the 'World Textures' , 'Shared Textures' and 'Prim Textures' section highlighted in Red. 'World Textures' contains all the textures specific to the selected world (In this example, World 8). 'Shared Textures' are the textures that are available between all worlds and 'Prim Textures' are textures reserved for Prim Objects. These are not typically applied to Maps, but it is possible to do so. 

![image](https://github.com/user-attachments/assets/7799a755-9e53-476b-a786-24dbd5ee878d)

To paint a cell, click one of the textures from the 'Texture Selection' window. You will see it copied to the 'Selected Texture' box in the main Window. Use the 'Rotate' buttons to rotate the textures left or right.

![image](https://github.com/user-attachments/assets/c8d28cb6-72bc-41f9-a14c-71e616497375)

When you are happy with the rotation, select a cell on the grid to place the texture. You can use the 'Mouse Position' in the top left to keep track of where in the Map you are.

![image](https://github.com/user-attachments/assets/0006f32c-c8c5-4ba4-8181-7d52fa8ae0eb)

Next, select 'Height'. This will bring forward the Height modification grid you can use to raise or lower the terrain. Clicking a cell will increase the height by 1 unit. Right clicking the cell will decrease the height by 1 unit. Additionally, you can hold 'Shift' key to increase/decrease by units of 10. The cell itself does not raise, but rather the bottom right vertex of the cell.

![image](https://github.com/user-attachments/assets/42b0332e-8481-4bf9-9023-e9ffb1d7bf9e)

Finally, select the 'Prims' button to bring forward the 'Prim Selection Window' and add / remove Prim objects from the Map. Once again, if you accidentally close the Prim Window, it can be restored by selecting 'Window' → 'Prim Selection' from the Main menu. Select a desired Prim by clicking the relevant Prim button from the 'Prim Selection' window. Once clicked, the top box will populate with a top down preview of the Prim.

![image](https://github.com/user-attachments/assets/fb6fde01-a3a1-4052-83e9-d162b92b6e15)

You can use the 'Rotate' buttons in the 'Prim Selection' window to change the Yaw of the Prim before placing it onto the map.

![image](https://github.com/user-attachments/assets/8ce6a3ce-33ef-4755-800f-f9eab1a089e4)

Objects can be placed at any height on the map also. Use the 'Adjust Height' button to enable the height slider for the object. The slider has a '+' and '-' button which will adjust the height of the object one storey above and below respectively. The exact height can also be manually fine tuned by modifying the number in the slider text box.

![image](https://github.com/user-attachments/assets/9645d6d1-6cc5-476d-ae55-54cf97be5a7b)

Finally, there are the Prim flag checboxes. These can be used to enable various properties concerning the Prim. The 'On Floor' checbox is typically used to anchor the Prim to the height of the cell, the 'Searchable' checkbox allows the Prim to become searchable by holding the 'Action' key in-game. The 'Not on PSX' flag is ambiguous, the 'Damaged' checkbox will enable the 'Damaged' property of the Prim (e.g. a Lamp Post will lean), 'Warehouse' is not known. 'Hidden Item' is not known. 'Reserved 1' and 'Reserved 2' are unusued. As an example, to make the Van searchable, the checkbox is ticked and the Prim is placed onto the grid by Left Mouse clicking on the desired cell. Prims can be removed by hovering over the red elipse and Right mouse clicking. Prim Information can also be displayed by Left Mouse clicking an already placed elipse. 

![image](https://github.com/user-attachments/assets/15ad407f-fe65-4a3d-8f8c-ed89af4fad67)

![image](https://github.com/user-attachments/assets/346dafbf-e87b-49d0-826b-fb4fb865d387)

Objects are referenced by the game Engine via a structure called MapWho. There are 1024 MapWho cells which divide the 128x128 map into 32x32 Mapwho cells (each coverying 4x4 tiles). To view the MapWho grid overlayed onto the Map, you can select the 'Prims' → 'Draw MapWho Grid' item from the main menu.

![image](https://github.com/user-attachments/assets/19b17f24-7ba3-47d9-aae9-de578ee1430e)

The graphics for the Prims can be toggled on and off also via the Checbox under 'Prims' → 'Graphics Enabled'. This may be useful because Prims cannot be placed precisely in an area that is occupied by a Prim image. This is a known bug that will be fixed at a later date. 

![image](https://github.com/user-attachments/assets/af836984-84b0-4113-9d2b-1480b15284f8)

You can view all the Prims on a Map by selecting the 'Prims' → 'Show Prim Info' menu item. This will list the details of each Prim object.

![image](https://github.com/user-attachments/assets/e01284d0-8b75-45f7-9de3-98463755293b)

You can also view the MapWho information by selecting 'Prims' → 'Show MapWho Info'. This lists the values for each of the 1024 ordered MapWho cells. MapWho cells are 2 byte structures. The Lower 11 bits specifies the index in the Prim array that the first object in the MapWho cell starts and the upper 5 bits specify the number of objects that appear in MapWho. It is worth noting that the Prim objects are stored by MapWho cell to accomodate this.

![image](https://github.com/user-attachments/assets/5c758633-c5f8-4530-aaeb-5446f001cfd6)

Finally, you can use the '+' and '-' buttons on the Main window to zoom in and out of the map. The more cells are in view the harder the graphics processing needs to work, so use sparingly.

![image](https://github.com/user-attachments/assets/bd9c0569-96ec-4d8b-96fc-b337aa6003fa)

When you are happy with the map, you can save the map by selecitng 'File' → 'Save' or 'File' → 'Save As'. This will open the file save dialog. Save your map file into the 'data' directory of your Urban Chaos installation, then you can load the Map by modifying the 'urban.sty' file or using the 'BANGUNSNOTGAMES' debug code and using 'Ctrl + R' at the main menu to load the map.

![image](https://github.com/user-attachments/assets/384fbed1-889e-4a53-89a8-7c863e1a66ab)

![image](https://github.com/user-attachments/assets/61fa44ef-b8ba-4722-ad6b-8f2c75358af2)

![image](https://github.com/user-attachments/assets/7f3b15d5-eefe-4f92-a7fb-8b37636189cc)

### Loading A Map
Select 'File' → 'Load Map' to load an existing map file. The UC Map Editor supports every Map version available in game. This will load the Map, its textures, heights and Prim objects which can be modified using the above methods and saved back to disk by selecting the 'File' → 'Save' button.

In this example, the 'Insane Assault' level is loaded by selecting 'Balbash1.iam'.

![image](https://github.com/user-attachments/assets/6010ad12-008a-4cc8-a5b2-78330d9315cc)

Texture Maps can also be exported to a 8192 x 8192 pixel Bitmap image by selecting 'File' → 'Export Map to BMP' file menu option.

![image](https://github.com/user-attachments/assets/96852c54-89cc-4022-842d-29fc0dea462a)

![image](https://github.com/user-attachments/assets/0e14ae4c-334a-4ed8-b0cd-fc29b4622832)

## Acknowledgements
- **Urban Chaos Discord**: [Urban Chaos Discord](https://discord.gg/EhFGnWWagx)
- **Pieroz**: Special thanks to [Pieroz](https://github.com/PieroZ) for breathing life into Urban Chaos modding.
- **Inco1**: [Inco1](https://github.com/inco1/) - The PHP tools he created served as the basis for understanding .iam files.
- **ChatGPT**: This tool was created in its entirety with the assitance of ChatGPT4-Omni

## Contributing
Contributions to UC Map Editor are welcome. Please feel free to fork the repository, make your changes, and submit a pull request.

## License
This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
