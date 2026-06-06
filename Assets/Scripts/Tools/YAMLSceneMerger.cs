using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Editor utility that parses Unity Scene YAML files and builds
/// a structured representation for diff-based comparison.
///
/// Features:
/// - Parses Scene YAML into GameObject / Component graph
/// - Restores hierarchy including prefab instances
/// - Compares origin scene vs target scenes (Add / Remove / Modify)
///
/// Purpose:
/// Used for detecting structural changes during scene merging workflows.
/// </summary>
#if UNITY_EDITOR
public class YAMLSceneMerger : MonoBehaviour
{
    /// <summary>
    /// Runtime representation of a Unity Scene parsed from YAML.
    /// Stores GameObjects and Components in ID-based lookup tables.
    /// </summary>
    class SceneYAMLData
    {
        public string sceneName;

        public GameObjectYAMLData root = new();
        public Dictionary<string, GameObjectYAMLData> gameobjects = new();
        public Dictionary<string, ComponentYAMLData> components = new();

        /// <summary> Creates a GameObject entry if not already exists. </summary>
        public void AddGameObjectData(string id)
        {
            if (gameobjects.ContainsKey(id)) return;

            GameObjectYAMLData yamlData = gameobjects[id] = new();
            yamlData.m_id = id;
        }
        /// <summary> Registers a GameObject with raw YAML data.  </summary>
        public void AddGameObjectData(string id, string name, string data)
        {
            AddGameObjectData(id);

            GameObjectYAMLData yamlData = gameobjects[id];
            yamlData.m_id = id;
            yamlData.m_name = name;
            yamlData.m_data = data;
        }
        /// <summary> Registers a prefab instance GameObject and links it to its prefab root. </summary>
        public void AddPrefabGameObjectData(string prefabinstance, string id, string name, string data)
        {
            AddGameObjectData(prefabinstance);
            AddGameObjectData(id);

            GameObjectYAMLData yamlData = gameobjects[id];
            yamlData.m_id = id;
            yamlData.m_name = name;
            yamlData.m_data = data;
            yamlData.m_prefabinstance = true;

            yamlData.parent = gameobjects[prefabinstance];
            gameobjects[prefabinstance].childs.Add(yamlData);
        }
        /// <summary> Registers a prefab root instance object in the scene. </summary>
        public void AddPrefabInstanceData(string id, string name, string data)
        {
            AddGameObjectData(id);

            GameObjectYAMLData yamlData = gameobjects[id];
            yamlData.m_id = id;
            yamlData.m_name = name;
            yamlData.m_data = data;
            yamlData.m_prefabinstance = true;
            yamlData.m_prefabroot = true;
        }
        /// <summary> Adds a component entry and links it to its owning GameObject. </summary>
        public void AddComponentData(string gameobject, string id, string type, string data)
        {
            ComponentYAMLData yamlData = new();
            yamlData.m_id = id;
            yamlData.m_gameobject_id = gameobject;
            yamlData.m_type = type;
            yamlData.m_data = data;

            components[id] = yamlData;

            AddGameObjectData(gameobject);
            gameobjects[gameobject].components.Add(yamlData);
        }
        /// <summary> Assigns parent relationship using Transform component reference. </summary>
        public void SetGameObjectParentId(string gameobject_id, string parent_transform_id)
        {
            AddGameObjectData(gameobject_id);
            gameobjects[gameobject_id].m_parent_transform_id = parent_transform_id;
        }
        /// <summary> Assigns parent relationship for prefab instance GameObjects and marks the object as a prefab instance. </summary>
        public void SetPrefabInstanceParentId(string prefabinstance, string parent_transform_id)
        {
            AddGameObjectData(prefabinstance);
            gameobjects[prefabinstance].m_parent_transform_id = parent_transform_id;
            gameobjects[prefabinstance].m_prefabinstance = true;
        }
        /// <summary> Finalizes hierarchy construction after all components are parsed. </summary>
        public void SetParentAll()
        {
            foreach (var go in gameobjects.Values)
            {
                if (go.m_prefabinstance && go.parent != null) continue;

                if (go.m_parent_transform_id == "0")
                {
                    go.parent = root;
                    root.childs.Add(go);
                    continue;
                }

                var parent_id = components[go.m_parent_transform_id].m_gameobject_id;

                go.parent = gameobjects[parent_id];
                gameobjects[parent_id].childs.Add(go);
            }
        }
    }

