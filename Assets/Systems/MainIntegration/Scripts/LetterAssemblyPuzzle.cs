using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StoryTell.MainIntegration
{
    public sealed class LetterAssemblyPuzzle : MonoBehaviour
    {
        [SerializeField] private MainPresentation controller;
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform board;
        [SerializeField] private RectTransform[] slots;
        [SerializeField] private LetterPuzzlePiece[] pieces;
        [SerializeField] private TMP_Text status;
        private readonly LetterAssemblyState state = new();
        private LetterPuzzlePiece selected;
        private InputAction rotate, close;
        private CursorLockMode previousCursor;
        private bool previousVisible;
        private int openedFrame;
        public bool IsOpen { get; private set; }
        public int PlacedCount => state.PlacedCount;
        public event System.Action ProgressChanged;
        public bool Complete => state.Complete;
        public int PlacedMask => state.Mask;
        public void RestorePlaced(int mask)
        {
            state.Restore(mask);
            foreach(var piece in pieces)if(state.IsPlaced(piece.slotIndex))PlaceVisual(piece);
        }
        public RectTransform Slot(int index) => slots[index];

        private void OnEnable()
        {
            rotate ??= new InputAction("RotateLetterPiece",InputActionType.Button,"<Keyboard>/r");
            if(rotate.bindings.Count==1)rotate.AddBinding("<Mouse>/rightButton");
            close ??= new InputAction("CloseLetterPuzzle",InputActionType.Button,"<Keyboard>/escape");
            if(close.bindings.Count==1)close.AddBinding("<Keyboard>/e");
            rotate.Enable();close.Enable();
        }
        private void OnDisable(){Close();rotate?.Disable();close?.Disable();}
        private void OnDestroy(){rotate?.Dispose();close?.Dispose();}
        public void Open()
        {
            if(IsOpen)return;
            foreach(var piece in pieces){var text=piece.GetComponentInChildren<TMP_Text>(true);if(text){text.text=controller.LetterFragmentText(piece.slotIndex);text.gameObject.SetActive(!piece.HasArtwork);}}
            IsOpen=true;openedFrame=Time.frameCount;panel.SetActive(true);
            previousCursor=Cursor.lockState;previousVisible=Cursor.visible;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            UpdateStatus();
        }
        public void Close()
        {
            if(!IsOpen)return;
            IsOpen=false;panel.SetActive(false);selected=null;
            Cursor.lockState=previousCursor;Cursor.visible=previousVisible;
            if(controller!=null)controller.EndLetterAssembly();
        }
        public void Select(LetterPuzzlePiece piece)
        {
            if(piece==null||piece.Placed)return;
            if(selected!=null&&!selected.Placed)selected.color=new Color(0.94f,0.89f,0.76f);
            selected=piece;selected.color=new Color(1,0.96f,0.83f);
        }
        public void RotateSelected()
        {
            if(!IsOpen||selected==null||selected.Placed)return;
            selected.rectTransform.Rotate(0,0,90);TrySnap(selected);
        }
        private void Update()
        {
            if(!IsOpen)return;
            if(rotate.WasPressedThisFrame())RotateSelected();
            if(Time.frameCount>openedFrame+1&&close.WasPressedThisFrame())Close();
        }
        public bool TrySnap(LetterPuzzlePiece piece)
        {
            if(!IsOpen||piece==null||piece.Placed)return false;
            var target=slots[piece.slotIndex];
            Vector3 difference=board.InverseTransformPoint(piece.transform.position)-board.InverseTransformPoint(target.position);
            if(!state.TryPlace(piece.slotIndex,new Vector2(difference.x,difference.y).magnitude,piece.rectTransform.localEulerAngles.z))return false;
            PlaceVisual(piece);
            if(selected==piece)selected=null;
            if(state.Complete)controller.CompleteLetterAssembly();
            ProgressChanged?.Invoke();
            UpdateStatus();return true;
        }
        private void PlaceVisual(LetterPuzzlePiece piece)
        {
            var target=slots[piece.slotIndex];piece.transform.SetParent(board,false);piece.rectTransform.anchorMin=target.anchorMin;piece.rectTransform.anchorMax=target.anchorMax;piece.rectTransform.pivot=target.pivot;piece.rectTransform.sizeDelta=target.sizeDelta;piece.rectTransform.anchoredPosition=target.anchoredPosition;piece.rectTransform.localRotation=Quaternion.identity;piece.MarkPlaced();
        }
        private void UpdateStatus()
        {
            status.text=state.Complete?"Carta reconstruida · [E] Volver":"Fragmentos: "+state.PlacedCount+"/4 · Arrastrá · [R / clic derecho] Girar · [E / Esc] Volver";
        }
    }
}
