#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace StoryTell.GameplayPath.Editor
{
    [InitializeOnLoad]
    public static class GameplayPathEditorProbe
    {
        static double next;
        static GameplayPathEditorProbe() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            next = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists("Library/GameplayPath.inspect")) return;
            File.Delete("Library/GameplayPath.inspect");
            try
            {
                var scene = SceneManager.GetActiveScene();
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                File.WriteAllText("Library/GameplayPath-inspection.json", JsonUtility.ToJson(new Snapshot {
                    scene = scene.path, dirty = scene.isDirty, playing = EditorApplication.isPlaying,
                    objects = all.Where(t => t.parent == null || t.name.Contains("Tent") || t.name.Contains("zone") || t.name.Contains("VFX") || t.name.Contains("Player") || t.name.Contains("NPC") || t.name.Contains("Explosion")).Select(t => new Entry { name = t.name, position = t.position, local = t.localPosition, components = string.Join(",",t.GetComponents<Component>().Select(c => c ? c.GetType().Name : "MISSING")) }).ToArray(),
                    terrains = all.Select(t => t.GetComponent<Terrain>()).Where(t => t).Select(t => new Entry { name = t.name, position = t.transform.position, local = t.terrainData.size }).ToArray()
                }, true));
            }
            catch (Exception e) { File.WriteAllText("Library/GameplayPath-inspection.json", e.ToString()); }
        }
        [Serializable] class Snapshot { public string scene; public bool dirty, playing; public Entry[] objects, terrains; }
        [Serializable] class Entry { public string name; public Vector3 position, local; public string components; }
    }
}
#endif
