using System;
using UnityEngine;

namespace LizardCrossing
{
    public enum AdPlacement { Revive, DoubleRewards, BonusChest }

    /// <summary>
    /// Monetization seam (packet: ethical rewarded ads — revive, double rewards,
    /// bonus chest; never forced, never pay-to-win). Phase 2 ships a stub that
    /// "plays" instantly and always rewards; a real SDK (Unity LevelPlay / AdMob)
    /// drops in behind this interface in the launch phase with zero call-site
    /// changes. All rewarded ads are strictly opt-in (the player taps a button).
    /// </summary>
    public interface IAdService
    {
        bool IsRewardedReady { get; }
        void ShowRewarded(AdPlacement placement, Action<bool> onComplete);
    }

    public class StubAdService : IAdService
    {
        public bool IsRewardedReady { get { return true; } }

        public void ShowRewarded(AdPlacement placement, Action<bool> onComplete)
        {
            Debug.Log("[Ads] (stub) rewarded ad watched for: " + placement);
            // a real SDK invokes the callback with false if the user skips or no fill
            onComplete?.Invoke(true);
        }
    }
}
