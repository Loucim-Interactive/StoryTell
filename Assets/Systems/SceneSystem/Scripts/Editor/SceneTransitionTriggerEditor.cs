using UnityEditor;
using UnityEngine;

namespace SceneSystem.Scripts.Editor
{
    [CustomEditor(typeof(SceneTransitionTrigger))]
    public class SceneTransitionTriggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SceneTransitionTrigger trigger = (SceneTransitionTrigger)target;

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Trigger Scene Transition"))
                    trigger.Trigger();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Manual trigger is available in Play Mode.", MessageType.Info);
        }
    }
}