    /// <summary>
    /// Represents a GameObject parsed from Unity Scene YAML.
    /// Includes hierarchy, prefab state, and raw YAML data.
    /// </summary>
    class GameObjectYAMLData
    {
        public string m_id;
        public string m_name;
        public string m_data;
        public bool m_prefabinstance = false;
        public bool m_prefabroot = false;
        public string m_parent_transform_id;

        public GameObjectYAMLData parent = null;
        public List<GameObjectYAMLData> childs = new();
        public List<ComponentYAMLData> components = new();
    }

    /// <summary>
    /// Represents a Unity Component within a GameObject.
    /// Stores raw YAML block for diff comparison.
    /// </summary>
    class ComponentYAMLData
    {
        public string m_id;
        public string m_gameobject_id;
        public string m_type;
        public string m_data;
    }

    public SceneAsset originScene;
    public List<SceneAsset> newScenes = new();
    public bool useFullName;
    public bool showDetail;

    Dictionary<string, bool> gameobjectChanged = new();

    /// <summary>
    /// Executes scene comparison pipeline:
    /// 1. Parse origin scene
    /// 2. Parse target scenes
    /// 3. Compare structural differences
    /// </summary>
    public void Run()
    {
        ClearConsole();

        gameobjectChanged = new();

        SceneYAMLData originYamlData = SceneToYamlData(originScene);

        List<SceneYAMLData> newYamlDatas = new();
        foreach (var sc in newScenes)
            newYamlDatas.Add(SceneToYamlData(sc));

        foreach (var yaml in newYamlDatas)
        {
            Debug.Log("-----------------------------");
            Debug.Log(yaml.sceneName);

            CompareYAMLData(originYamlData, yaml);
        }
    }

    /// <summary> Converts SceneAsset into structured YAML representation. </summary>
    SceneYAMLData SceneToYamlData(SceneAsset scene)
    {
        SceneYAMLData yamlData = new();
        yamlData.sceneName = scene.name;

        string yaml = SceneToYAML(scene);
        var objects = YamlToObjectBlocks(yaml);
        
        foreach (var obj in objects)
            ConvertBlockData(yamlData, obj);

        yamlData.SetParentAll();

        return yamlData;
    }
    /// <summary> Reads Unity Scene file as raw YAML text. </summary>
    string SceneToYAML(SceneAsset scene)
    {
        string path = AssetDatabase.GetAssetPath(scene);
        string yaml = File.ReadAllText(path);
        yaml = yaml.Replace("\r\n", "\n").TrimEnd();

        return yaml;
    }
    /// <summary> Splits YAML document into object-level blocks. </summary>
    string[] YamlToObjectBlocks(string yaml)
    {
        var blocks = Regex.Split(yaml, @"(?=^--- !u!\d+ &\d+)", RegexOptions.Multiline);

        return blocks;
    }

