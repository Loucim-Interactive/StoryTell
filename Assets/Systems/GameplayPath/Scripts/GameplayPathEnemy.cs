using StoryTell.Weapons;
using UnityEngine;
using UnityEngine.AI;
namespace StoryTell.GameplayPath
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GameplayPathEnemy : MonoBehaviour, IWeaponDamageable
    {
        private NavMeshAgent agent;
        private GameplayPathBattle battle;
        private GameplayPathController controller;
        private GameplayPathDefinition definition;
        private Transform[] route;
        private int waypoint;
        private float health, nextRepath, lastProgressTime, lastDistance;
        private readonly RaycastHit[] contactHits = new RaycastHit[32];
        public bool IsAlive => health > 0 && gameObject.activeSelf;
        public float Health => health;
        public bool Activate(GameplayPathBattle owner, GameplayPathController path, GameplayPathDefinition config, Transform[] points)
        {
            if (points == null || points.Length == 0 || !NavMesh.SamplePosition(points[0].position, out var hit, 3, NavMesh.AllAreas)) return false;
            battle = owner; controller = path; definition = config; route = points; waypoint = points.Length > 1 ? 1 : points.Length;
            health = config.enemyHealth; transform.position = hit.position; gameObject.SetActive(true);
            agent = GetComponent<NavMeshAgent>();
            if (!agent.Warp(hit.position)) { gameObject.SetActive(false); return false; }
            agent.speed = config.enemySpeed; agent.isStopped = false; agent.ResetPath();
            lastProgressTime = Time.time; lastDistance = float.PositiveInfinity; nextRepath = 0;
            return true;
        }
        private void Update()
        {
            if (!IsAlive || controller == null || !battle.Active) return;
            if (CheckPlayerContact()) return;
            if (agent == null || !agent.isOnNavMesh) return;
            var player = controller.Player;
            if (Time.time < nextRepath) return;
            nextRepath = Time.time + 0.35f;
            if (waypoint < route.Length && Vector3.Distance(transform.position, route[waypoint].position) < 1.6f) { waypoint++; lastProgressTime = Time.time; }
            Vector3 goal = waypoint < route.Length ? route[waypoint].position : player.position;
            if (NavMesh.SamplePosition(goal, out var hit, 5, NavMesh.AllAreas)) agent.SetDestination(hit.position);
            float distance = Vector3.Distance(transform.position, goal);
            if (distance < lastDistance - 0.15f) lastProgressTime = Time.time;
            lastDistance = distance;
            // A blocked intermediate corner may be skipped, but never remove a live enemy to fake a win.
            if (Time.time - lastProgressTime > 10 && waypoint < route.Length) { waypoint++; lastProgressTime = Time.time; lastDistance = float.PositiveInfinity; }
        }
        public bool ReceiveWeaponDamage(float damage, Vector3 point, Vector3 direction, GameObject source)
        {
            if (!IsAlive || damage <= 0) return false;
            health = Mathf.Max(0, health - damage);
            if (health <= 0) { gameObject.SetActive(false); battle.EnemyDefeated(); }
            return true;
        }
        public bool CheckPlayerContact()
        {
            if (!IsAlive || controller == null || battle == null || !battle.Active) return false;
            Transform player = controller.Player;
            if (player == null) return false;
            var body = GetComponent<Collider>();
            var playerBody = player.GetComponent<Collider>();
            Vector3 delta = player.position - transform.position;
            bool close = Mathf.Abs(delta.y) < 1.9f && new Vector2(delta.x, delta.z).sqrMagnitude <= definition.contactDistance * definition.contactDistance;
            if (!close && body != null && playerBody != null)
            {
                Vector3 playerPoint;
                if(playerBody is CharacterController capsule)
                {
                    Vector3 scale=player.lossyScale;
                    float radius=capsule.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
                    float half=Mathf.Max(0,capsule.height*Mathf.Abs(scale.y)*0.5f-radius);
                    Vector3 center=player.TransformPoint(capsule.center);
                    Vector3 axisPoint=center+player.up*Mathf.Clamp(Vector3.Dot(body.bounds.center-center,player.up),-half,half);
                    Vector3 offset=body.bounds.center-axisPoint;
                    playerPoint=offset.sqrMagnitude<=radius*radius?body.bounds.center:axisPoint+offset.normalized*radius;
                }
                else playerPoint=playerBody.ClosestPoint(body.bounds.center);
                close = (body.ClosestPoint(playerPoint) - playerPoint).sqrMagnitude <= 0.08f * 0.08f;
            }
            if (!close) return false;
            Vector3 from = body != null ? body.bounds.center : transform.position + Vector3.up;
            Vector3 to = playerBody != null ? playerBody.bounds.center : player.position + Vector3.up;
            Vector3 direction = to - from;
            int hits = Physics.RaycastNonAlloc(from, direction.normalized, contactHits, direction.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
            {
                Transform hit = contactHits[i].collider.transform;
                if (hit.IsChildOf(transform) || hit.IsChildOf(player)) continue;
                if (hit.GetComponentInParent<GameplayPathEnemy>() != null) continue;
                return false;
            }
            return controller.FinishBattle(true);
        }
    }
}
