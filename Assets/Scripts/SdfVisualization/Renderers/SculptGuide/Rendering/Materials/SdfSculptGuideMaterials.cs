using UnityEngine;

public sealed class SdfSculptGuideMaterials
{
    public Material CachePointsMaterial { get; private set; }
    public Material PointCloudMaterial { get; private set; }
    public Material MeshMaterial { get; private set; }

    public bool EnsureMaterials(Shader cachePointsShader, Shader pointCloudShader, Shader betweenShader, Shader meshShader)
    {
        if (CachePointsMaterial == null && cachePointsShader != null)
            CachePointsMaterial = new Material(cachePointsShader) { name = "SdfSculptGuideCachePointsMaterial" };

        if (PointCloudMaterial == null && pointCloudShader != null)
            PointCloudMaterial = new Material(pointCloudShader) { name = "SdfSculptGuidePointCloudMaterial" };

        if (MeshMaterial == null && meshShader != null)
            MeshMaterial = new Material(meshShader) { name = "SdfSculptGuideDepthMeshMaterial" };

        return PointCloudMaterial != null || CachePointsMaterial != null || MeshMaterial != null;
    }

    public void Release()
    {
        if (CachePointsMaterial != null)
        {
            Object.Destroy(CachePointsMaterial);
            CachePointsMaterial = null;
        }

        if (PointCloudMaterial != null)
        {
            Object.Destroy(PointCloudMaterial);
            PointCloudMaterial = null;
        }

        if (MeshMaterial != null)
        {
            Object.Destroy(MeshMaterial);
            MeshMaterial = null;
        }

    }
}
