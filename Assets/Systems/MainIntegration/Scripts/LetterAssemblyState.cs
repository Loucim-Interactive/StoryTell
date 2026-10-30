using UnityEngine;

namespace StoryTell.MainIntegration
{
    public sealed class LetterAssemblyState
    {
        private int mask;
        public int PlacedCount { get; private set; }
        public bool Complete => mask == 15;
        public int Mask => mask;
        public void Restore(int value){mask=value&15;PlacedCount=0;for(int i=0;i<4;i++)if(IsPlaced(i))PlacedCount++;}
        public bool IsPlaced(int index) => index >= 0 && index < 4 && (mask & (1 << index)) != 0;
        public bool TryPlace(int index, float distance, float angle)
        {
            if (index < 0 || index >= 4 || IsPlaced(index) || distance > 34 || distance < 0 || Mathf.Abs(Mathf.DeltaAngle(angle, 0)) > 14) return false;
            mask |= 1 << index; PlacedCount++; return true;
        }
    }
}
