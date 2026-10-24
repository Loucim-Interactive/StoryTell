using StoryTell.Weapons;
using Systems.InteractionSystem.Scripts;
using UnityEngine;
namespace StoryTell.GameplayPath
{
    public sealed class GameplayPathSupply : InteractableScript
    {
        [SerializeField] private GameplayPathController controller;
        [SerializeField] private Level1WeaponTrial trial;
        [SerializeField, Min(1)] private int magazines = 12;
        public int Magazines => magazines;
        protected override void OnInteract()
        {
            if (controller.Progress.Stage != PathStage.Combat) { controller.Notice("Reserva del puesto avanzado para el ataque."); return; }
            if (magazines <= 0) { controller.Notice("Esta caja ya no tiene cargadores."); return; }
            int added = trial.CollectMagazineCount(magazines); magazines -= added;
            controller.Notice(added > 0 ? $"+{added} cargadores. Quedan {magazines} en esta caja." : "Ya llevás todos los cargadores que podés transportar.");
        }
    }
}
