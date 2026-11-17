using StardewValley;

namespace BeneathYou;

/// <summary>Per-screen runtime state for split-screen and multiplayer safety.</summary>
internal sealed class ScreenState
{
    public ActivePushSlide? ActivePush { get; set; }
    public bool ActiveBeneathYouSwing { get; set; }
    public Farmer? IgnoreSelfCollisionFarmer { get; set; }
}

