using UnityEngine;

public sealed class SdfWorkspaceState
{
    public Transform Root { get; private set; }
    public Vector3 Size { get; private set; }
    public Vector3 Corner { get; private set; }

    public bool IsValid => Root != null;

    public bool Update(Transform root, Vector3 size)
    {
        bool changed = Root != root || Size != size;

        Root = root;
        Size = size;
        Corner = -0.5f * Size;

        return changed;
    }
}