    /// <summary>
    /// Parses Unity YAML object blocks into structured Scene data.
    ///
    /// Object type routing is based on Unity internal YAML class IDs:
    /// - 1     : GameObject (scene object or prefab instance root)
    /// - 1001  : PrefabInstance metadata (prefab source + hierarchy root)
    /// - other : Component data (Transform, Renderer, etc.)
    ///
    /// This classification is the core dispatch logic of the parser.
    /// </summary>
    void ConvertBlockData(SceneYAMLData yamlData, string block)
    {
        var headerMatch = Regex.Match(block, @"^--- !u!(\d+) &(\d+)", RegexOptions.Multiline);

        if (!headerMatch.Success) return;

        string objType = headerMatch.Groups[1].Value;
        string objId = headerMatch.Groups[2].Value;

        if (objType == "1")
        {
            var prefabinstanceMatch = Regex.Match(block, @"\bm_PrefabInstance:\s*\{fileID:\s*(\d+)\}");
            string prefabinstanceId = "";

            if (prefabinstanceMatch.Success && (prefabinstanceId = prefabinstanceMatch.Groups[1].Value) != "0")
            {   //Prefab Gameobject
                bool isStripped = headerMatch.Value.Contains("stripped");
                var nameMatch = Regex.Match(block, @"m_Name:\s*(.+)");
                var csoMatch = Regex.Match(block, @"m_CorrespondingSourceObject:\s*\{fileID:\s*(\d+),\s*guid:\s*([0-9a-fA-F]+),\s*type:\s*(\d+)\}");
                string objName = "";

                if (csoMatch.Success)
                {
                    string csoFileID = csoMatch.Groups[1].Value;
                    string csoGuid = csoMatch.Groups[2].Value;
                    string csoType = csoMatch.Groups[3].Value;
                    string prefabPath = AssetDatabase.GUIDToAssetPath(csoGuid);
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefabAsset != null)
                        {
                            objName = prefabAsset.name;
                        }
                    }
                }

                yamlData.AddPrefabGameObjectData(prefabinstanceId, objId, objName, block);
            }
            else
            {   //Scene Gameobject
                var nameMatch = Regex.Match(block, @"m_Name:\s*(.+)");
                var objName = nameMatch.Success ? nameMatch.Groups[1].Value : "";

                yamlData.AddGameObjectData(objId, objName, block);
            }
        }
        else if (objType == "1001")
        {   //PrefabInstance
            var sourceprefabMatch = Regex.Match(block, @"m_SourcePrefab:\s*\{fileID:\s*(\d+),\s*guid:\s*([0-9a-fA-F]+),\s*type:\s*(\d+)\}");
            var transformparentMatch = Regex.Match(block, @"\bm_TransformParent:\s*\{fileID:\s*(\d+)\}");
            string objName = "";

            if (sourceprefabMatch.Success)
            {
                string sourceprefabFileID = sourceprefabMatch.Groups[1].Value;
                string sourceprefabGuid = sourceprefabMatch.Groups[2].Value;
                string sourceprefabType = sourceprefabMatch.Groups[3].Value;
                string prefabPath = AssetDatabase.GUIDToAssetPath(sourceprefabGuid);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefabAsset != null)
                    {
                        objName = prefabAsset.name;
                    }
                }
            }

            yamlData.AddPrefabInstanceData(objId, objName, block);

            if (transformparentMatch.Success)
            {
                string transformparentID = transformparentMatch.Groups[1].Value;

                yamlData.SetPrefabInstanceParentId(objId, transformparentID);
            }
        }
        else
        {
            var prefabinstanceMatch = Regex.Match(block, @"\bm_PrefabInstance:\s*\{fileID:\s*(\d+)\}");
            var gameobjectMatch = Regex.Match(block, @"\bm_GameObject:\s*\{fileID:\s*(\d+)\}");
            var typeNameMatch = Regex.Match(block, @"^([^\s][\w]+):\s*$", RegexOptions.Multiline);

            string prefabinstanceID = "";
            bool prefabinstance = prefabinstanceMatch.Success && (prefabinstanceID = prefabinstanceMatch.Groups[1].Value) != "0";
            string gameobjectID = gameobjectMatch.Success ? gameobjectMatch.Groups[1].Value : "";
            string typeName = typeNameMatch.Success ? typeNameMatch.Groups[1].Value : "";

            if ((prefabinstanceID == "" || prefabinstanceID == "0") && (gameobjectID == "" || gameobjectID == "0")) return;

            if (prefabinstance)
            {
                yamlData.AddComponentData(prefabinstanceID, objId, typeName, block);
            }
            else
            {
                yamlData.AddComponentData(gameobjectID, objId, typeName, block);
            }

            if (!prefabinstance && (objType == "4" || objType == "224"))
            {
                var fatherMatch = Regex.Match(block, @"\bm_Father:\s*\{fileID:\s*(\d+)\}");

                if (!fatherMatch.Success) return;

                string fatherID = fatherMatch.Groups[1].Value;

                yamlData.SetGameObjectParentId(gameobjectID, fatherID);
            }
        }
    }

    /// Compares two scene snapshots (origin vs target) and outputs structural diffs.
    ///
    /// Traversal strategy:
    /// - Forward pass: detect Add / Modify
    /// - Prefab objects: deep compare children + components
    /// - Component-level diff for non-prefab objects
    /// - Backward pass: detect Remove
    ///
    /// Note:
    /// Parent-change propagation can create duplicate noise,
    /// so IsParentChanged() is used to suppress redundant logs.
    void CompareYAMLData(SceneYAMLData originYamlData, SceneYAMLData newYamlData)
    {
        foreach (var obj in GetGameObjectHierarchy(newYamlData.root))
        {
            if (obj.m_prefabinstance && !obj.m_prefabroot) continue;

            //add gameobject
            if (!originYamlData.gameobjects.ContainsKey(obj.m_id))
            {
                // Skip noisy diffs caused by hierarchy (parent change propagation)
                if (!showDetail && IsParentChanged(obj)) continue;
                if (gameobjectChanged.ContainsKey(obj.m_id)) continue;

                gameobjectChanged[obj.m_id] = true;
                PrintAddGameobjectMessage(obj);
            }
            else
            {
                var originGo = originYamlData.gameobjects[obj.m_id];
                var newGo = obj;

                //change data
                if (!string.Equals(originGo.m_data, newGo.m_data))
                {
                    // Skip noisy diffs caused by hierarchy (parent change propagation)
                    if (!showDetail && IsParentChanged(obj)) continue;
                    if (gameobjectChanged.ContainsKey(obj.m_id)) continue;

                    gameobjectChanged[obj.m_id] = true;
                    PrintChangeGameobjectMessage(obj);
                }

                if (originGo.m_prefabinstance)
                {   //Prefabinstance Compare
                    foreach (var go in newGo.childs)
                    {
                        if (!originYamlData.gameobjects.ContainsKey(go.m_id) || !string.Equals(go.m_data, originYamlData.gameobjects[go.m_id].m_data))
                        {
                            //prefab gameobject change

                            if (gameobjectChanged.ContainsKey(obj.m_id)) continue;

                            gameobjectChanged[obj.m_id] = true;
                            PrintChangeGameobjectMessage(obj);
                        }
                    }

                    foreach (var comp in newGo.components)
                    {
                        if (!originYamlData.components.ContainsKey(comp.m_id) || !string.Equals(comp.m_data, originYamlData.components[comp.m_id].m_data))
                        {
                            //prefab component change

                            if (gameobjectChanged.ContainsKey(obj.m_id)) continue;

                            gameobjectChanged[obj.m_id] = true;
                            PrintChangeGameobjectMessage(obj);
                        }
                    }
                }
                else
                {   //Component Compare
                    foreach (var comp in newGo.components)
                    {
                        if (!originYamlData.components.ContainsKey(comp.m_id))
                        {   //component add
                            if (!showDetail) continue;
                            if (gameobjectChanged.ContainsKey(newGo.m_id)) continue;

                            PrintAddGameobjectComponentMessage(newGo, comp);
                        }
                        else if (!string.Equals(comp.m_data, originYamlData.components[comp.m_id].m_data))
                        {   //component change
                            if (!showDetail) continue;
                            if (gameobjectChanged.ContainsKey(newGo.m_id)) continue;

                            PrintChangeGameobjectComponentMessage(newGo, comp);
                        }
                    }

                    foreach (var comp in originGo.components)
                    {
                        if (!newYamlData.components.ContainsKey(comp.m_id))
                        {   //component remove
                            if (!showDetail) continue;
                            if (gameobjectChanged.ContainsKey(newGo.m_id)) continue;

                            PrintRemoveGameobjectComponentMessage(originGo, comp);
                        }
                    }
                }
            }
        }

        foreach (var obj in GetGameObjectHierarchy(originYamlData.root))
        {
            if (obj.m_prefabinstance && !obj.m_prefabroot) continue;

            //remove gameobject
            if (!newYamlData.gameobjects.ContainsKey(obj.m_id))
            {
                // Skip noisy diffs caused by hierarchy (parent change propagation)
                if (!showDetail && IsParentChanged(obj)) continue;
                if (gameobjectChanged.ContainsKey(obj.m_id)) continue;

                gameobjectChanged[obj.m_id] = true;
                PrintRemoveGameobjectMessage(obj);
            }
        }
    }

    int GetGameobjectDepth(GameObjectYAMLData gameobject)
    {
        int n = 0;
        GameObjectYAMLData go = gameobject;
        while (go.parent != null)
        {
            n++;
            go = go.parent;
        }
        return n;
    }
    string GetFullGameobjectName(GameObjectYAMLData gameobject)
    {
        List<string> names = new();
        GameObjectYAMLData go = gameobject;
        while (go.parent != null)
        {
            names.Add(go.m_name);
            go = go.parent;
        }
        names.Reverse();

        return string.Join(">", names);
    }
    GameObjectYAMLData GetDepthParent(GameObjectYAMLData gameobject, int depth)
    {
        int obj_depth = GetGameobjectDepth(gameobject);
        var obj = gameobject;

        for (; obj_depth > depth; obj_depth--)
            obj = obj.parent;

        return obj;
    }

    /// <summary>
    /// Checks whether this object is affected by a parent-level change.
    ///
    /// In Unity YAML, transform or hierarchy changes on a parent
    /// can cascade and appear as changes on all children.
    /// This method prevents duplicate diff logs by detecting
    /// whether any ancestor has already been marked as changed.
    /// </summary>
    bool IsParentChanged(GameObjectYAMLData gameobject)
    {
        var obj = gameobject;
        while (obj.parent != null)
        {
            if (gameobjectChanged.ContainsKey(obj.m_id)) return true;
            obj = obj.parent;
        }
        return false;
    }

    List<GameObjectYAMLData> GetGameObjectHierarchy(GameObjectYAMLData root)
    {
        // Flattens hierarchy into a linear list while preserving sibling order.
        // Uses in-place expansion instead of recursion or queue traversal.

        List<GameObjectYAMLData> list = new();
        list.Add(root);
        
        int idx = 0;
        while (idx < list.Count)
        {
            list.InsertRange(idx + 1, list[idx].childs);
            idx++;
        }
        list.RemoveAt(0);

        return list;
    }

    void PrintYaml(SceneAsset scene)
    {
        var yaml = SceneToYAML(scene);
        Debug.Log(yaml);
    }
    void PrintGameObjectDatas(SceneYAMLData yamlData)
    {
        foreach (var obj in yamlData.gameobjects.Values)
        {
            Debug.LogFormat("{0} {1}", obj.m_id, obj.m_name);
        }
    }
    void PrintGameObjectHierarchy(SceneYAMLData yamlData)
    {
        var list = GetGameObjectHierarchy(yamlData.root);
        foreach (var obj in list)
        {
            int depth = GetGameobjectDepth(obj);

            Debug.LogFormat("{0}{1}", string.Concat(Enumerable.Repeat("    ", depth)), obj.m_name);
        }
    }
    void PrintAddGameobjectMessage(GameObjectYAMLData gameobject)
    {
        string name = useFullName ? GetFullGameobjectName(gameobject) : gameobject.m_name;

        if (gameobject.m_prefabinstance)
            Debug.LogFormat("[추가 (Prefab)] \t\t\t{0}", name);
        else Debug.LogFormat("[추가] \t\t\t\t{0}", name);
    }
    void PrintRemoveGameobjectMessage(GameObjectYAMLData gameobject)
    {
        string name = useFullName ? GetFullGameobjectName(gameobject) : gameobject.m_name;

        if (gameobject.m_prefabinstance)
            Debug.LogFormat("[삭제 (Prefab)] \t\t\t{0}", name);
        else Debug.LogFormat("[삭제] \t\t\t\t{0}", name);
    }
    void PrintChangeGameobjectMessage(GameObjectYAMLData gameobject)
    {
        string name = useFullName ? GetFullGameobjectName(gameobject) : gameobject.m_name;

        if (gameobject.m_prefabinstance)
            Debug.LogFormat("[수정 (Prefab)] \t\t\t{0}", name);
        else Debug.LogFormat("[수정] \t\t\t\t{0}", name);
    }
    void PrintAddGameobjectComponentMessage(GameObjectYAMLData gameobject, ComponentYAMLData component)
    {
        string name = useFullName ? GetFullGameobjectName(gameobject) : gameobject.m_name;
        string type = component.m_type;

        Debug.LogFormat("[추가 (component)] \t\t{0} ({1})", name, type);
    }
    void PrintRemoveGameobjectComponentMessage(GameObjectYAMLData gameobject, ComponentYAMLData component)
    {
        string name = useFullName ? GetFullGameobjectName(gameobject) : gameobject.m_name;
        string type = component.m_type;

        Debug.LogFormat("[삭제 (component)] \t\t{0} ({1})", name, type);
    }
    void PrintChangeGameobjectComponentMessage(GameObjectYAMLData gameobject, ComponentYAMLData component)
    {
        string name = useFullName ? GetFullGameobjectName(gameobject) : gameobject.m_name;
        string type = component.m_type;

        Debug.LogFormat("[수정 (component)] \t\t{0} ({1})", name, type);
    }

    void ClearConsole()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);
    }
}

[CustomEditor(typeof(YAMLSceneMerger), true)]
public class YAMLSceneMergerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        YAMLSceneMerger obj = (YAMLSceneMerger)target;

        if (GUILayout.Button("Run"))
        {
            obj.Run();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif