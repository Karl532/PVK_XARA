using UnityEngine;


    public class MeshTriangleUploader
    {
        // float3 per vertex, 3 vertices per triangle
        public ComputeBuffer TriangleBuffer { get; private set; }
        public int TriangleCount { get; private set; }

        public void Upload(Mesh mesh, Matrix4x4 localToWorkspace)
        {
            Release();

            if (mesh == null) throw new System.ArgumentNullException(nameof(mesh));

            var verts = mesh.vertices;
            var tris = mesh.triangles;
            int triCount = tris.Length / 3;
            TriangleCount = triCount;

            // Flatten to tri vertices
            var triVerts = new Vector3[triCount * 3];

            for (int i = 0; i < triCount; i++)
            {
                int t = i * 3;
                Vector3 v0 = verts[tris[t + 0]];
                Vector3 v1 = verts[tris[t + 1]];
                Vector3 v2 = verts[tris[t + 2]];

                // Transform into workspace space
                triVerts[t + 0] = localToWorkspace.MultiplyPoint3x4(v0);
                triVerts[t + 1] = localToWorkspace.MultiplyPoint3x4(v1);
                triVerts[t + 2] = localToWorkspace.MultiplyPoint3x4(v2);
            }

            TriangleBuffer = new ComputeBuffer(triVerts.Length, sizeof(float) * 3, ComputeBufferType.Structured);
            TriangleBuffer.SetData(triVerts);
        }

        public void Release()
        {
            if (TriangleBuffer != null)
            {
                TriangleBuffer.Release();
                TriangleBuffer = null;
            }
            TriangleCount = 0;
        }
    }

