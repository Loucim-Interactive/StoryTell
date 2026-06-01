using UnityEditor;
using UnityEngine;

namespace SceneSystem.Scripts.Editor
{
    [CustomEditor(typeof(SceneTransitionManager))]
    public class SceneTransitionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SceneTransitionManager manager = (SceneTransitionManager)target;

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying || manager.IsTransitioning))
            {
                if (GUILayout.Button("Trigger Manual Transition"))
                    manager.TriggerManualTransition();

                if (GUILayout.Button("Preview Transition Only"))
                    manager.PreviewTransition();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Manual transition buttons are available in Play Mode.", MessageType.Info);
        }
    }
}
