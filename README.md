# What is this tool for?
When opening a Unity project in an older Unity version, the sibling order of prefabs may not be preserved. This tool can be used in the newer, working Unity version of the project to save the sibling ordering into a text file. Subsequently the same tool may be used in an older UNity version to reorder the siblings to match the original by reading the order from the formerly saved text file. 
#Does it do anything else?
No, it will only reorder GameObject siblings as described above. If there are other issues between Unity versions, this tool will not help.
#Usage
##Installation
Copy the HierarchyFixerUpperTool.cs script to a directory named Editor inside your project folder. After it is imported you should see a menu entry for Tool/Hierarchy Fixer Upper.
