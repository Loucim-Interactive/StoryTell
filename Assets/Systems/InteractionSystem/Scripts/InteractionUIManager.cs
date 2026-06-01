using System.Collections.Generic;
using InteractionSystem.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InteractionSystem.Scripts {
    public class InteractionUIManager : MonoBehaviour
    {
        
        [SerializeField] private List<UIInteractionIcon> uiInteractionIcons = new();
        [SerializeField] private TextMeshProUGUI interactionName;
        [SerializeField] private TextMeshProUGUI interactionAction;
        [SerializeField] private Image interactionImage;
        [SerializeField] private GameObject uiObject;
        public static InteractionUIManager Instance { get; private set; }

        private Dictionary<EInteractions, Sprite> _lookup;
        
        private void Awake() {
            if (Instance != null) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupSpriteLookup();
        }

        private void OnDisable() { 
            HideInteraction();
        }

        public void DisplayInteraction(InteractableScript interactionScript) {
            uiObject.SetActive(true);
            UIInteraction interaction = interactionScript.UIInteraction;
            interactionName.text = interactionScript.interactableName;
            interactionAction.text = interaction.label;
            interactionImage.sprite = GetSprite(interaction.interactionType);
        }

        public void HideInteraction() {
            uiObject.SetActive(false);
        }

        private void SetupSpriteLookup() {
            _lookup = new Dictionary<EInteractions, Sprite>(); // basically, we make a Dictionary version of the List<UIInteractionIcon> for easier access
            foreach (var entry in uiInteractionIcons) {
                _lookup[entry.InteractionType] = entry.Icon;
            }
        }

        private Sprite GetSprite(EInteractions interaction) {
            _lookup.TryGetValue(interaction, out Sprite icon);
            return icon;
        }
    }
}
