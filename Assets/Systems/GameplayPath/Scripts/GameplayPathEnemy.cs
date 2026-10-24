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
            if (!IsAlive || controller == null || !battle.Active || !agent.isOnNavMesh) return;
            var player = controller.Player;
            Vector3 delta = player.position - transform.position;
            if (Mathf.Abs(delta.y) < 1.9f && new Vector2(delta.x, delta.z).sqrMagnitude < definition.contactDistance * definition.contactDistance)
            {
                if (!Physics.Linecast(transform.position + Vector3.up, player.position + Vector3.up, 1 << 0 | 1 << 3, QueryTriggerInteraction.Ignore))
                    controller.FinishBattle(true);
                return;
            }
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
    }
}
