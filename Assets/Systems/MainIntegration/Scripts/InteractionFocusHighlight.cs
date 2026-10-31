using System.Collections.Generic;
using DialogueSystem.Scripts;
using Systems.InteractionSystem.Scripts;
using UnityEngine;
using UnityEngine.Rendering;

namespace StoryTell.MainIntegration
{
    public sealed class InteractionFocusHighlight:MonoBehaviour
    {
        [SerializeField] private InteractionScript interaction;
        [SerializeField] private UiSessionCoordinator session;
        [SerializeField] private Transform origin;
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private float distance=3.5f;
        private readonly List<GameObject> overlays=new();
        public InteractableScript HighlightedTarget{get;private set;}
        public int OverlayCount=>overlays.Count;
        private void LateUpdate()
        {
            var target=interaction&&interaction.isActiveAndEnabled&&(!session||session.Mode==UiMode.None)?interaction.CurrentTarget:null;
            if(target&&(!target.isActiveAndEnabled||target.GetComponentInParent<SpeakerScript>()||Vector3.Distance(origin.position,target.transform.position)>distance))target=null;
            SetTarget(target);
        }
        private void SetTarget(InteractableScript target)
        {
            if(target==HighlightedTarget&&(!target||target.isActiveAndEnabled))return;
            Clear();HighlightedTarget=target;if(!target||!highlightMaterial)return;
            foreach(var source in target.GetComponentsInChildren<MeshRenderer>(false))
            {
                var filter=source.GetComponent<MeshFilter>();if(!source.enabled||!filter||!filter.sharedMesh)continue;
                var go=new GameObject("Interaction focus",typeof(MeshFilter),typeof(MeshRenderer));go.layer=2;go.transform.SetParent(source.transform,false);go.transform.localScale=Vector3.one*1.002f;
                go.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var renderer=go.GetComponent<MeshRenderer>();var materials=new Material[filter.sharedMesh.subMeshCount];for(int i=0;i<materials.Length;i++)materials[i]=highlightMaterial;renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;overlays.Add(go);
            }
        }
        private void Clear(){foreach(var go in overlays)if(go)Destroy(go);overlays.Clear();HighlightedTarget=null;}
        private void OnDisable()=>Clear();
    }
}
