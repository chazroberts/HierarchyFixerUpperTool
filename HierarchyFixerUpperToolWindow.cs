using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class HierarchyFixerUpperToolWindow :  EditorWindow
{
    [MenuItem("Tools/Hierarchy Fixer Upper")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        var window = EditorWindow.GetWindow(typeof(HierarchyFixerUpperToolWindow));
    }
    
    void OnGUI()
    {
        EditorGUILayout.BeginVertical("Box");
        
        GUILayout.Label("Hierarchy Fixer Upper", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Scenes"))
        {
            rebuildSceneList = true;
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("Save Hierarchy(ies)"))
        {
            DoSaveHierarchies();
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("Fix Up Hierarchy(ies)"))
        {
            DoFixUpHierarchies();
        }
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Scenes:");

        PopulateSceneList();
            
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));
        scenes.ToList().ForEach(sd => { sd.selected = EditorGUILayout.ToggleLeft(sd.name, sd.selected); });
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }

    private void PopulateSceneList()
    {
        if (!rebuildSceneList) return;
        
        scenes.Clear();
        for (var iScene = 0; iScene < SceneManager.sceneCount; iScene++)
        {
            var scene = SceneManager.GetSceneAt(iScene);
            scenes.Add(new SceneData() { name = scene.name, theScene = scene });
        }
        rebuildSceneList = false;
    }

    private void DoSaveHierarchies()
    {
        scenes.Where(sd => sd.selected)
            .ToList()
            .ForEach(sd =>
        {
            HierarchyWalker hw = new HierarchyWalker();
            hw.SaveHierarchy(sd);
        });
    }

    private void DoFixUpHierarchies()
    {
        scenes.Where(sd => sd.selected)
            .ToList()
            .ForEach(sd =>
            {
                HierarchyWalker hw = new HierarchyWalker();
                if (hw.FixupHierarchy(sd))
                {
                    Debug.Log($"<color=cyan>Fixed up scene {sd.name} successfully ({hw.NumChanges} changes)</color>");
                }
                else
                {
                    Debug.Log($"<color=cyan>Errors encountered while fixing up scene {sd.name}</color>");
                }
            });
    }
    
    private List<SceneData> scenes = new ();
    Vector2 scrollPosition = Vector2.zero;
    private bool rebuildSceneList = true;
}

public class HierarchyWalker
{
    public void SaveHierarchy(SceneData sceneData)
    {
        List<string> flattenedScene = new();
        TraverseSceneFlatOutput(sceneData.theScene, flattenedScene);
        SaveFlattenedHierarchyToFile(sceneData.theScene.name, flattenedScene);
    }
    
    private void SaveFlattenedHierarchyToFile(string sceneName, List<string> flattened)
    {
        try
        {
            var outputFolder = GetOutputFolder();
            var outputFile = Path.Combine(outputFolder, $"{sceneName}.txt");
            using var sw = new StreamWriter(outputFile);
            flattened.ForEach(p => sw.WriteLine(p));
            sw.Close();
            Debug.Log($"<color=yellow>Saved hierarchy layout for {sceneName} to:</color> <color=white>{outputFile}</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=yellow>{ex}</color>");
        }
    }
        
    private string GetOutputFolder()
    {
        var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SceneHierarchies");
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        return outputFolder;
    }
    
    private void TraverseSceneFlatOutput(Scene s, List<string> flatPathList)
    {
        var scene = s;
        var rootObjs = scene.GetRootGameObjects();
        foreach (var rootObj in rootObjs)
        {
            ProcessObject(rootObj, flatPathList);
        }
    }
    
    private void ProcessObject(GameObject o, List<string> flatPathList)
    {
        flatPathList.Add(o.transform.GetPath());
        for (var iT = 0; iT < o.transform.childCount; iT++)
        {
            var tC = o.transform.GetChild(iT);
            ProcessObject(tC.gameObject, flatPathList);
        }
    }
    
