namespace Systems.EventSystem.Scripts {
    public enum GameplayEventType
    {
        Explosion,
        StartInspection,
        EndInspection,
        DefaultZoom,
        MaxZoom,
        StartLookAtPoint,
        EndLookAtPoint,
        StateThought,
        InteractableAction,
        VFX,
    }

    public static class GameplayEvents
    {
        public const string InteractAction = "interact.action";
        public const string Explosion = "explosion";
        public const string StartInspection = "inspection.start";
        public const string DefaultZoom = "zoom.default";
        public const string MaxZoom = "zoom.max";
        public const string EndInspection = "inspection.end";
        public const string StartLookAtPoint = "lookAtPoint.start";
        public const string EndLookAtPoint = "lookAtPoint.end";
        public const string StateThought = "stateThought";
        public const string VFX = "vfx";

        public static string GetName(GameplayEventType eventType) {
            switch (eventType) {
                case GameplayEventType.Explosion:
                    return Explosion;
                case GameplayEventType.StartInspection:
                    return StartInspection;
                case GameplayEventType.DefaultZoom:
                    return DefaultZoom;
                case GameplayEventType.MaxZoom:
                    return MaxZoom;
                case GameplayEventType.EndInspection:
                    return EndInspection;
                case GameplayEventType.StartLookAtPoint:
                    return StartLookAtPoint;
                case GameplayEventType.EndLookAtPoint:
                    return EndLookAtPoint;
                case GameplayEventType.StateThought:
                    return StateThought;
                case GameplayEventType.InteractableAction:
                    return InteractAction;
                case GameplayEventType.VFX:
                    return VFX;
                default:
                    return string.Empty;
            }
        }
    }
}
