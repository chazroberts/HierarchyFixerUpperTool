# What is this tool for?
When opening a Unity project in an older Unity version, the sibling order of prefabs may not be preserved. This tool can be used in the newer, working Unity version of the project to save the sibling ordering into a text file. Subsequently the same tool may be used in an older UNity version to reorder the siblings to match the original by reading the order from the formerly saved text file. 
#Does it do anything else?
No, it will only reorder GameObject siblings as described above. If there are other issues between Unity versions, this tool will not help.
#Usage
##Installation
Copy the HierarchyFixerUpperTool.cs script to a directory named Editor inside your project folder. After it is imported you should see a menu entry for Tool/Hierarchy Fixer Upper.
##Saving the desired sibling order
In the newer version of Unity, load your project. Using the Tools/Hierarchy Fixer Upper menu item will open the tool window. There will be a list of loaded scenes at the bottom of the window. If you do not see the currently loaded scenes, click the Refresh Scene List button. For each scene you want to fix in the older Unity version, check the checkbox next to the scene name and click the Save Hierarchy(ies) button. This will write a text file named {Scene Name}.txt into a folder called SceneHierarchies in your user home folder, where {Scene Name} is the name of each selected scene. 
##Restoring the desired sibling order
In the older version of Unity, load your project. Once again, open the tool and select the scene you wish to fix. Then click the Fix Up Hierarchy(ies) button. This will read the desired sibling order and update the scene. Do not forget to save the updated scene.
##Additionl Notes
Make sure the projects in thenew and old versions are exactly the same (of course other than variations upon import due to the UNity version differences). There is some output in the Console when fixing up the hierarchy indicating what is being moved or any errors encountered.
