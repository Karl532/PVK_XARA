using UnityEngine;

/// <summary>
/// Placeholder for sculpt guide surface mesh generation.
/// This will own mesh buffers and depth-to-surface reconstruction.
/// </summary>
public class SdfSculptGuideSurfaceMesh
{
    public Mesh Mesh { get; private set; }

    public void EnsureMesh()
    {
        if (Mesh != null)
            return;
        Mesh = new Mesh { name = "SdfSculptGuideSurfaceMesh" };
        Mesh.MarkDynamic();
    }

    public void Clear()
    {
        if (Mesh != null)
            Mesh.Clear();
    }
}
