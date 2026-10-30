using Systems.DialogueSystem.Scripts;
using UnityEngine;
namespace StoryTell.MainIntegration
{
    [DefaultExecutionOrder(200)]
    public sealed class DialoguePlayerLock:MonoBehaviour
    {
        [SerializeField] private DialogueManagerScript dialogue;
        [SerializeField] private Behaviour[] controls;
        private bool[] previous;private bool locked;
        private CursorLockMode cursorLock;private bool cursorVisible;
        private void LateUpdate()
        {
            bool block=dialogue&&dialogue.IsInConversation;if(block&&!locked){previous=new bool[controls.Length];for(int i=0;i<controls.Length;i++)if(controls[i]){previous[i]=controls[i].enabled;controls[i].enabled=false;}cursorLock=Cursor.lockState;cursorVisible=Cursor.visible;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;locked=true;}
            else if(!block)Release();
        }
        private void Release(){if(!locked)return;locked=false;for(int i=0;i<controls.Length;i++)if(controls[i])controls[i].enabled=previous[i];Cursor.lockState=cursorLock;Cursor.visible=cursorVisible;}
        private void OnDisable()=>Release();
    }
}
