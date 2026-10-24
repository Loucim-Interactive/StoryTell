using System;
using DialogueSystem.Scripts;
using UnityEngine;
namespace StoryTell.GameplayPath
{
    [CreateAssetMenu(menuName = "StoryTell/Gameplay Path/Scene Definition")]
    public sealed class GameplayPathDefinition : ScriptableObject
    {
        public ConversationSO introduction, equipmentReady, kitchen, letterRequest, letterReturn, sergeant, delivery, battle;
        [TextArea(5, 14)] public string motherLetter;
        [Header("Battle balance")]
        [Min(1)] public int totalEnemies = 45;
        [Range(5, 8)] public int simultaneousEnemies = 6;
        [Min(0.1f)] public float enemySpeed = 1.6f;
        [Min(1)] public float enemyHealth = 70;
        [Min(0)] public float spawnInterval = 1.4f;
        [Range(0.4f, 2)] public float contactDistance = 0.85f;
    }
    [Serializable] public sealed class BattleRoute { public string name; public Transform[] points; }
}
