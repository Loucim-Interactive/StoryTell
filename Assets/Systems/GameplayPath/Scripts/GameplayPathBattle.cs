using System;
using UnityEngine;
namespace StoryTell.GameplayPath
{
    public sealed class GameplayPathBattle : MonoBehaviour
    {
        [SerializeField] private GameplayPathController controller;
        [SerializeField] private GameplayPathDefinition definition;
        [SerializeField] private GameplayPathEnemy[] pool;
        [SerializeField] private BattleRoute[] routes;
        [SerializeField] private ParticleSystem[] explosions;
        private bool active;
        private float nextSpawn;
        private int alive, spawned, defeated;
        public int Spawned => spawned;
        public int Defeated => defeated;
        public int Alive => alive;
        public int Total => definition.totalEnemies;
        public bool Active => active;
        public void Begin()
        {
            if (active || spawned != 0) return;
            active = true; nextSpawn = Time.time + 2;
            foreach (var ps in explosions) if (ps != null) ps.Play(true);
        }
        private void Update()
        {
            if (!active || Time.time < nextSpawn || spawned >= Total || alive >= definition.simultaneousEnemies) return;
            foreach (var enemy in pool)
            {
                if (enemy.gameObject.activeSelf) continue;
                var route = routes[spawned % routes.Length];
                if (!enemy.Activate(this, controller, definition, route.points)) { nextSpawn = Time.time + 1; return; }
                spawned++; alive++; nextSpawn = Time.time + definition.spawnInterval; return;
            }
        }
        public void EnemyDefeated()
        {
            if (!active) return;
            alive = Mathf.Max(0, alive - 1); defeated++;
            if (defeated == Total && spawned == Total) controller.FinishBattle(false);
        }
        public void StopBattle()
        {
            active = false;
            foreach (var enemy in pool) if (enemy != null) enemy.gameObject.SetActive(false);
            foreach (var ps in explosions) if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
