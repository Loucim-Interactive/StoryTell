namespace Systems.EventSystem.Scripts {
    public enum GameplayEventType
    {
        Explosion,
        StartInspection,
        EndInspection,
        MaxZoom,
        LookAtPoint,
        StateThought,
    }

    public static class GameplayEvents
    {
        public const string Explosion = "explosion";
        public const string StartInspection = "inspection.start";
        public const string MaxZoom = "zoom.max";
        public const string EndInspection = "inspection.end";
        public const string LookAtPoint = "lookAtPoint";
        public const string StateThought = "stateThought";

        public static string GetName(GameplayEventType eventType) {
            switch (eventType) {
                case GameplayEventType.Explosion:
                    return Explosion;
                case GameplayEventType.StartInspection:
                    return StartInspection;
                case GameplayEventType.MaxZoom:
                    return MaxZoom;
                case GameplayEventType.EndInspection:
                    return EndInspection;
                case GameplayEventType.LookAtPoint:
                    return LookAtPoint;
                case GameplayEventType.StateThought:
                    return StateThought;
                default:
                    return string.Empty;
            }
        }
    }
}
