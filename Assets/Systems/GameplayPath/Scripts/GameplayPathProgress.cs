using System;
namespace StoryTell.GameplayPath
{
    public enum PathStage { Arrival, Equipment, ReturnEquipment, Kitchen, LetterRequest, FindLetter, ReturnLetter, Sergeant, TakeCrate, CarryCrate, ApproachBattle, Combat, Complete }
    [Flags] public enum PathEquipment { None = 0, Helmet = 1, Rifle = 2, Ammunition = 4, Map = 8, Radio = 16, All = 31 }
    public enum PathAction { Ruben, Eat, Sergeant, Crate, Officer, BattleEntrance, Helmet, Rifle, Ammunition, Map, Radio, Fragment, Read }
    [Serializable]
    public sealed class GameplayPathProgress
    {
        public PathStage Stage { get; private set; }
        public PathEquipment Equipment { get; private set; }
        public int Fragments { get; private set; }
        public bool LetterAssembled { get; private set; }
        private int fragmentMask;
        public bool Has(PathEquipment item) => (Equipment & item) == item;
        public bool Collect(PathEquipment item)
        {
            if (Stage != PathStage.Equipment || item == PathEquipment.None || (item & ~PathEquipment.All) != 0 || Has(item)) return false;
            Equipment |= item;
            if (Equipment == PathEquipment.All) Stage = PathStage.ReturnEquipment;
            return true;
        }
        public bool CollectFragment(int index)
        {
            if (index < 0 || index >= 4 || Stage > PathStage.ReturnLetter || (fragmentMask & (1 << index)) != 0) return false;
            fragmentMask |= 1 << index; Fragments++;
            if (Stage == PathStage.FindLetter && Fragments == 4) Stage = PathStage.ReturnLetter;
            return true;
        }
        public bool Advance(PathAction action)
        {
            switch (Stage)
            {
                case PathStage.Arrival when action == PathAction.Ruben: Stage = PathStage.Equipment; break;
                case PathStage.ReturnEquipment when action == PathAction.Ruben: Stage = PathStage.Kitchen; break;
                case PathStage.Kitchen when action == PathAction.Eat: Stage = PathStage.LetterRequest; break;
                case PathStage.LetterRequest when action == PathAction.Ruben: Stage = Fragments == 4 ? PathStage.ReturnLetter : PathStage.FindLetter; break;
                case PathStage.ReturnLetter when action == PathAction.Ruben && LetterAssembled: Stage = PathStage.Sergeant; break;
                case PathStage.Sergeant when action == PathAction.Sergeant: Stage = PathStage.TakeCrate; break;
                case PathStage.TakeCrate when action == PathAction.Crate: Stage = PathStage.CarryCrate; break;
                case PathStage.CarryCrate when action == PathAction.Officer: Stage = PathStage.ApproachBattle; break;
                case PathStage.ApproachBattle when action == PathAction.BattleEntrance: Stage = PathStage.Combat; break;
                default: return false;
            }
            return true;
        }
        public bool Finish()
        {
            if (Stage != PathStage.Combat) return false;
            Stage = PathStage.Complete; return true;
        }
        public bool AssembleLetter()
        {
            if (Fragments != 4 || LetterAssembled) return false;
            LetterAssembled = true; return true;
        }
    }
}
