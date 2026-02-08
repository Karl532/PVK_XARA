using UnityEngine;

public class BlockScaler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The block/cube to resize from Settings.")]
    public Transform block;

    void Update()
    {
        if (block == null) return;
        Settings settings = SettingsManager.Instance != null ? SettingsManager.Instance.settings : null;
        if (settings == null) return;

        block.localScale = settings.stoneBlockDimensions;
    }
}
