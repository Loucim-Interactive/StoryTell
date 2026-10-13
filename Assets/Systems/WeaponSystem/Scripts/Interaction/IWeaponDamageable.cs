using UnityEngine;

namespace StoryTell.Weapons
{
    public interface IWeaponDamageable
    {
        bool ReceiveWeaponDamage(float damage, Vector3 hitPoint, Vector3 shotDirection, GameObject source);
    }
}
