using System;
using UnityEngine;

namespace LizardCrossing
{
    /// <summary>
    /// Static event hub decoupling gameplay systems from UI/camera/audio.
    /// Bootstrap calls Clear() on scene start so reloads never leak stale handlers.
    /// </summary>
    public static class GameEvents
    {
        public static event Action RunStarted;
        /// <summary>Lizard took a stomp but survived. heartsLeft, world position of the hazard.</summary>
        public static event Action<int, Vector3> PlayerHit;
        public static event Action<DeathCause> PlayerDied;
        /// <summary>Player watched a rewarded ad and came back to life mid-run.</summary>
        public static event Action PlayerRevived;
        public static event Action<RunResults> RunWon;

        /// <summary>pos = world position of impact, severity 0..1 (proximity to player).</summary>
        public static event Action<Vector3, float> HazardImpact;
        /// <summary>A slam landed close but did not hit.</summary>
        public static event Action<Vector3> NearMiss;
        public static event Action<int, int> BugCollected; // collected, total

        public static void RaiseRunStarted() { RunStarted?.Invoke(); }
        public static void RaisePlayerHit(int heartsLeft, Vector3 hazardPos) { PlayerHit?.Invoke(heartsLeft, hazardPos); }
        public static void RaisePlayerDied(DeathCause cause) { PlayerDied?.Invoke(cause); }
        public static void RaisePlayerRevived() { PlayerRevived?.Invoke(); }
        public static void RaiseRunWon(RunResults r) { RunWon?.Invoke(r); }
        public static void RaiseHazardImpact(Vector3 pos, float severity) { HazardImpact?.Invoke(pos, severity); }
        public static void RaiseNearMiss(Vector3 pos) { NearMiss?.Invoke(pos); }
        public static void RaiseBugCollected(int collected, int total) { BugCollected?.Invoke(collected, total); }

        public static void Clear()
        {
            RunStarted = null;
            PlayerHit = null;
            PlayerDied = null;
            PlayerRevived = null;
            RunWon = null;
            HazardImpact = null;
            NearMiss = null;
            BugCollected = null;
        }
    }

    public enum DeathCause { Stomped, Unknown }

    public struct RunResults
    {
        public int Stars;
        public int BugsCollected;
        public int BugsTotal;
        public float Time;
        public float ParTime;
        public int CloseCalls;
        public int HeartsLeft;
    }
}
