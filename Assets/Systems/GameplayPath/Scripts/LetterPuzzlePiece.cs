using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StoryTell.GameplayPath
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LetterPuzzlePiece : MaskableGraphic, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int slotIndex;
        public LetterAssemblyPuzzle owner;
        private Vector3 grabOffset;
        public bool Placed { get; private set; }
        protected override void OnEnable(){useLegacyMeshGeneration=false;base.OnEnable();}

        public void OnPointerDown(PointerEventData data) { if (!Placed) owner.Select(this); }
        public void OnBeginDrag(PointerEventData data)
        {
            if (Placed) return;
            owner.Select(this);transform.SetAsLastSibling();
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform.parent, data.position, data.pressEventCamera, out var world)) grabOffset = transform.position - world;
        }
        public void OnDrag(PointerEventData data)
        {
            if (!Placed && RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform.parent, data.position, data.pressEventCamera, out var world))
                transform.position = world + grabOffset;
        }
        public void OnEndDrag(PointerEventData data)
        {
            if(Placed||owner.TrySnap(this))return;
            var parent=(RectTransform)transform.parent;var point=rectTransform.localPosition;
            float angle=rectTransform.localEulerAngles.z*Mathf.Deg2Rad;
            float halfX=(Mathf.Abs(Mathf.Cos(angle))*rectTransform.rect.width+Mathf.Abs(Mathf.Sin(angle))*rectTransform.rect.height)*0.5f;
            float halfY=(Mathf.Abs(Mathf.Sin(angle))*rectTransform.rect.width+Mathf.Abs(Mathf.Cos(angle))*rectTransform.rect.height)*0.5f;
            point.x=Mathf.Clamp(point.x,parent.rect.xMin+halfX+12,parent.rect.xMax-halfX-12);
            point.y=Mathf.Clamp(point.y,parent.rect.yMin+halfY+150,parent.rect.yMax-halfY-150);
            rectTransform.localPosition=point;
        }
        public void MarkPlaced() { Placed = true; raycastTarget = false; color = new Color(0.94f,0.89f,0.76f); }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();Rect rect = GetPixelAdjustedRect();
            mesh.AddVert(new Vector3(rect.center.x,rect.center.y),color,Vector2.one*0.5f);
            int col=slotIndex%2,row=slotIndex/2;
            const int samples=8,count=samples*4;
            for(int side=0;side<4;side++)for(int sample=0;sample<samples;sample++)
            {
                float t=sample/(float)samples;Vector2 point;
                if(side==0){point=new Vector2(Mathf.Lerp(-0.5f,0.5f,t),0.5f);if(row==1)point.y+=Tear((col+t)*0.5f);}
                else if(side==1){point=new Vector2(0.5f,Mathf.Lerp(0.5f,-0.5f,t));if(col==0)point.x+=Tear((2-row-t)*0.5f);}
                else if(side==2){point=new Vector2(Mathf.Lerp(0.5f,-0.5f,t),-0.5f);if(row==0)point.y+=Tear((col+1-t)*0.5f);}
                else{point=new Vector2(-0.5f,Mathf.Lerp(-0.5f,0.5f,t));if(col==1)point.x+=Tear((1-row+t)*0.5f);}
                mesh.AddVert(new Vector3(rect.center.x+point.x*rect.width,rect.center.y+point.y*rect.height),color,point+Vector2.one*0.5f);
            }
            for(int i=0;i<count;i++)mesh.AddTriangle(0,i+1,(i+1)%count+1);
        }
        private static float Tear(float coordinate)=>Mathf.Sin(coordinate*Mathf.PI*8)*0.045f+Mathf.Sin(coordinate*Mathf.PI*20)*0.012f;
    }
}
