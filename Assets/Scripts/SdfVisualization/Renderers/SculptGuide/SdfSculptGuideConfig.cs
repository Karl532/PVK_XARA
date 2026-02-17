using UnityEngine;

[CreateAssetMenu(fileName = "SdfSculptGuideConfig", menuName = "SDF/SdfSculptGuideConfig")]
public class SdfSculptGuideConfig : ScriptableObject
{
    [Header("Surface Guide")]
    [Tooltip("Max distance to visualize (meters).")]
    public float maxDistanceMeters = 0.05f;
    public float alpha = 0.6f;
}
