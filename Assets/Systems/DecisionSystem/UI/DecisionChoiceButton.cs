using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.DecisionSystem.UI
{
    public class DecisionChoiceButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;

        public void Setup(string text, Action onClick)
        {
            label.text = text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}