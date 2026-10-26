using UnityEngine;

namespace StoryTell.GameplayPath
{
    public sealed class LetterAssemblyState
    {
        private int mask;
        public int PlacedCount { get; private set; }
        public bool Complete => mask == 15;
        public bool IsPlaced(int index) => index >= 0 && index < 4 && (mask & (1 << index)) != 0;
        public bool TryPlace(int index, float distance, float angle)
        {
            if (index < 0 || index >= 4 || IsPlaced(index) || distance > 34 || distance < 0 || Mathf.Abs(Mathf.DeltaAngle(angle, 0)) > 14) return false;
            mask |= 1 << index; PlacedCount++; return true;
        }
    }
}
