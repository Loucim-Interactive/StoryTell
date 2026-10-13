#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace StoryTell.PhysicalMap.Tests
{
public sealed class PhysicalMapWorldReferenceTests
{
    private const string PrefabPath = "Assets/Systems/PhysicalMapSystem/Prefabs/PhysicalMap.prefab";
    private const string MaterialPath = "Assets/Systems/PhysicalMapSystem/Materials/PaperMapMaterial.mat";

    private GameObject mapObject;
    private GameObject originObject;
    private MapSettings settings;

    [SetUp]
    public void SetUp()
    {
        mapObject = new GameObject("MapTest");
        originObject = new GameObject("WorldOriginTest");
        settings = ScriptableObject.CreateInstance<MapSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(mapObject);
        Object.DestroyImmediate(originObject);
        Object.DestroyImmediate(settings);
    }

    [Test]
    public void WorldCenter_MapsToPaperCenter()
    {
        PhysicalMapController controller = mapObject.AddComponent<PhysicalMapController>();
        controller.Configure(settings);

        MapWorldReference reference = mapObject.AddComponent<MapWorldReference>();
        reference.Configure(originObject.transform, new Vector2(100f, 200f), controller);

        bool mapped = reference.TryWorldToMapPosition(
            originObject.transform.position,
            out Vector3 mapPosition,
            out bool isInside);

        Assert.That(mapped, Is.True);
        Assert.That(isInside, Is.True);
        Assert.That(mapPosition.x, Is.EqualTo(mapObject.transform.position.x).Within(0.001f));
        Assert.That(mapPosition.z, Is.EqualTo(mapObject.transform.position.z).Within(0.001f));
    }

    [Test]
    public void WorldCorner_MapsToPaperCorner()
    {
        PhysicalMapController controller = mapObject.AddComponent<PhysicalMapController>();
        controller.Configure(settings);

        MapWorldReference reference = mapObject.AddComponent<MapWorldReference>();
        reference.Configure(originObject.transform, new Vector2(100f, 200f), controller);

        Vector3 worldCorner = originObject.transform.position +
                              originObject.transform.right * 50f +
                              originObject.transform.forward * 100f;

        reference.TryWorldToMapPosition(worldCorner, out Vector3 mapPosition, out bool isInside);

        Vector3 localPosition = mapObject.transform.InverseTransformPoint(mapPosition);
        Assert.That(isInside, Is.True);
        Assert.That(localPosition.x, Is.EqualTo(settings.width * 0.5f).Within(0.001f));
        Assert.That(localPosition.z, Is.EqualTo(settings.height * 0.5f).Within(0.001f));
    }

    [Test]
    public void OutsideWorldArea_IsReportedAndClamped()
    {
        PhysicalMapController controller = mapObject.AddComponent<PhysicalMapController>();
        controller.Configure(settings);

        MapWorldReference reference = mapObject.AddComponent<MapWorldReference>();
        reference.Configure(originObject.transform, new Vector2(100f, 100f), controller);

        reference.TryWorldToMapPosition(new Vector3(500f, 0f, 0f), out Vector3 mapPosition, out bool isInside);

        Vector3 localPosition = mapObject.transform.InverseTransformPoint(mapPosition);
        Assert.That(isInside, Is.False);
        Assert.That(localPosition.x, Is.EqualTo(settings.width * 0.5f).Within(0.001f));
    }

    [Test]
    public void PhysicalMapPrefab_HasRequiredRuntimeComponents()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<PhysicalMapController>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<MapPhysicsController>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<MapWindReceiver>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<PaperDeformationController>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<MapWorldReference>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<Rigidbody>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<BoxCollider>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
        Assert.That(prefab.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<MapMarkerController>(), Is.Not.Null);
    }

    [Test]
    public void PaperMaterial_UsesStoryTellPhysicalMapShader()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        Assert.That(material, Is.Not.Null);
        Assert.That(material.shader, Is.Not.Null);
        Assert.That(material.shader.name, Is.EqualTo("StoryTell/PhysicalMap/PaperDeformation"));
    }
}
}
#endif
