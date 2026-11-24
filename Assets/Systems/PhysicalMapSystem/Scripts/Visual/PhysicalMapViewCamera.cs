using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace StoryTell.PhysicalMap
{
    /// <summary>Keeps the held paper legible in first person, including near the player mesh and trench walls.</summary>
    [DisallowMultipleComponent]
    public sealed class PhysicalMapViewCamera : MonoBehaviour
    {
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private RawImage surface;
        private RenderTexture target;
        public RenderTexture Target=>target;
        private readonly Vector3[] corners=new Vector3[4];
        public Rect DisplayRect
        {
            get
            {
                if(!surface)return mapCamera.pixelRect;
                var canvas=surface.canvas.rootCanvas;var uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;surface.rectTransform.GetWorldCorners(corners);
                var a=RectTransformUtility.WorldToScreenPoint(uiCamera,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(uiCamera,corners[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
            }
        }
        public Vector2 ProjectToScreen(Vector3 world){var v=mapCamera.WorldToViewportPoint(world);var rect=DisplayRect;return rect.position+Vector2.Scale(new Vector2(v.x,v.y),rect.size);}

        public Camera MapCamera => mapCamera;

        public void Configure(PhysicalMapController controller, Camera playerView, Camera paperView)
        {
            map = controller;
            playerCamera = playerView;
            mapCamera = paperView;
        }

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += OnCameraRendered;
            if (map == null || mapCamera == null) return;
            map.MapOpened += Refresh;
            map.MapClosed += Refresh;
            Refresh();
        }

        private void LateUpdate()
        {
            if (playerCamera == null || mapCamera == null) return;
            if(surface)
            {
                surface.gameObject.SetActive(map&&map.IsOpen);
                if(!map||!map.IsOpen)return;
                int width=Mathf.Clamp(Mathf.CeilToInt(DisplayRect.width),256,2048),heightPixels=Mathf.Max(1,Mathf.RoundToInt(width*map.Settings.height/map.Settings.width));
                if(!target||target.width!=width||target.height!=heightPixels)
                {
                    ReleaseTarget();target=new RenderTexture(width,heightPixels,24,RenderTextureFormat.ARGB32){name="Held map isolated view",antiAliasing=1};target.Create();
                    var previous=RenderTexture.active;try{RenderTexture.active=target;GL.Clear(true,true,Color.clear);}finally{RenderTexture.active=previous;}
                    surface.texture=target;mapCamera.targetTexture=target;
                }
                mapCamera.enabled=true;
                mapCamera.orthographic=true;mapCamera.orthographicSize=map.Settings.height*.5f;mapCamera.aspect=map.Settings.width/map.Settings.height;mapCamera.transform.localPosition=new Vector3(map.Settings.localHoldPosition.x,map.Settings.localHoldPosition.y,0);mapCamera.transform.localRotation=Quaternion.identity;return;
            }
            mapCamera.aspect = playerCamera.aspect;
            if(map==null||map.Settings==null)return;
            // Fit the paper into the left side at every aspect ratio; player FOV should not shrink it.
            var settings=map.Settings;
            float height=Mathf.Max(settings.height/.76f,settings.width/(.65f*mapCamera.aspect));
            float distance=Mathf.Max(.1f,settings.localHoldPosition.z);
            mapCamera.fieldOfView=2*Mathf.Atan(height/(2*distance))*Mathf.Rad2Deg;
            mapCamera.transform.localPosition=new Vector3(settings.localHoldPosition.x+height*mapCamera.aspect*.15f,settings.localHoldPosition.y,0);
            mapCamera.transform.localRotation=Quaternion.identity;
        }

        private void Refresh()
        {
            if (mapCamera != null) mapCamera.enabled = map != null && map.IsOpen && (!surface||target);
            if(surface){surface.enabled=false;surface.gameObject.SetActive(map&&map.IsOpen);if(map&&map.IsOpen)Canvas.ForceUpdateCanvases();}
        }

        private void OnCameraRendered(ScriptableRenderContext context, Camera renderedCamera)
        {
            if(surface&&renderedCamera==mapCamera&&target&&map&&map.IsOpen)surface.enabled=true;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnCameraRendered;
            if (map != null)
            {
                map.MapOpened -= Refresh;
                map.MapClosed -= Refresh;
            }
            if (mapCamera != null) mapCamera.enabled = false;
            if(surface)surface.gameObject.SetActive(false);
        }
        private void ReleaseTarget(){if(!target)return;if(mapCamera)mapCamera.targetTexture=null;if(surface){surface.texture=null;surface.enabled=false;}target.Release();Destroy(target);target=null;}
        private void OnDestroy()=>ReleaseTarget();
    }
}
