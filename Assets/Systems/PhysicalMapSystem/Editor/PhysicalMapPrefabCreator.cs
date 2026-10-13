using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StoryTell.PhysicalMap.Editor
{
public static class PhysicalMapPrefabCreator
{
    private const string RootPath = "Assets/Systems/PhysicalMapSystem";
    private const string SettingsPath = RootPath + "/Data/SO_MapSettings_Default.asset";
    private const string MeshPath = RootPath + "/Data/PhysicalMap_50x50_GeneratedMesh.asset";
    private const string MaterialPath = RootPath + "/Materials/PaperMapMaterial.mat";
    private const string PlayerMarkerMaterialPath = RootPath + "/Materials/PlayerMarker.mat";
    private const string ObjectiveMarkerMaterialPath = RootPath + "/Materials/ObjectiveMarker.mat";
    private const string PrefabPath = RootPath + "/Prefabs/PhysicalMap.prefab";

    [MenuItem("StoryTell/Physical Map/Create Default Assets")]
    public static void CreateDefaultAssets()
    {
        CreateOrUpdateDefaultAssets(true);
    }

    [MenuItem("StoryTell/Physical Map/Install In Active Scene")]
    public static void InstallInActiveScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Exit Play Mode before installing the Physical Map.");
            return;
        }

        CreateOrUpdateDefaultAssets(false);

        PhysicalMapController controller = Object.FindFirstObjectByType<PhysicalMapController>();
        if (controller == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "PhysicalMap";
            Undo.RegisterCreatedObjectUndo(instance, "Install Physical Map");
            controller = instance.GetComponent<PhysicalMapController>();
        }

        ConfigureSceneReferences(controller);
        Selection.activeGameObject = controller.gameObject;
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log("Physical Map installed. Assign the final texture and adjust PhysicalMap_WorldOrigin/world size when the level layout is known.", controller);
    }

    [MenuItem("StoryTell/Physical Map/Add Selected As Objective Marker")]
    public static void AddSelectedAsObjectiveMarker()
    {
        Transform target = Selection.activeTransform;
        PhysicalMapController controller = Object.FindFirstObjectByType<PhysicalMapController>();

        if (target == null || controller == null || target.IsChildOf(controller.transform))
        {
            Debug.LogWarning("Select a world object and make sure a Physical Map is installed in the active scene.");
            return;
        }

        MapWorldReference reference = controller.GetComponent<MapWorldReference>();
        if (reference == null)
        {
            Debug.LogWarning("The installed Physical Map does not have a MapWorldReference. Run Install In Active Scene first.");
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(ObjectiveMarkerMaterialPath);
        GameObject marker = CreateMarkerObject("ObjectiveMarker_" + target.name, material, controller.transform);
        marker.GetComponent<MapMarkerController>().Configure(reference, controller, target);
        Undo.RegisterCreatedObjectUndo(marker, "Add Physical Map Objective Marker");
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Selection.activeGameObject = marker;
    }

    private static void CreateOrUpdateDefaultAssets(bool showDialog)
    {
        EnsureFolder(RootPath + "/Data");
        EnsureFolder(RootPath + "/Materials");
        EnsureFolder(RootPath + "/Prefabs");

        MapSettings settings = AssetDatabase.LoadAssetAtPath<MapSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<MapSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        EditorUtility.SetDirty(settings);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("StoryTell/PhysicalMap/PaperDeformation");
            if (shader == null)
                Debug.LogWarning("PaperDeformation shader was not found yet. Unity may still be importing it.");

            material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = "PaperMapMaterial"
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        Material playerMarkerMaterial = GetOrCreateMarkerMaterial(
            PlayerMarkerMaterialPath,
            new Color(0.12f, 0.85f, 1f, 1f));
        GetOrCreateMarkerMaterial(
            ObjectiveMarkerMaterialPath,
            new Color(1f, 0.48f, 0.08f, 1f));

        GameObject root = new GameObject("PhysicalMap");
        MeshFilter meshFilter = root.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = root.AddComponent<MeshRenderer>();
        Rigidbody mapRigidbody = root.AddComponent<Rigidbody>();
        BoxCollider boxCollider = root.AddComponent<BoxCollider>();
        MapPhysicsController physicsController = root.AddComponent<MapPhysicsController>();
        PaperDeformationController deformationController = root.AddComponent<PaperDeformationController>();
        PhysicalMapController controller = root.AddComponent<PhysicalMapController>();
        MapWindReceiver windReceiver = root.AddComponent<MapWindReceiver>();
        MapWorldReference worldReference = root.AddComponent<MapWorldReference>();

        meshRenderer.sharedMaterial = material;
        mapRigidbody.useGravity = true;
        mapRigidbody.mass = 0.08f;
        mapRigidbody.linearDamping = 5f;
        mapRigidbody.angularDamping = 7f;
        boxCollider.size = new Vector3(settings.width, settings.colliderThickness, settings.height);

        SetPrivateObject(controller, "settings", settings);
        SetPrivateObject(physicsController, "settings", settings);
        SetPrivateObject(deformationController, "settings", settings);
        SetPrivateObject(windReceiver, "settings", settings);

        if (Camera.main != null)
            SetPrivateObject(controller, "viewAnchor", Camera.main.transform);

        SetPrivateObject(worldReference, "mapController", controller);
        GameObject playerMarker = CreateMarkerObject("PlayerMarker", playerMarkerMaterial, root.transform);
        MapMarkerController playerMarkerController = playerMarker.GetComponent<MapMarkerController>();
        SetPrivateObject(playerMarkerController, "worldReference", worldReference);
        SetPrivateObject(playerMarkerController, "mapController", controller);
        SetPrivateObject(playerMarkerController, "markerVisual", playerMarker.transform);
        SetPrivateBool(playerMarkerController, "useMainCameraAsFallback", true);

        deformationController.RebuildMesh(settings);

        Mesh meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (meshAsset == null)
        {
            meshAsset = Object.Instantiate(meshFilter.sharedMesh);
            meshAsset.name = "PhysicalMap_50x50_GeneratedMesh";
            AssetDatabase.CreateAsset(meshAsset, MeshPath);
        }

        meshFilter.sharedMesh = meshAsset;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
            EditorUtility.DisplayDialog("Physical Map", "Default PhysicalMap assets created or updated.", "OK");
    }

    private static void ConfigureSceneReferences(PhysicalMapController controller)
    {
        Undo.RecordObject(controller, "Configure Physical Map");

        if (Camera.main != null)
            controller.SetViewAnchor(Camera.main.transform);

        MapWorldReference reference = controller.GetComponent<MapWorldReference>();
        if (reference == null)
            reference = Undo.AddComponent<MapWorldReference>(controller.gameObject);

        GameObject origin = GameObject.Find("PhysicalMap_WorldOrigin");
        if (origin == null)
            origin = GameObject.Find("WorldOrigin");

        if (origin == null)
        {
            origin = new GameObject("PhysicalMap_WorldOrigin");
            Undo.RegisterCreatedObjectUndo(origin, "Create Physical Map World Origin");
        }

        Undo.RecordObject(reference, "Configure Physical Map World Reference");
        reference.Configure(origin.transform, new Vector2(100f, 100f), controller);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(reference);

        if (PrefabUtility.IsPartOfPrefabInstance(controller))
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);

        if (PrefabUtility.IsPartOfPrefabInstance(reference))
            PrefabUtility.RecordPrefabInstancePropertyModifications(reference);
    }

    private static GameObject CreateMarkerObject(string markerName, Material material, Transform parent)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = markerName;
        marker.transform.SetParent(parent, false);
        marker.transform.localScale = new Vector3(0.025f, 0.004f, 0.025f);

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
            Object.DestroyImmediate(markerCollider);

        marker.GetComponent<Renderer>().sharedMaterial = material;
        marker.AddComponent<MapMarkerController>();
        return marker;
    }

    private static Material GetOrCreateMarkerMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogError("Could not create marker material because no supported unlit shader was found.");
            return null;
        }
        material = new Material(shader)
        {
            name = System.IO.Path.GetFileNameWithoutExtension(path),
            color = color
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static void SetPrivateObject(Object target, string fieldName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateBool(Object target, string fieldName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
}
