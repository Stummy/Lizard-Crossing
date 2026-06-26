using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LizardCrossing.EditorTools
{
    /// <summary>
    /// Forces the editor Game view to a portrait 9:16 ASPECT RATIO so recorded clips + screenshots
    /// match the portrait-mobile concept framing (the camera's FOV adapts to aspect, so a landscape
    /// preview frames the hero differently than the shipping portrait game).
    /// CLAUDE.md / the session-boot protocol require "Game view set to 9:16 portrait" before
    /// recording for the Gemini tester — this makes that one menu click, repeatably.
    ///
    /// FIX (2026-06-26): this used to register a FIXED 1080x1920 resolution, but the editor Game-view
    /// WINDOW is only ~831x619, so the fixed RT exceeded the window and ScreenCapture (used by the
    /// MP4 recorder + screenshots) failed with "requested a region that exceeds the active Render
    /// Target" and produced a 1-frame clip. An ASPECT RATIO (9:16) size scales to fit whatever the
    /// window is, so ScreenCapture always has a valid RT — same portrait framing, no overflow.
    ///
    /// Unity's GameView sizing API is internal, so this goes through reflection (the
    /// SizeSelectionCallback(int,object) + GameViewSizes singleton pattern is stable across
    /// 2019..6000). If anything in the reflection chain is missing it logs a warning and no-ops
    /// rather than throwing, so the recorder can still run at the current aspect as a fallback.
    /// </summary>
    internal static class GameViewPortrait
    {
        // 9:16 portrait as an ASPECT RATIO (W:H ratio, not pixels) so it fits the editor window.
        private const int W = 9, H = 16;
        // New label (was "Lizard 9:16", which prior sessions registered as a broken FIXED 1080x1920);
        // a fresh label guarantees we create + select the aspect-ratio size, not reuse the old fixed one.
        private const string Label = "Lizard 9:16 AR";

        [MenuItem("Lizard Crossing/Bot/Set Game View 9:16")]
        private static void SetPortrait()
        {
            try
            {
                var asm = typeof(Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singletonType.GetProperty("instance").GetValue(null, null);
                var currentGroupType = (int)sizesType.GetProperty("currentGroupType").GetValue(instance, null);
                var group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { currentGroupType });

                int idx = IndexOf(group, Label);
                if (idx < 0)
                {
                    var gvsType = asm.GetType("UnityEditor.GameViewSize");
                    var gvsTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");
                    var ctor = gvsType.GetConstructor(new[] { gvsTypeEnum, typeof(int), typeof(int), typeof(string) });
                    var size = ctor.Invoke(new object[] { Enum.Parse(gvsTypeEnum, "AspectRatio"), W, H, Label });
                    group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                    idx = IndexOf(group, Label);
                }
                if (idx < 0) { Debug.LogWarning("[GameViewPortrait] couldn't register the 9:16 size; recording at current aspect."); return; }

                var gvType = asm.GetType("UnityEditor.GameView");
                var gv = EditorWindow.GetWindow(gvType, false, "Game", false);
                var cb = gvType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                cb.Invoke(gv, new object[] { idx, null });
                gv.Repaint();
                Debug.Log("[GameViewPortrait] Game view set to " + W + "x" + H + " (9:16 portrait).");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameViewPortrait] reflection failed (" + e.GetType().Name + ": " + e.Message +
                                 "); set the Game view to a 9:16 portrait size manually before recording.");
            }
        }

        private static int IndexOf(object group, string label)
        {
            var texts = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
            for (int i = 0; i < texts.Length; i++)
                if (texts[i] != null && texts[i].Contains(label)) return i;
            return -1;
        }
    }
}
