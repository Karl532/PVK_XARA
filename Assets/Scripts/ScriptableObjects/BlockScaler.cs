using UnityEngine;
using TMPro; // TextMeshPro namespace

public class BlockScalerTMP : MonoBehaviour
{
    public Transform block; // The block/cube you want to resize
    public TMP_InputField inputWidth;
    public TMP_InputField inputHeight;
    public TMP_InputField inputLength;

    void Start()
    {
        // Initialize input fields with current block size
        inputWidth.text = block.localScale.x.ToString("F2");
        inputHeight.text = block.localScale.y.ToString("F2");
        inputLength.text = block.localScale.z.ToString("F2");

        // Add listeners to update block when input changes
        inputWidth.onEndEdit.AddListener(UpdateWidth);
        inputHeight.onEndEdit.AddListener(UpdateHeight);
        inputLength.onEndEdit.AddListener(UpdateLength);
    }

    void UpdateWidth(string value)
    {
        if(float.TryParse(value, out float w))
        {
            Vector3 scale = block.localScale;
            scale.x = Mathf.Max(w, 0.01f); // prevent zero/negative scale
            block.localScale = scale;
        }
    }

    void UpdateHeight(string value)
    {
        if(float.TryParse(value, out float h))
        {
            Vector3 scale = block.localScale;
            scale.y = Mathf.Max(h, 0.01f);
            block.localScale = scale;
        }
    }

    void UpdateLength(string value)
    {
        if(float.TryParse(value, out float l))
        {
            Vector3 scale = block.localScale;
            scale.z = Mathf.Max(l, 0.01f);
            block.localScale = scale;
        }
    }
}
