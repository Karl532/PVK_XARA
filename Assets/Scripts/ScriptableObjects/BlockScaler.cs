using UnityEngine;
using TMPro;

public class BlockScalerTMP : MonoBehaviour
{
    [Header("References")]
    public Transform block; // The block/cube to resize
    public TMP_InputField inputWidth;
    public TMP_InputField inputHeight;
    public TMP_InputField inputLength;

    [Header("Settings")]
    public Settings settings; // Reference to your ScriptableObject

    void Start()
    {
        // Initialize block size from Settings if autoScaleBlock is enabled
        if (settings != null && settings.autoScaleBlock && block != null)
        {
            block.localScale = settings.stoneBlockDimensions;
        }

        // Initialize input fields with current block size
        if (block != null)
        {
            inputWidth.text = block.localScale.x.ToString("F2");
            inputHeight.text = block.localScale.y.ToString("F2");
            inputLength.text = block.localScale.z.ToString("F2");
        }

        // Add listeners to update block when input changes
        inputWidth.onEndEdit.AddListener(UpdateWidth);
        inputHeight.onEndEdit.AddListener(UpdateHeight);
        inputLength.onEndEdit.AddListener(UpdateLength);
    }

    void UpdateWidth(string value)
    {
        if (float.TryParse(value, out float w) && block != null)
        {
            Vector3 scale = block.localScale;
            scale.x = Mathf.Max(w, 0.01f); // prevent zero/negative scale
            block.localScale = scale;

            // Update Settings to match
            if (settings != null)
            {
                settings.stoneBlockDimensions.x = scale.x;
            }
        }
    }

    void UpdateHeight(string value)
    {
        if (float.TryParse(value, out float h) && block != null)
        {
            Vector3 scale = block.localScale;
            scale.y = Mathf.Max(h, 0.01f);
            block.localScale = scale;

            if (settings != null)
            {
                settings.stoneBlockDimensions.y = scale.y;
            }
        }
    }

    void UpdateLength(string value)
    {
        if (float.TryParse(value, out float l) && block != null)
        {
            Vector3 scale = block.localScale;
            scale.z = Mathf.Max(l, 0.01f);
            block.localScale = scale;

            if (settings != null)
            {
                settings.stoneBlockDimensions.z = scale.z;
            }
        }
    }
}
