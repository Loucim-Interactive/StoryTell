using UnityEngine;

namespace Systems.Missions.FindObjects {
    [CreateAssetMenu(menuName = "Missions/Equipment Mission Action")]
    public class EquipmentMissionAction : ScriptableObject {
        public void AcceptMission() {
            EquipmentManager mission = FindFirstObjectByType<EquipmentManager>();
            if (mission) mission.StartMission();
        }
    }
}
