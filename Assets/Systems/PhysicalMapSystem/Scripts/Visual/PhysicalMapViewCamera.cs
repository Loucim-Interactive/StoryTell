using UnityEngine;

namespace StoryTell.PhysicalMap
{
    /// <summary>Keeps the held paper legible in first person, including near the player mesh and trench walls.</summary>
    [DisallowMultipleComponent]
    public sealed class PhysicalMapViewCamera : MonoBehaviour
    {
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Camera mapCamera;

        public Camera MapCamera => mapCamera;

        public void Configure(PhysicalMapController controller, Camera playerView, Camera paperView)
        {
            map = controller;
            playerCamera = playerView;
            mapCamera = paperView;
        }

        private void OnEnable()
        {
            if (map == null || mapCamera == null) return;
            map.MapOpened += Refresh;
            map.MapClosed += Refresh;
            Refresh();
        }

        private void LateUpdate()
        {
            if (playerCamera == null || mapCamera == null) return;
            mapCamera.fieldOfView = playerCamera.fieldOfView;
            mapCamera.aspect = playerCamera.aspect;
        }

        private void Refresh()
        {
            if (mapCamera != null) mapCamera.enabled = map != null && map.IsOpen;
        }

        private void OnDisable()
        {
            if (map != null)
            {
                map.MapOpened -= Refresh;
                map.MapClosed -= Refresh;
            }
            if (mapCamera != null) mapCamera.enabled = false;
        }
    }
}
