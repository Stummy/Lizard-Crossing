using UnityEngine;
using UnityEditor;

namespace LizardCrossing.EditorTools
{
    /// <summary>
    /// Editor-only playtest driver. Menu items run on the main thread even during
    /// play mode, so an automated harness (MCP execute_menu_item) can start a run
    /// and steer the lizard by poking the <see cref="InputProvider"/> test seams.
    /// Pure measurement harness — not part of the shipping game.
    /// </summary>
    internal static class BotPlaytestMenu
    {
        private const string Root = "Lizard Crossing/Bot/";

        [MenuItem(Root + "Start Run")]
        private static void StartRun() => InputProvider.StartOverride = true;

        [MenuItem(Root + "Move Forward")]
        private static void Forward() => InputProvider.MoveOverride = new Vector2(0f, 1f);

        [MenuItem(Root + "Move Fwd+Right")]
        private static void ForwardRight() => InputProvider.MoveOverride = new Vector2(0.7f, 0.7f);

        [MenuItem(Root + "Move Fwd+Left")]
        private static void ForwardLeft() => InputProvider.MoveOverride = new Vector2(-0.7f, 0.7f);

        [MenuItem(Root + "Move Right")]
        private static void Right() => InputProvider.MoveOverride = new Vector2(1f, 0f);

        [MenuItem(Root + "Move Left")]
        private static void Left() => InputProvider.MoveOverride = new Vector2(-1f, 0f);

        [MenuItem(Root + "Stop Moving")]
        private static void StopMoving() => InputProvider.MoveOverride = Vector2.zero;

        [MenuItem(Root + "Release Override")]
        private static void Release() => InputProvider.MoveOverride = null;

        [MenuItem(Root + "Dash")]
        private static void Dash() => InputProvider.PressDash();

        [MenuItem(Root + "Toggle POV")]
        private static void TogglePov()
        {
            if (LizardCameraController.Instance != null) LizardCameraController.Instance.ToggleView();
        }

        [MenuItem(Root + "Force Foot Bump")]
        private static void ForceFootBump()
        {
            var gm = GameStateManager.Instance;
            var p = PlayerController.Instance;
            if (gm != null && p != null)
                gm.FootBump(p.transform.position + p.transform.forward * 0.3f);
        }

        // Autonomous playtest: plays N full runs with a dodge AI and writes a telemetry
        // report to Temp/Playtest/report.txt (outcomes, distance, frame time, errors).
        // Must be in Play mode first.
        [MenuItem(Root + "Auto-Playtest (8 runs)")]
        private static void AutoPlaytest8()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[AutoPlaytest] Enter Play mode first."); return; }
            LizardCrossing.Testing.AutoPlaytest.Launch(8);
        }

        // Records a real MP4 of a bot run to Temp/Recording/run.mp4 (HUD included) for
        // uploading to a video model (e.g. Gemini) to critique motion/feel. Play mode first.
        [MenuItem(Root + "Record MP4 (10s)")]
        private static void RecordMp4()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[RunRecorder] Enter Play mode first."); return; }
            LizardCrossing.Testing.RunRecorder.LaunchVideo(10f, 30);
        }
    }
}
