namespace Systems.InspectionSystem.Scripts {
    public interface IInspectionTarget {
        // Called after inspection cleanup, including cancellation or controller disable.
        void OnInspectionEnded();
    }
}
