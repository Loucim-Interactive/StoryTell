using NUnit.Framework;
using StoryTell.Weapons;
using UnityEngine;

public sealed class WeaponSystemTests
{
    private readonly System.Collections.Generic.List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }
        createdObjects.Clear();
    }

    [Test]
    public void MagazineInventory_PreservesIndividualMagazineRoundsAndOrder()
    {
        MagazineInventory inventory = CreateComponent<MagazineInventory>("Inventory");
        inventory.Configure(20, 3);

        Assert.That(inventory.AddMagazine(7), Is.True);
        Assert.That(inventory.AddMagazine(20), Is.True);
        Assert.That(inventory.MagazineCount, Is.EqualTo(2));
        Assert.That(inventory.TotalReserveRounds, Is.EqualTo(27));

        Assert.That(inventory.TryTakeNextMagazine(out int rounds), Is.True);
        Assert.That(rounds, Is.EqualTo(7));
        Assert.That(inventory.GetMagazineRounds(0), Is.EqualTo(20));
    }

    [Test]
    public void Reload_DiscardsRoundsLeftInCurrentMagazine()
    {
        WeaponController weapon = CreateWeapon(10, 2);
        weapon.BeginSequence(6, 1);

        Assert.That(weapon.TryReload(), Is.True);
        Assert.That(weapon.State, Is.EqualTo(WeaponState.Reloading));
        Assert.That(weapon.RoundsInMagazine, Is.Zero);
        Assert.That(weapon.MagazineInventory.MagazineCount, Is.Zero);

        weapon.CompleteReload();
        Assert.That(weapon.RoundsInMagazine, Is.EqualTo(10));
        Assert.That(weapon.MagazineInventory.TotalReserveRounds, Is.Zero);
    }

    [Test]
    public void Condition_JamsAtConfiguredIntervalAndRespondsToMud()
    {
        WeaponCondition condition = CreateComponent<WeaponCondition>("Condition");
        condition.ConfigureExactIntervalsForTests(3, 2, 2, 1);

        Assert.That(condition.EvaluateShotForJam(), Is.False);
        Assert.That(condition.EvaluateShotForJam(), Is.False);
        Assert.That(condition.EvaluateShotForJam(), Is.True);

        condition.ApplyContamination(WeaponContamination.Mud);
        Assert.That(condition.EvaluateShotForJam(), Is.True);
    }

    [Test]
    public void ForcedJam_DoesNotConsumeRoundAndCanBeCleared()
    {
        WeaponController weapon = CreateWeapon(10, 1);
        weapon.BeginSequence(5, 0);
        weapon.ForceNextJam();

        Assert.That(weapon.TryFire(), Is.EqualTo(WeaponFireResult.Jammed));
        Assert.That(weapon.RoundsInMagazine, Is.EqualTo(5));
        Assert.That(weapon.TryReload(), Is.True);
        Assert.That(weapon.State, Is.EqualTo(WeaponState.ClearingJam));

        weapon.CompleteClearJam();
        Assert.That(weapon.State, Is.EqualTo(WeaponState.Ready));
    }

    [Test]
    public void DamageReceiver_OnlyTakesDamageWhenEnabled()
    {
        WeaponDamageReceiver receiver = CreateComponent<WeaponDamageReceiver>("Target");
        receiver.ConfigureForTests(100f, false);

        Assert.That(receiver.ReceiveWeaponDamage(30f, Vector3.zero, Vector3.forward, null), Is.False);
        Assert.That(receiver.CurrentHealth, Is.EqualTo(100f));

        receiver.SetAcceptsWeaponDamage(true);
        Assert.That(receiver.ReceiveWeaponDamage(30f, Vector3.zero, Vector3.forward, null), Is.True);
        Assert.That(receiver.CurrentHealth, Is.EqualTo(70f));
    }

    private WeaponController CreateWeapon(int magazineCapacity, int maxMagazines)
    {
        GameObject gameObject = CreateGameObject("Weapon");
        MagazineInventory inventory = gameObject.AddComponent<MagazineInventory>();
        WeaponCondition condition = gameObject.AddComponent<WeaponCondition>();
        WeaponController controller = gameObject.AddComponent<WeaponController>();
        WeaponDefinition definition = ScriptableObject.CreateInstance<WeaponDefinition>();
        definition.ConfigureForTests(magazineCapacity, maxMagazines);
        createdObjects.Add(definition);
        controller.Configure(definition, inventory, condition);
        return controller;
    }

    private T CreateComponent<T>(string name) where T : Component
    {
        return CreateGameObject(name).AddComponent<T>();
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }
}
