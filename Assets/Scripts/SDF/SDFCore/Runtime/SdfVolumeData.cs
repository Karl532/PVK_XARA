using UnityEngine;

    /// <summary>
    /// Runtime data container for a TSDF volume.
    /// All bounds are defined in WORKSPACE SPACE.
    /// </summary>
    public struct SdfVolumeData
    {
        /// <summary>
        /// 3D texture storing truncated distance values (RHalf).
        /// Values are distances in meters, clamped to ±Mu.
        /// </summary>
        public RenderTexture Tsdf;

        /// <summary>
        /// Resolution of the volume (e.g. 128 or 256).
        /// Assumed cubic (Resolution³).
        /// </summary>
        public int Resolution;

        /// <summary>
        /// Minimum corner of the volume in workspace space.
        /// </summary>
        public Vector3 Corner;

        /// <summary>
        /// Physical size of the volume in meters (workspace space).
        /// </summary>
        public Vector3 Size;

        /// <summary>
        /// Truncation distance μ (meters).
        /// Distances are clamped to [0, Mu] for unsigned TSDF.
        /// </summary>
        public float Mu;

        /// <summary>
        /// True if texture exists and is created.
        /// </summary>
        public bool IsValid =>
            Tsdf != null &&
            Tsdf.IsCreated();

        /// <summary>
        /// Convert a workspace-space position to normalized UVW (0–1).
        /// </summary>
        public Vector3 WorkspaceToUVW(Vector3 posWS)
        {
            Vector3 local = posWS - Corner;

            return new Vector3(
                Size.x > 0f ? local.x / Size.x : 0f,
                Size.y > 0f ? local.y / Size.y : 0f,
                Size.z > 0f ? local.z / Size.z : 0f
            );
        }

        public SdfVolumeData(
        RenderTexture tsdf,
        Vector3 corner,
        Vector3 size,
        int resolution,
        float mu)
        {
            Tsdf = tsdf;
            Corner = corner;
            Size = size;
            Resolution = resolution;
            Mu = mu;
        }


    /// <summary>
    /// Check if a workspace-space position lies inside the volume bounds.
    /// </summary>
    public bool Contains(Vector3 posWS)
        {
            Vector3 max = Corner + Size;

            return posWS.x >= Corner.x && posWS.x <= max.x &&
                   posWS.y >= Corner.y && posWS.y <= max.y &&
                   posWS.z >= Corner.z && posWS.z <= max.z;
        }

        /// <summary>
        /// Voxel size in meters (assuming cubic voxels).
        /// </summary>
        public float VoxelSize =>
            Resolution > 0 ? Size.x / Resolution : 0f;
    }

