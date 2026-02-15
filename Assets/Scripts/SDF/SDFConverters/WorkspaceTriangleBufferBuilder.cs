using System;
using UnityEngine;

    /// <summary>
    /// Builds a ComputeBuffer<float3> containing triangle vertex positions in WORKSPACE space.
    /// Layout: 3 float3s per triangle (flattened).
    /// </summary>
    public sealed class WorkspaceTriangleBufferBuilder : IDisposable
    {
        public ComputeBuffer TriangleBuffer { get; private set; }
        public int TriangleCount { get; private set; }

        public void Build(Mesh mesh, Matrix4x4 modelLocalToWorkspace)
        {
            Release();

            if (mesh == null) throw new ArgumentNullException(nameof(mesh));

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            int triCount = triangles.Length / 3;
            TriangleCount = triCount;

            var triVerts = new Vector3[triCount * 3];

            for (int i = 0; i < triCount; i++)
            {
                int t = i * 3;

                Vector3 v0 = vertices[triangles[t + 0]];
                Vector3 v1 = vertices[triangles[t + 1]];
                Vector3 v2 = vertices[triangles[t + 2]];

                triVerts[t + 0] = modelLocalToWorkspace.MultiplyPoint3x4(v0);
                triVerts[t + 1] = modelLocalToWorkspace.MultiplyPoint3x4(v1);
                triVerts[t + 2] = modelLocalToWorkspace.MultiplyPoint3x4(v2);
            }

            TriangleBuffer = new ComputeBuffer(triVerts.Length, sizeof(float) * 3, ComputeBufferType.Structured);
            TriangleBuffer.SetData(triVerts);
        }

        public void Release()
        {
            TriangleBuffer?.Release();
            TriangleBuffer = null;
            TriangleCount = 0;
        }

        public void Dispose() => Release();
    }

