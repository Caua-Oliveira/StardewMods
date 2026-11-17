namespace BeneathYou;

/// <summary>Preferred priority order when choosing a push destination tile.</summary>
public enum PushDirectionPreference
{
    MovementThenStepBack,
    StepBackFirst,
    StepForwardFirst,
    FacingDirectionOnly
}

/// <summary>How the player moves onto the push destination tile.</summary>
public enum PushMovementStyle
{
    SmoothSlide,
    InstantSnap
}

/// <summary>User configuration for Beneath You.</summary>
public sealed class ModConfig
{
    /// <summary>Master toggle for the mod.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Allow placing solid objects, furniture, and trellis crops on the player's tile, pushing the player to an adjacent empty tile.</summary>
    public bool AllowSolidPlacementUnderPlayer { get; set; } = true;

    /// <summary>Priority order for choosing an adjacent empty tile (<c>MovementThenStepBack</c>, <c>StepBackFirst</c>, <c>StepForwardFirst</c>, <c>FacingDirectionOnly</c>).</summary>
    public string PreferredPushDirection { get; set; } = nameof(PushDirectionPreference.MovementThenStepBack);

    /// <summary>Allow pushing the player to a diagonal empty tile if all 4 cardinal tiles are blocked.</summary>
    public bool AllowDiagonalPush { get; set; } = false;

    /// <summary>How the player is displaced (<c>SmoothSlide</c> or <c>InstantSnap</c>).</summary>
    public string PushMovementStyle { get; set; } = nameof(BeneathYou.PushMovementStyle.SmoothSlide);

    /// <summary>Turn the player to face the newly placed item after being pushed onto an adjacent tile.</summary>
    public bool FacePlacedItemAfterPush { get; set; } = true;

    /// <summary>Parse <see cref="PreferredPushDirection"/> with support for legacy/alias string values.</summary>
    public PushDirectionPreference GetPushDirectionPreference()
    {
        return NormalizePushDirection(this.PreferredPushDirection);
    }

    /// <summary>Parse <see cref="PushMovementStyle"/> into a strongly typed enum value.</summary>
    public PushMovementStyle GetPushMovementStyle()
    {
        return NormalizePushMovementStyle(this.PushMovementStyle);
    }

    public static PushDirectionPreference NormalizePushDirection(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return PushDirectionPreference.MovementThenStepBack;

        return raw.Trim() switch
        {
            "AutoStepBack" or "StepBackFirst" => PushDirectionPreference.StepBackFirst,
            "StepForward" or "StepForwardFirst" => PushDirectionPreference.StepForwardFirst,
            "FacingDirectionOnly" => PushDirectionPreference.FacingDirectionOnly,
            _ => PushDirectionPreference.MovementThenStepBack
        };
    }

    public static PushMovementStyle NormalizePushMovementStyle(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return BeneathYou.PushMovementStyle.SmoothSlide;

        return raw.Trim() == nameof(BeneathYou.PushMovementStyle.InstantSnap)
            ? BeneathYou.PushMovementStyle.InstantSnap
            : BeneathYou.PushMovementStyle.SmoothSlide;
    }
}

