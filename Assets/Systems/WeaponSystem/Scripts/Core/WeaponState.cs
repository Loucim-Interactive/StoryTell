namespace StoryTell.Weapons
{
    public enum WeaponState
    {
        Holstered,
        Ready,
        Aiming,
        Firing,
        Reloading,
        Jammed,
        ClearingJam,
        Disabled
    }

    public enum WeaponFireResult
    {
        Fired,
        Empty,
        Jammed,
        Cooldown,
        NotReady
    }
}
