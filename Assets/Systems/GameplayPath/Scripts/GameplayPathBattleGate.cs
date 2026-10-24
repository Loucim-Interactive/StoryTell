using UnityEngine;
namespace StoryTell.GameplayPath
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class GameplayPathBattleGate : MonoBehaviour
    {
        [SerializeField] private GameplayPathController controller;
        private void OnTriggerEnter(Collider other) { Check(other); }
        private void OnTriggerStay(Collider other) { Check(other); }
        private void Check(Collider other)
        {
            if (controller != null && controller.Player != null && other.transform.IsChildOf(controller.Player))
                controller.TryEnterBattle();
        }
    }
}
