using UnityEngine;
namespace Systems.BattleSystem
{
    public sealed class ShadowDamageTarget : MonoBehaviour
    {
        private bool hit;
        public event System.Action Defeated;
        public void ReceiveHit(){if(hit)return;hit=true;Defeated?.Invoke();Destroy(gameObject);}
    }
}
