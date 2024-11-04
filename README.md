# UC-MapPainter
Urban Chaos Map Editor

## Overview
UC-MapPainter is an application used for creating, modifying, and exporting the textures of Urban Chaos map files (.iam files). It loads map files onto a grid and allows cell-by-cell texture editing. All files required to run the application are included.

## Features
- **Create New Maps**: Generate new maps using a default world template.
- **Load Maps**: Directly load .iam files with automatic texture,height, Prims and Buildig recognition.
- **Load Lighting**: Directly load .lgt files and edit Light way points and ambient lighting.
- **Zoom and Coordinate Tracking**: Zoom in/out functionality and real-time cell coordinate tracking.
- **Dump Building Data***: Dump raw building data to file for analysis
- **Export Maps**: Export the maps to an 8192 x 8192 pixel image file.
- **Save Maps**: Save maps back to .iam format for use in Urban Chaos.
- **Save Lights**: Save light structures back to .lgt format for use in Urban Chaos.

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
- **Save Map**: When satisfied with the edits, save back to a .iam file using the "Save Map" or "Save As" File option. You can also export the map file to an image.

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

You are also able to view "Wall Styles". In Urban Chaos, walls are textured using 'styles' of textures which are a group of 5 textures which repeat in specific patterns accross walls. These are stored by world number in the 'style.tma' file. The 'style.tma' file for the selected world can be viewed by clicking the 'View / Edit Wall Styles' button in the Texture Selection Window.

