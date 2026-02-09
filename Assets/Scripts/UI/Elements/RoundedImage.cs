using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RoundedImage : MonoBehaviour
{
    [SerializeField] private float cornerRadius = 20f;
    private Image image;
    private Material roundedMaterial;

    void Awake()
    {
        image = GetComponent<Image>();
        ApplyRoundedCorners();
    }

    public void ApplyRoundedCorners()
    {
        if (image == null) image = GetComponent<Image>();

        // Create material instance
        Shader roundedShader = Shader.Find("UI/RoundedCorners");
        if (roundedShader != null)
        {
            roundedMaterial = new Material(roundedShader);
            roundedMaterial.SetFloat("_Radius", cornerRadius);
            image.material = roundedMaterial;
        }
        else
        {
            Debug.LogWarning("RoundedCorners shader not found.");
        }
    }

    public void SetRadius(float radius)
    {
        cornerRadius = radius;
        if (roundedMaterial != null)
        {
            roundedMaterial.SetFloat("_Radius", radius);
        }
    }

    public void SetColor(Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }

    void OnDestroy()
    {
        if (roundedMaterial != null)
        {
            Destroy(roundedMaterial);
        }
    }
}