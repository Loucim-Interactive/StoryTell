using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;
using StoryTell.MainIntegration;

namespace Systems.BattleSystem {
    [DisallowMultipleComponent]
    public sealed class ShadowSpawner : MonoBehaviour {
        [SerializeField] private SpawnArea spawnArea;
        [SerializeField] private Material blackUnlitMaterial;
        [SerializeField, Min(.01f)] private float capsuleRadius = .4f;
        [SerializeField, Min(.02f)] private float capsuleHeight = 1.8f;
        [SerializeField] private Vector2 lifetimeRange = new Vector2(3, 7);
        private readonly List<GameObject> shadows = new();
        [SerializeField]private Transform combatTarget;
        [SerializeField]private CombatConclusion conclusion;
        [SerializeField,Range(1,8)]private int maximumActive=7;
        private int remaining,alive;
        public int Remaining=>remaining+alive;
        private float nextWave;
        private Vector3 entryPosition;
        public bool IsConfigured => spawnArea && blackUnlitMaterial;

        public int Spawn(int count) {
            if (!IsConfigured) return 0;
            if(combatTarget&&conclusion){entryPosition=combatTarget.position;remaining=Mathf.Max(0,count);alive=0;FillWave();return alive;}
            shadows.RemoveAll(shadow => !shadow);
            float radius = Mathf.Max(.01f, capsuleRadius);
            float height = Mathf.Max(radius * 2, capsuleHeight);
            float minimumLifetime = Mathf.Max(.01f, Mathf.Min(lifetimeRange.x, lifetimeRange.y));
            float maximumLifetime = Mathf.Max(minimumLifetime, Mathf.Max(lifetimeRange.x, lifetimeRange.y));
            int spawned = 0;
            for (int i = 0; i < count; i++) {
                if (!spawnArea.TryGetPosition(out var position)) continue;
                var shadow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                shadow.name = "Battle Shadow";
                shadow.AddComponent<ShadowDamageTarget>();
                shadow.transform.position = position + Vector3.up * (height * .5f);
                shadow.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2);
                var renderer = shadow.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = blackUnlitMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                shadows.Add(shadow);
                Destroy(shadow, Random.Range(minimumLifetime, maximumLifetime));
                spawned++;
            }
            if (spawned < count) Debug.LogWarning($"Spawned {spawned}/{count} battle shadows. Check the spawn bounds and baked NavMesh.", this);
            return spawned;
        }

        private void Update(){if(combatTarget&&conclusion&&!conclusion.Ending&&remaining>0&&Time.time>=nextWave){FillWave();nextWave=Time.time+1;}}
        private void FillWave()
        {
            int attempts=0,noPoint=0,noNav=0,noPath=0,close=0;
            while(remaining>0&&alive<maximumActive&&attempts++<32)
            {
                if(!spawnArea.TryGetPosition(out var point)){noPoint++;continue;}if(Vector3.Distance(point,combatTarget.position)<10){close++;continue;}if(!NavMesh.SamplePosition(point,out var nav,3,NavMesh.AllAreas)){noNav++;continue;}
                var path=new NavMeshPath();if(!NavMesh.SamplePosition(entryPosition,out var entry,2,NavMesh.AllAreas)||!NavMesh.CalculatePath(nav.position,entry.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){noPath++;continue;}
                var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="Sombra";go.transform.position=nav.position;
                // Keep the agent root at the soles; primitive geometry is centered one metre up.
                var visual=GameObject.CreatePrimitive(PrimitiveType.Capsule);visual.name="Silueta";visual.transform.SetParent(go.transform,false);visual.transform.localPosition=Vector3.up*.9f;visual.transform.localScale=new Vector3(.7f,.9f,.7f);Destroy(visual.GetComponent<Collider>());
                go.GetComponent<MeshRenderer>().enabled=false;var col=go.GetComponent<CapsuleCollider>();col.center=Vector3.up*.9f;col.height=1.8f;col.radius=.35f;col.isTrigger=false;
                visual.GetComponent<Renderer>().sharedMaterial=blackUnlitMaterial;visual.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                var agent=go.AddComponent<NavMeshAgent>();agent.radius=.35f;agent.height=1.8f;agent.speed=1.25f;agent.acceleration=4;agent.angularSpeed=180;agent.stoppingDistance=.1f;
                go.AddComponent<AdvancingShadow>().Configure(combatTarget,conclusion);go.AddComponent<ShadowDamageTarget>().Defeated+=OnDefeated;
                shadows.Add(go);remaining--;alive++;
            }
            if(alive==0&&remaining>0&&Application.isBatchMode)Debug.Log($"Shadow spawn diagnostic: target={combatTarget.position} area={spawnArea.transform.position} point={noPoint} nav={noNav} path={noPath} close={close}");
        }
        private void OnDefeated(){alive--;if(remaining==0&&alive==0)conclusion.Finish(false);else nextWave=Time.time+.6f;}

        private void OnDestroy() {
            foreach (var shadow in shadows) if (shadow) Destroy(shadow);
            shadows.Clear();
        }
    }
}
