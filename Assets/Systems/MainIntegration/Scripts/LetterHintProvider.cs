using DialogueSystem.Scripts;
using Systems.Missions.FindObjects;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    public sealed class LetterHintProvider : MonoBehaviour
    {
        [SerializeField] private LetterManager mission;
        [SerializeField] private LetterInteractableScript[] fragments;
        [SerializeField] private ConversationSO[] hints;
        public ConversationSO GetHint()
        {
            if(!mission || !mission.MissionStarted || mission.MissionCompleted || mission.CollectedCount>=fragments.Length)return null;
            for(int i=0;i<fragments.Length&&i<hints.Length;i++)
                if(fragments[i]&&fragments[i].gameObject.activeSelf)return hints[i];
            return null;
        }
    }
}
