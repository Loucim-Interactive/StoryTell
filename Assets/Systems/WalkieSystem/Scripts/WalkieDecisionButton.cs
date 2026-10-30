using Systems.DecisionSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace Systems.WalkieSystem.Scripts {
    public class WalkieDecisionButton : MonoBehaviour,IPointerClickHandler,IPointerEnterHandler
    {
        [Header("Refs")]
        [SerializeField] private TextMeshProUGUI _textMesh;
        [SerializeField] private GameObject _selectedBackground;

        [Header("Settings")]
        [SerializeField] private Color _selectedTextColor = Color.black;
        [SerializeField] private Color _unselectedTextColor = Color.white;
        private Action select,confirm;
        private bool hasSelection,selectedState;
        private bool sizing;
        public void Bind(Action onSelect,Action onConfirm){select=onSelect;confirm=onConfirm;}
        public void OnPointerEnter(PointerEventData data)=>select?.Invoke();
        public void OnPointerClick(PointerEventData data){if(data.button==PointerEventData.InputButton.Left)confirm?.Invoke();}

        public void Setup(string text) {
            _textMesh.text = text;
            _textMesh.textWrappingMode=TextWrappingModes.Normal;_textMesh.overflowMode=TextOverflowModes.Overflow;
            Resize();
            SetSelected(false);
        }
        private void OnRectTransformDimensionsChange()=>Resize();
        private void Resize()
        {
            if(sizing||!_textMesh)return;sizing=true;
            try{var rect=(RectTransform)transform;var parent=rect.parent as RectTransform;float width=Mathf.Max(260,(parent?parent.rect.width:rect.rect.width)-40);var layout=GetComponent<LayoutElement>();if(!layout)layout=gameObject.AddComponent<LayoutElement>();float height=Mathf.Max(58,_textMesh.GetPreferredValues(_textMesh.text,width,10000).y+24);if(Mathf.Abs(layout.preferredHeight-height)>.1f){layout.preferredHeight=height;layout.minHeight=height;}}
            finally{sizing=false;}
        }

        public void SetSelected(bool selected) {
            if(hasSelection&&selected==selectedState)return;hasSelection=true;selectedState=selected;
            _selectedBackground.SetActive(selected);
            _textMesh.color = selected ? _selectedTextColor : _unselectedTextColor;
            if(selected)
            {
                var scroll=GetComponentInParent<ScrollRect>();
                if(scroll&&scroll.viewport&&scroll.content&&scroll.gameObject.activeInHierarchy)
                {
                    Canvas.ForceUpdateCanvases();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,transform);var view=scroll.viewport.rect;var position=scroll.content.anchoredPosition;
                    if(bounds.min.y<view.yMin)position.y+=view.yMin-bounds.min.y;
                    if(bounds.max.y>view.yMax)position.y-=bounds.max.y-view.yMax;
                    scroll.content.anchoredPosition=position;
                }
            }
        }
    }
}
