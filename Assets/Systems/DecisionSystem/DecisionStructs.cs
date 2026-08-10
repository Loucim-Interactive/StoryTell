namespace Systems.DecisionSystem
{
    public readonly struct DecisionPendingPayload
    {
        public readonly WalkieDecisionAsset Decision;
        public readonly float DurationSeconds; // <=0 means no countdown / can't be ignored
        public DecisionPendingPayload(WalkieDecisionAsset decision, float durationSeconds)
        {
            Decision = decision;
            DurationSeconds = durationSeconds;
        }
    }

    public readonly struct DecisionChoicesReadyPayload
    {
        public readonly WalkieDecisionAsset Decision;
        public readonly float RemainingSeconds; // <=0 means no countdown
        public DecisionChoicesReadyPayload(WalkieDecisionAsset decision, float remainingSeconds)
        {
            Decision = decision;
            RemainingSeconds = remainingSeconds;
        }
    }

    public readonly struct DecisionResolvedPayload
    {
        public readonly WalkieDecisionAsset Decision;
        public readonly RadioDecisionChoice Choice;
        public readonly int ChoiceIndex;
        public DecisionResolvedPayload(WalkieDecisionAsset decision, RadioDecisionChoice choice, int choiceIndex)
        {
            Decision = decision;
            Choice = choice;
            ChoiceIndex = choiceIndex;
        }
    }
}