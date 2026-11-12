namespace Arte.Sport.Core.Features;

/// <summary>Receipts, training plans and AvalAI stay off until the branch core is stable.</summary>
public sealed class FeatureOptions
{
    public const string Section = "Features";
    public bool Receipts { get; set; }
    public bool TrainingPlans { get; set; }
    public bool AvalAi { get; set; }
}

/// <summary>
/// Decided before the flag may be turned on: chats are kept 90 days, and a monthly cap of 0 rials
/// means the model is not called even if the flag is flipped by mistake.
/// </summary>
public sealed class AvalAiOptions
{
    public const string Section = "AvalAi";
    public int RetentionDays { get; set; } = 90;
    public long MonthlyCostCapRials { get; set; }
}

public static class AvalAiGate
{
    public static bool CanCall(FeatureOptions features, AvalAiOptions ai) =>
        features.AvalAi && ai.RetentionDays > 0 && ai.MonthlyCostCapRials > 0;
}
