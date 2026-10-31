using System;
using StoryTell.PhysicalMap;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StoryTell.MainIntegration
{
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
                    bool active=i==ActiveDestination;var image=zones[i].marker.GetComponent<Image>();if(image)image.color=active?new Color(1,.65f,.18f):new Color(.14f,.12f,.09f);
                    var text=zones[i].marker.GetComponentInChildren<TMP_Text>();if(text)text.color=active?Color.black:Color.white;
                }
                if(legend)legend.text="1 · Equipo / Diego\n2 · Mateo\n3 · Puesto avanzado\n4 · Frente\n\n"+objective.CurrentObjective+"\n[M] Cerrar mapa";
            }
            var area=(RectTransform)overlay.transform;
            foreach(var zone in zones)
            {
                if(zone.location==null)continue;
                bool mapped=reference.TryWorldToMapPosition(zone.location.position,out var position,out bool inside);
                Vector3 viewport=mapCamera.WorldToViewportPoint(position);
                bool visible=mapped&&inside&&viewport.z>0;
                zone.marker.gameObject.SetActive(visible);zone.leader.gameObject.SetActive(visible);
                if(!visible)continue;
                Vector2 point=new Vector2(viewport.x,viewport.y);
                zone.marker.anchorMin=zone.marker.anchorMax=point;
                zone.marker.anchoredPosition=zone.labelOffset;
                zone.leader.anchorMin=zone.leader.anchorMax=point;
                zone.leader.anchoredPosition=zone.labelOffset*0.5f;
                zone.leader.sizeDelta=new Vector2(zone.labelOffset.magnitude,2);
                zone.leader.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(zone.labelOffset.y,zone.labelOffset.x)*Mathf.Rad2Deg);
            }
        }
    }
}
