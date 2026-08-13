using Systems.DecisionSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.WalkieSystem.Scripts {
    public class WalkieDecisionButton : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TextMeshProUGUI _textMesh;
        [SerializeField] private GameObject _selectedBackground;

        [Header("Settings")]
        [SerializeField] private Color _selectedTextColor = Color.black;
        [SerializeField] private Color _unselectedTextColor = Color.white;

        public void Setup(string text) {
            _textMesh.text = text;
            SetSelected(false);
        }

        public void SetSelected(bool selected) {
            _selectedBackground.SetActive(selected);
            _textMesh.color = selected ? _selectedTextColor : _unselectedTextColor;
        }
    }
}
