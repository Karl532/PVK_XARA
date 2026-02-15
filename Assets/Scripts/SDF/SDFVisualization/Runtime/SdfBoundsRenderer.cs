using UnityEngine;

    /// <summary>
    /// Runtime wireframe bounds renderer (Quest-friendly).
    /// Uses 3 LineRenderers (workspace/global/local). No Gizmos.
    /// All corners/sizes are in WORKSPACE space; workspaceRoot defines workspace->world.
    /// </summary>
    public class SdfBoundsRenderer : MonoBehaviour
    {
        [Header("Workspace frame")]
        [SerializeField] private Transform workspaceRoot;

        [Header("Line material (Unlit)")]
        [SerializeField] private Material lineMaterial;

        [Header("Widths (meters)")]
        [SerializeField] private float lineWidth = 0.002f;

        [Header("Enable")]
        [SerializeField] private bool drawWorkspace = true;
        [SerializeField] private bool drawGlobal = true;
        [SerializeField] private bool drawLocal = true;

        private LineRenderer _ws;
        private LineRenderer _global;
        private LineRenderer _local;

        private static readonly int[] BoxEdgeOrder =
        {
            // bottom rectangle (0-3)
            0,1, 1,2, 2,3, 3,0,
            // top rectangle (4-7)
            4,5, 5,6, 6,7, 7,4,
            // verticals
            0,4, 1,5, 2,6, 3,7
        };

        private void Awake()
        {
            _ws = CreateLR("WorkspaceBounds");
            _global = CreateLR("GlobalBounds");
            _local = CreateLR("LocalBounds");
        }

        private LineRenderer CreateLR(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.material = lineMaterial;
            lr.widthMultiplier = lineWidth;
            lr.positionCount = BoxEdgeOrder.Length; // we’ll feed pairs as a line list (not continuous)
            lr.numCapVertices = 0;
            lr.numCornerVertices = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            // LineRenderer draws a continuous strip; to emulate separate segments,
            // we duplicate endpoints so it “jumps” between edges.
            // The order list already does pairs, we’ll write them as:
            // p0,p1, p1,p2, p2,p3... which is continuous but OK for a box.
            return lr;
        }

        /// <summary>
        /// Set all bounds (workspace-space min corner + size).
        /// Provide any you don't want to draw with drawX=false.
        /// </summary>
        public void Render(
            Transform workspaceRootTransform,
            Vector3 workspaceCornerWS, Vector3 workspaceSizeWS,
            Vector3 globalCornerWS, Vector3 globalSizeWS,
            Vector3 localCornerWS, Vector3 localSizeWS)
        {
            workspaceRoot = workspaceRootTransform;

            if (workspaceRoot == null || lineMaterial == null) return;

            if (drawWorkspace) UpdateBox(_ws, workspaceCornerWS, workspaceSizeWS, enabled: true);
            else _ws.enabled = false;

            if (drawGlobal) UpdateBox(_global, globalCornerWS, globalSizeWS, enabled: true);
            else _global.enabled = false;

            if (drawLocal) UpdateBox(_local, localCornerWS, localSizeWS, enabled: true);
            else _local.enabled = false;
        }

        private void UpdateBox(LineRenderer lr, Vector3 cornerWS, Vector3 sizeWS, bool enabled)
        {
            lr.enabled = enabled;

            // 8 corners in workspace space
            Vector3 min = cornerWS;
            Vector3 max = cornerWS + sizeWS;

            Vector3[] c = new Vector3[8];
            c[0] = new Vector3(min.x, min.y, min.z);
            c[1] = new Vector3(max.x, min.y, min.z);
            c[2] = new Vector3(max.x, min.y, max.z);
            c[3] = new Vector3(min.x, min.y, max.z);
            c[4] = new Vector3(min.x, max.y, min.z);
            c[5] = new Vector3(max.x, max.y, min.z);
            c[6] = new Vector3(max.x, max.y, max.z);
            c[7] = new Vector3(min.x, max.y, max.z);

            // Convert to world space
            for (int i = 0; i < 8; i++)
                c[i] = workspaceRoot.TransformPoint(c[i]);

            // Write the line strip positions
            // (LineRenderer is a strip, so this draws edges sequentially; good enough for debug.)
            var positions = new Vector3[BoxEdgeOrder.Length];
            for (int i = 0; i < BoxEdgeOrder.Length; i++)
                positions[i] = c[BoxEdgeOrder[i]];

            lr.positionCount = positions.Length;
            lr.SetPositions(positions);
        }
    }