![image](https://github.com/user-attachments/assets/3122a2cf-3531-4500-97a0-40efaadd6dc4)

This will bring up the Style viewer window. Currently, this is view-only but will show you all the styles in the .tma file. It also displays the rendering flags for each of the cells.

![image](https://github.com/user-attachments/assets/b6255d47-709f-4c6c-a2bb-4599c382011b)

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

### Viewing Building Data
When a file has been loaded via any of the above methods, Building data can be viewed by selecting the "Buildings" tab.
![image](https://github.com/user-attachments/assets/13f7708b-f42e-45b9-8036-3c96b5edd045)
This will generate the outlines of walls and will label each wall against each building on the Map. You will notice a label like B: x W : x which tells you which building and which wall the line being drawn belongs to. "Walls" in the game include normal building walls, fences, ladders, doors and cables. And these are color coded. Bright green are normal walls, orange are ladders, red are cables, yellow are fences, purple are doors. An example below is shown for the assault1.iam map.
![image](https://github.com/user-attachments/assets/ed26a4a8-1d78-47a4-81de-3137b08f140e)

Building data can be viewed via the 'Building' menu item. 

The building header data can be viewed by selecting 'Building' → 'View' → 'Building Header' which displays the number of Buildings and walls in the Map

![image](https://github.com/user-attachments/assets/9dba5f5f-d878-4494-9a01-36eb1da3cba4)

The buildings data can be viewed by selecting 'Building' → 'View' → 'Buildings' which displays all Building data of the map
![image](https://github.com/user-attachments/assets/916aa617-308c-4bd5-b53a-f127a7900412)

The walls data can be viewed by selecting 'Building' → 'View' → 'Walls' which displays all Walls data of the map

![image](https://github.com/user-attachments/assets/e665dcef-b72a-429f-9aec-385fc30efcac)

Raw building data can be viewed by selecting 'Building' → 'View' → 'Walls'

![image](https://github.com/user-attachments/assets/8ee18874-ff4c-4f22-a4ae-f6332dfeaf42)

This data can also be dumped by selecting 'Building' → 'Dump Building Data' which will output the data to 'BuildingData.bin' in the working directory

![image](https://github.com/user-attachments/assets/7a378ce7-dde4-4caa-a94f-fccc73d96546)

### Editing Lights
This application can also be used to edit Lighting (.lgt) files used in the game. As the name implies, these are used to light the maps and can be loaded alongside .iam files using the 'Ctrl+R' method. You can begin by loading a .iam file into the Map Editor as per the methods above. This will unlock the 'Lights' button. Select this to bring up the 'Light Selection Window'.

![image](https://github.com/user-attachments/assets/a2cc5797-dcbf-46e9-8c30-71cb442040af)

You initially start with a blank .lgt file that you can fill with new light entries. You can place a new light by first setting the 'Range', 'Red', 'Green' and 'Blue' values. The 'Range' value goes from 0-255 and determines the size of the light. The RGB values are signed and range from -127 to 127. There is a color preview bar available which shows the color the light will be when it is placed. As an example, a Green light is placed by increasing the 'Range' value to 255, and changesing the RGB combination to -127,127,-127. You can also modify the height of the light by adjusting the 'Y' slider. The Y Slider is set to increment in storeys. In this example, we simply place it at Storey 1 (which is common for most lights that appear on level ground).

When the values are set, hover the mouse over the grid where you want to place the light and left click to place it. You will also see it added to the 'Light Entries' list box.

![image](https://github.com/user-attachments/assets/ce7c1b18-6350-47f6-93a9-3fa013646ca2)

Next, you can update the 'Night Flags' section. There are three properties: 'Lamps on', 'Darken Walls' and 'Day'. The 'Lamps On' checbox controls whether Street lights are turned on or off. 'Darken Walls' when enabled will darken building points. The final checbox is to control whether it is day or not. By default, light files are at night time. In this example, we will turn the street lights on and keep it at night (which will also generate rain automatically). The SPecular sliders are also RGBA coded and are used for highlighting and reflections. This value rarely changes in the observed .lgt files in the game from 0,0,0,255, so we will keep it at that.

![image](https://github.com/user-attachments/assets/5b05f224-fd6b-4bdb-8780-218d3306cfb1)

The D3D and Specular Color section controls the overall ambient lighting of the map and is an RGBA control set - think of it like a movie filter. We can make levels look like Mexico using the RGB color code for the Mexico movie filter (255,191,55). We will make it fully opaque by keeping the D3D Alpha at 255.

![image](https://github.com/user-attachments/assets/255d0ec2-7930-422c-9fef-2947f8a2e40f)

The 'Night Ambient' section is used to control Ambient Lighting when we are in Night mode, we are currently in Night mode, so we will set it to our Mexico Filter colors again though these values are signed so should reflect the following numbers (127,64,-72).

![image](https://github.com/user-attachments/assets/243111b5-c0f1-4620-82aa-bf1c663a11af)

You can control the colors of the Light emitting Prims (such as Lamp Posts, Billboards etc.) by modifying the RGB values in the 'Light Emitting Prim Color' section. The default values for these are (70,70,36). In this example, we will make them green by setting the values to (-127,127,-127). You can also change the Radius that the light of these Prims reaches by changing the 'Radius' slider. For this, we will keep it at 255.

![image](https://github.com/user-attachments/assets/bcbac4bf-0d72-43aa-97e8-6f3c4c7774b2)

Finally, you can set the Night Sky Colour. The default, sensible values for this is 210,200,240. Which is what we will keep for the example. When we are happy with the values, we can output the new .lgt file by selecting the 'Save Lighting File' button at the top of the Window which will prompt the user to save to a new file. 

![image](https://github.com/user-attachments/assets/d9821111-ddf9-47e1-b7f1-133bf971239c)

We can now load the lighting file using the 'Ctrl+R' method. 

![image](https://github.com/user-attachments/assets/da8e1cb2-17f6-4810-91b7-93adc81b289f) ![image](https://github.com/user-attachments/assets/efa006a4-d922-4d32-b509-ce87aa8a07c4)

We can also Load existing .lgt files for viewing by selecting 'Load Lighting File' button. This will overlay the lights onto the map and update the value sliders according to the file. In this example, we will use the lighting file actually intended for the 'assault.iam' file (assault1.lgt)

![image](https://github.com/user-attachments/assets/f0203b4f-0c51-4716-90fc-a06143887470)
![image](https://github.com/user-attachments/assets/61dfcb74-9e47-4a85-a57a-7c17572ee3b9)


## Acknowledgements
- **Urban Chaos Discord**: [Urban Chaos Discord](https://discord.gg/EhFGnWWagx)
- **Pieroz**: Special thanks to [Pieroz](https://github.com/PieroZ) for breathing life into Urban Chaos modding.
- **Inco1**: [Inco1](https://github.com/inco1/) - The PHP tools he created served as the basis for understanding .iam files.
- **ChatGPT**: This tool was created in its entirety with the assitance of ChatGPT4-Omni

## Contributing
Contributions to UC Map Editor are welcome. Please feel free to fork the repository, make your changes, and submit a pull request.

## License
This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
