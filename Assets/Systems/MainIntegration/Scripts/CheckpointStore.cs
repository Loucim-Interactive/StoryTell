using System;
using System.IO;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    [Serializable] public sealed class SpeakerCheckpoint
    {
        public string id;
        public int index;
        public bool spoken;
    }
    [Serializable] public sealed class CheckpointData
    {
        public int version = 1;
        public string scene = "Level1";
        public RouteStage stage;
        public bool equipmentStarted, letterStarted, letterCompleted, mapUnlocked;
        public int equipmentMask, letterMask, puzzleMask;
        public SpeakerCheckpoint[] speakers = Array.Empty<SpeakerCheckpoint>();
        public string[] triggeredZones = Array.Empty<string>();
        public string[] narratedZones = Array.Empty<string>();
        public bool IsValid => version == 1 && scene == "Level1" && stage >= RouteStage.Camp && stage < RouteStage.Battle &&
            equipmentMask >= 0 && equipmentMask <= 7 && letterMask >= 0 && letterMask <= 15 && puzzleMask >= 0 && puzzleMask <= 15 &&
            (equipmentStarted || equipmentMask == 0) && (letterStarted || letterMask == 0) &&
            (!letterCompleted || letterMask == 15) && (puzzleMask == 0 || letterMask == 15) &&
            (stage == RouteStage.Camp || equipmentMask == 7 && (!letterStarted || letterCompleted)) &&
            speakers != null && speakers.Length <= 64 && triggeredZones != null && triggeredZones.Length <= 256 && narratedZones != null && narratedZones.Length <= 256;
    }

    // A completed write replaces the previous save. Its backup remains usable after a damaged write.
    public sealed class CheckpointStore
    {
        private readonly string path;
        public CheckpointStore(string path) { this.path = path; }
        public bool TryRead(out CheckpointData data)
        {
            if (Read(path, out data)) return true;
            return Read(path + ".bak", out data);
        }
        private static bool Read(string file, out CheckpointData data)
        {
            data = null;
            try
            {
                if (!File.Exists(file) || new FileInfo(file).Length > 128 * 1024) return false;
                data = JsonUtility.FromJson<CheckpointData>(File.ReadAllText(file));
                return data != null && data.IsValid;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { return false; }
        }
        public bool TryWrite(CheckpointData data)
        {
            if (data == null || !data.IsValid) return false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { return false; }
        }
    }
}
