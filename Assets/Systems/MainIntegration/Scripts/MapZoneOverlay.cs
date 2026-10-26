using System;
using StoryTell.PhysicalMap;
using UnityEngine;

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
        public Zone[] Zones => zones;
        private void LateUpdate(){Refresh();}
        public void Refresh()
        {
            bool open=map!=null&&map.enabled&&map.IsOpen;
            if(overlay.activeSelf!=open)overlay.SetActive(open);
            if(!open||mapCamera==null)return;
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
