using System;
using StoryTell.PhysicalMap;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StoryTell.MainIntegration
{
    [DefaultExecutionOrder(650)]
    public sealed class MapZoneOverlay : MonoBehaviour
    {
        [Serializable] public sealed class Zone
        {
            public string name;
            public Transform location;
            public RectTransform marker, leader;
            public Vector2 labelOffset;
        }
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private MapWorldReference reference;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private GameObject overlay;
        [SerializeField] private Zone[] zones;
        [SerializeField] private ObjectiveHud objective;
        [SerializeField] private TMP_Text legend;
        private int lastDestination=-2;
        private string lastObjective;
        private Vector2[] origins, positions;
        private bool[] mappedZones;
        public int ActiveDestination => objective ? objective.DestinationZone : -1;
        public Zone[] Zones => zones;
        private void LateUpdate(){Refresh();}
        public void Refresh()
        {
            bool open=map!=null&&map.enabled&&map.IsOpen;
            if(overlay.activeSelf!=open)overlay.SetActive(open);
            if(!open||mapCamera==null)return;
            if(objective&&(lastDestination!=ActiveDestination||lastObjective!=objective.CurrentObjective))
            {
                lastDestination=ActiveDestination;lastObjective=objective.CurrentObjective;
                for(int i=0;i<zones.Length;i++)
                {
                    bool active=i==ActiveDestination;var image=zones[i].marker.GetComponent<Image>();if(image)image.color=active?new Color(1,.65f,.18f):new Color(.40f,.33f,.21f);
                    var text=zones[i].marker.GetComponentInChildren<TMP_Text>();if(text)text.color=active?Color.black:Color.white;
                }
                if(legend){var entries=new System.Text.StringBuilder();for(int i=0;i<zones.Length;i++)entries.Append(i+1).Append(" · ").AppendLine(zones[i].name);legend.text=entries+"\n"+objective.CurrentObjective+"\n\nNorte →\nCeleste · Vos\nNaranja · Destino actual\n[M] Cerrar mapa";}
            }
            var area=(RectTransform)overlay.transform;
            var canvas=area.GetComponentInParent<Canvas>();
            if(canvas)canvas=canvas.rootCanvas;
            var uiCamera=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            var pixels=canvas?canvas.pixelRect:new Rect(0,0,Screen.width,Screen.height);
            var heldView=map.GetComponent<StoryTell.PhysicalMap.PhysicalMapViewCamera>();
            Vector2 ToCanvas(Vector3 world)
            {
                var viewport=mapCamera.WorldToViewportPoint(world);
                var screen=heldView?heldView.ProjectToScreen(world):pixels.position+Vector2.Scale(new Vector2(viewport.x,viewport.y),pixels.size);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area,screen,uiCamera,out var result);return result;
            }
            var minimum=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var maximum=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            for(int cornerIndex=0;cornerIndex<4;cornerIndex++)
            {
                float x=(cornerIndex&1)==0?-.5f:.5f,z=(cornerIndex&2)==0?-.5f:.5f;
                var corner=ToCanvas(map.transform.TransformPoint(new Vector3(map.Settings.width*x,0,map.Settings.height*z)));
                minimum=Vector2.Min(minimum,corner);maximum=Vector2.Max(maximum,corner);
            }
            Vector2 KeepOnPaper(Vector2 point)=>new Vector2(Mathf.Clamp(point.x,minimum.x+24,maximum.x-24),Mathf.Clamp(point.y,minimum.y+24,maximum.y-24));
            bool hasPlayer=map.ViewAnchor&&reference.TryWorldToMapPosition(map.ViewAnchor.position,out _,out bool insidePlayer)&&insidePlayer;
            Vector2 playerPoint=Vector2.zero;
            if(hasPlayer){reference.TryWorldToMapPosition(map.ViewAnchor.position,out var playerMap,out _);playerPoint=ToCanvas(playerMap);}
            if(positions==null||positions.Length!=zones.Length){positions=new Vector2[zones.Length];origins=new Vector2[zones.Length];mappedZones=new bool[zones.Length];}
            for(int index=0;index<zones.Length;index++)
            {
                var zone=zones[index];mappedZones[index]=false;
                if(zone.location==null)continue;
                bool mapped=reference.TryWorldToMapPosition(zone.location.position,out var position,out bool inside);
                Vector3 viewport=mapCamera.WorldToViewportPoint(position);
                bool visible=mapped&&inside&&viewport.z>0;
                zone.marker.gameObject.SetActive(visible);zone.leader.gameObject.SetActive(visible);
                if(!visible)continue;
                mappedZones[index]=true;
                var point=ToCanvas(position);
                var labelOffset=zone.labelOffset;
                if(labelOffset.sqrMagnitude<55*55)labelOffset=labelOffset.sqrMagnitude>.01f?labelOffset.normalized*55:Vector2.up*55;
                origins[index]=point;positions[index]=KeepOnPaper(point+labelOffset);
            }
            // Keep nearby camp destinations distinct while retaining leaders to their real locations.
            for(int pass=0;pass<24;pass++)
            {
             for(int i=0;i<zones.Length;i++)for(int j=i+1;j<zones.Length;j++)
             {
                if(!mappedZones[i]||!mappedZones[j])continue;
                var difference=positions[j]-positions[i];float distance=difference.magnitude;
                if(distance>=44)continue;
                var direction=distance>.01f?difference/distance:Vector2.up;
                var correction=direction*((44-distance)*.5f+.05f);positions[i]=KeepOnPaper(positions[i]-correction);positions[j]=KeepOnPaper(positions[j]+correction);
             }
             if(hasPlayer)for(int i=0;i<zones.Length;i++)if(mappedZones[i])
             {
                var offset=positions[i]-playerPoint;if(offset.sqrMagnitude>=50*50)continue;
                var candidate=KeepOnPaper(playerPoint+(offset.sqrMagnitude>.01f?offset.normalized:Vector2.left)*50);
                if((candidate-playerPoint).sqrMagnitude<49*49)candidate=KeepOnPaper(playerPoint+Vector2.right*(offset.x>=0?50:-50));
                positions[i]=candidate;
             }
            }
            for(int i=0;i<zones.Length;i++)
            {
                if(!mappedZones[i])continue;var zone=zones[i];var offset=positions[i]-origins[i];
                zone.marker.anchorMin=zone.marker.anchorMax=Vector2.one*.5f;
                zone.marker.anchoredPosition=positions[i];
                zone.leader.anchorMin=zone.leader.anchorMax=Vector2.one*.5f;
                zone.leader.anchoredPosition=origins[i]+offset*.5f;
                zone.leader.sizeDelta=new Vector2(offset.magnitude,2);
                zone.leader.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(offset.y,offset.x)*Mathf.Rad2Deg);
            }
        }
    }
}
