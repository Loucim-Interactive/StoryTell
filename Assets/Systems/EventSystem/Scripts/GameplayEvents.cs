namespace Systems.EventSystem.Scripts {
    public enum GameplayEventType
    {
        Explosion,
        StartInspection,
        MaxZoom,
        EndInspection,
        LookAtPoint
    }

    public static class GameplayEvents
    {
        public const string Explosion = "explosion";
        public const string StartInspection = "inspection.start";
        public const string MaxZoom = "zoom.max";
        public const string EndInspection = "inspection.end";
        public const string LookAtPoint = "lookAtPoint";

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
                default:
                    return string.Empty;
            }
        }
    }
}
