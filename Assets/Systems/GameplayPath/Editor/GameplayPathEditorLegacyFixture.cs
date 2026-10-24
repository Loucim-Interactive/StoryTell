#if UNITY_EDITOR
using System.Linq;
using StoryTell.PhysicalMap;
using StoryTell.Weapons;
using UnityEditor;
using UnityEngine;
namespace StoryTell.GameplayPath.Editor
{
    // Legacy trial tests exercise the reusable weapon prototype in an isolated Play Mode fixture.
    // The saved Level1 keeps the new quest-owned weapon gate and enemy-owned ending.
    public static class GameplayPathEditorLegacyFixture
    {
        public static void RestoreTrialForLegacyTests()
        {
            var path=Object.FindAnyObjectByType<GameplayPathController>();if(path==null)return;
            var player=path.Player;path.gameObject.SetActive(false);
            var trial=Object.FindAnyObjectByType<Level1WeaponTrial>();var serialized=new SerializedObject(trial);
            serialized.FindProperty("gameplayPath").objectReferenceValue=null;serialized.ApplyModifiedPropertiesWithoutUndo();
            var zone=(BoxCollider)serialized.FindProperty("useZone").objectReferenceValue;
            zone.transform.position=player.position;zone.center=Vector3.up*2;zone.size=new Vector3(36,12,36);
            foreach(var pickup in Resources.FindObjectsOfTypeAll<WeaponTrialPickup>().Where(x=>x.gameObject.scene.IsValid()))pickup.gameObject.SetActive(true);
            foreach(var target in Resources.FindObjectsOfTypeAll<WeaponCylinderTarget>().Where(x=>x.gameObject.scene.IsValid()))target.gameObject.SetActive(true);
            Object.FindAnyObjectByType<PhysicalMapController>().enabled=true;
        }
    }
}
#endif
