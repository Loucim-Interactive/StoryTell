using Systems.EventSystem.Scripts;
using Systems.WalkieSystem.Scripts;
using UnityEngine;

namespace Systems.WalkieSystem {
    public class WalkieToggleTest : MonoBehaviour {

        [SerializeField] private GameObject radioObject;
        [SerializeField] private KeyCode toggleKey = KeyCode.F;

        private bool _isVisible;

        private void Awake() {
            SetVisible(false);
        }

        private void Update() {
            if (Input.GetKeyDown(toggleKey)) {
                SetVisible(!_isVisible);
            }
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            radioObject.SetActive(visible);
            GameEventBus.Raise(RadioEvents.VisibilityChanged, visible);
        }
    }
}
