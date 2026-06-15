using UnityEngine;
using UnityEditor;
using System.Linq;

public static class LizardCrossingSceneValidator
{
    [MenuItem("Lizard Crossing/Validate Phase 1 Scene")]
    public static void ValidatePhase1Scene()
    {
        Debug.Log("=== Lizard Crossing Phase 1 Scene Validation ===");

        Check("Player tagged object exists", GameObject.FindGameObjectWithTag("Player") != null);
        Check("Main Camera exists", Camera.main != null);

        var all = Object.FindObjectsOfType<GameObject>();

        Check("Safe Zone object exists", all.Any(o => o.name.ToLower().Contains("safe")));
        Check("Hazard/Shoe/Foot object exists", all.Any(o => 
            o.name.ToLower().Contains("hazard") || 
            o.name.ToLower().Contains("shoe") || 
            o.name.ToLower().Contains("foot")));

        Check("Bug/Collectible object exists", all.Any(o => 
            o.name.ToLower().Contains("bug") || 
            o.name.ToLower().Contains("collectible")));

        Check("HUD/Canvas exists", Object.FindObjectOfType<Canvas>() != null);

        if (Camera.main != null)
        {
            Debug.Log($"Camera position: {Camera.main.transform.position}");
            if (Camera.main.transform.position.y > 3f)
                Debug.LogWarning("Camera may be too high for lizard POV.");
        }

        Debug.Log("=== Validation Complete ===");
    }

    private static void Check(string label, bool passed)
    {
        if (passed) Debug.Log($"PASS: {label}");
        else Debug.LogError($"FAIL: {label}");
    }
}
