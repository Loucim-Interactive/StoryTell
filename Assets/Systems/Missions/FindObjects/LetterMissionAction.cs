using UnityEngine;

namespace Systems.Missions.FindObjects {
    // Dialogue assets reference this asset instead of holding invalid scene-object references.
    [CreateAssetMenu(menuName = "Missions/Letter Mission Action")]
    public class LetterMissionAction : ScriptableObject {
        public void AcceptMission() {
            LetterManager mission = FindFirstObjectByType<LetterManager>();
            if (mission) mission.StartMission();
        }

        public void ReturnLetter() {
            LetterManager mission = FindFirstObjectByType<LetterManager>();
            if (mission) mission.CompleteMission();
        }
    }
}