    public bool FixupHierarchy(SceneData sceneData)
    {
        nChanges = 0;
        var outputFolder = GetOutputFolder();
        var inputFile = Path.Combine(outputFolder, $"{sceneData.theScene.name}.txt");
        List<string> desiredHierarchy = new();
        ReadDesiredHierarchy(inputFile, desiredHierarchy);
        
        for( int iP = 0; iP < desiredHierarchy.Count; iP++)
        {
            var path = desiredHierarchy[iP];
            var newSceneObject = path.FindObject(sceneData.theScene);
            if (newSceneObject == null)
            {
                Debug.LogError($"<color=cyan>Could not find game object with path {path.DecodeGameObjectName()}</color>");
                // NEED TO SEARCH FOR IT?
                // HOW TO MATCH IF NOT BY PARENT?
                return false;
            }
            else
            {
                int expectedIndex = GetSiblingIndex(desiredHierarchy, path);
                if (expectedIndex < 0)
                {
                    Debug.LogError($"<color=cyan>Could not determine sibling index for {path.DecodeGameObjectName()}</color>");
                    return false;
                }

                if (expectedIndex != newSceneObject.transform.GetSiblingIndex())
                {
                    // Have object, and know where it is supposed to go, so put it there.
                    Debug.Log($"<color=cyan>Moving {path.DecodeGameObjectName()} from index {newSceneObject.transform.GetSiblingIndex()} to {expectedIndex}</color>");
                    newSceneObject.transform.SetSiblingIndex(expectedIndex);
                    nChanges++;
                    
                    // Now restart from the parent.
                    var parent = path.GetParent();
                    for (var index = iP; index > 0; index--)
                    {
                        if (parent == desiredHierarchy[index]) break;
                    }
                }
            }
        }
        return true;
    }

    public int NumChanges => nChanges;
    
    private int GetSiblingIndex(List<string> hierarchy, string sibling)
    {
        var iSibling = 0;
        var parent = sibling.GetParent();
        
        foreach (var path in hierarchy)
        {
            if (path == parent) continue;
            if (path == sibling) return iSibling;
            if (path.GetParent()  == sibling.GetParent()) iSibling++; // Only count same direct parent as sibling
        }
        return -1;
    }
    
    private void ReadDesiredHierarchy(string fileName, List<string> flattenedObjectList)
    {
        try
        {
            using StreamReader sr = new StreamReader(fileName);
            do
            {
                flattenedObjectList.Add(sr.ReadLine());
            } 
            while (!sr.EndOfStream);
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=cyan>{ex}</color>");
        }
    }

    private int nChanges;
}

#region AUX classes
public class SceneData
{
    public string name;
    public bool selected;
    public Scene theScene;
}

public static class pathExtensions
{
    public static string DecodeGameObjectName(this string name)
    {
        return name.Replace(objectNameSlashSubtitute, pathSeparator);
    }
    
    public static string GetPath(this Transform current) 
    {
        if (current.parent == null)
        {
            return pathSeparator + current.EncodeGameObjectName();
        }
        return current.parent.GetPath() + pathSeparator + current.EncodeGameObjectName();
    }
    
    public static string GetParent(this string path)
    {
        if (path.LastIndexOf(pathSeparator) < 0) return "";
        return path.Substring(0, path.LastIndexOf(pathSeparator));
    }
    
    public static GameObject FindObject(this string path, Scene scene)
    {
        // Can't use GameObject.Find() because it won't return disabled objects 
        return Resources
            .FindObjectsOfTypeAll<GameObject>()
            .Where(o => o.scene == scene)
            .FirstOrDefault(o => o.transform.GetPath() == path);
    }

    private static string EncodeGameObjectName(this Transform t)
    {
        return EncodeGameObjectName(t.name);
    }
    
    private static string EncodeGameObjectName(string n)
    {
        return n.Replace(pathSeparator, objectNameSlashSubtitute);
    }
    
    private const string pathSeparator = "/";
    private const string objectNameSlashSubtitute = @"\\";
}
#endregion