using UnityEngine;

// From the work packet (03_SelfTesting/Unity_Test_Scripts). Attached to every
// hazard so the editor flags any hazard that stops moving sideways across the
// lizard's route — the game's most important design rule.
public class SidewaysHazardDirectionHint : MonoBehaviour
{
    [Header("Direction validation helper")]
    [Tooltip("For a Z-forward level, hazards should mostly move along X.")]
    public Vector3 expectedSidewaysAxis = Vector3.right;

    [Tooltip("Hazard movement direction. Should usually be left/right across the path.")]
    public Vector3 movementDirection = Vector3.right;

    private void OnValidate()
    {
        movementDirection.y = 0f;
        expectedSidewaysAxis.y = 0f;

        if (movementDirection.sqrMagnitude > 0.001f && expectedSidewaysAxis.sqrMagnitude > 0.001f)
        {
            float alignment = Mathf.Abs(Vector3.Dot(movementDirection.normalized, expectedSidewaysAxis.normalized));
            if (alignment < 0.7f)
            {
                Debug.LogWarning($"{name}: Hazard may not be moving sideways across the lizard's path. Check direction.");
            }
        }
    }
}
