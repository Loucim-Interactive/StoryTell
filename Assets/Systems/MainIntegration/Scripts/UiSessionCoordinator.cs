using System;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    public enum UiMode { None, Map, Dialogue, Inspection, Puzzle, Radio, Pause }

    // A single owner restores the controls it acquired. Closing another view cannot unlock them.
    [DefaultExecutionOrder(-400)]
    public sealed class UiSessionCoordinator : MonoBehaviour
    {
        [SerializeField] private Behaviour[] movementControls, interactionControls;
        private Behaviour[] lockedControls;
        private bool[] previous;
        private UnityEngine.Object owner;
        private CursorLockMode savedCursor;
        private bool savedVisible;
        public UiMode Mode { get; private set; }
        public int LastClosedFrame { get; private set; } = -1;
        public event Action Changed;

        public bool TryAcquire(UiMode mode, UnityEngine.Object requester)
        {
            if (!isActiveAndEnabled || !requester || mode == UiMode.None) return false;
            if (Mode != UiMode.None) return owner == requester && Mode == mode;
            owner = requester;
            Mode = mode;
            savedCursor = Cursor.lockState;
            savedVisible = Cursor.visible;
            bool fullLock = mode == UiMode.Dialogue || mode == UiMode.Inspection || mode == UiMode.Puzzle || mode == UiMode.Pause;
            lockedControls = fullLock ? movementControls : interactionControls;
            previous = new bool[lockedControls.Length];
            for (int i = 0; i < lockedControls.Length; i++)
            {
                if (!lockedControls[i]) continue;
                previous[i] = lockedControls[i].enabled;
                lockedControls[i].enabled = false;
            }
            bool pointer = mode == UiMode.Dialogue || mode == UiMode.Puzzle || mode == UiMode.Pause;
            Cursor.lockState = pointer ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = pointer;
            Changed?.Invoke();
            return true;
        }

        public void Release(UnityEngine.Object requester)
        {
            if (Mode == UiMode.None || owner != requester) return;
            Restore();
        }

        private void Restore()
        {
            if (Mode == UiMode.None) return;
            for (int i = 0; i < lockedControls.Length; i++)
                if (lockedControls[i]) lockedControls[i].enabled = previous[i];
            Mode = UiMode.None;
            owner = null;
            LastClosedFrame = Time.frameCount;
            Cursor.lockState = savedCursor;
            Cursor.visible = savedVisible;
            Changed?.Invoke();
        }
        private void OnDisable() => Restore();
        private void LateUpdate()
        {
            if(Mode!=UiMode.None&&!owner)Restore();
            bool transitioning=SceneSystem.Scripts.SceneTransitionManager.Instance&&SceneSystem.Scripts.SceneTransitionManager.Instance.IsTransitioning;
            if(transitioning&&Mode==UiMode.None)TryAcquire(UiMode.Pause,this);
            else if(!transitioning&&owner==this)Release(this);
        }
    }
}
