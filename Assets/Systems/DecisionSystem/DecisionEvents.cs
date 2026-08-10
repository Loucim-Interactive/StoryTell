
namespace Systems.DecisionSystem {
    public static class DecisionEvents
    {
        public const string Requested      = "Decision.Requested";      // payload: RadioDecisionAsset
        public const string Pending        = "Decision.Pending";        // payload: DecisionPendingPayload
        public const string ChoicesReady   = "Decision.ChoicesReady";   // payload: DecisionChoicesReadyPayload
        public const string ChoicesClosed  = "Decision.ChoicesClosed";  // no payload
        public const string ChoiceSelected = "Decision.ChoiceSelected"; // payload: int
        public const string Resolved       = "Decision.Resolved";       // payload: DecisionResolvedPayload
        public const string Ignored        = "Decision.Ignored";        // payload: RadioDecisionAsset

    }
}
