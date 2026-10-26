using UnityEngine;
namespace Systems.BattleSystem
{
    // Shooting may dissolve a temporary shadow. Its authored lifetime remains in ShadowSpawner.
    public sealed class ShadowDamageTarget : MonoBehaviour
    {
        public void ReceiveHit(){Destroy(gameObject);}
    }
}
