using UnityEngine;

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
