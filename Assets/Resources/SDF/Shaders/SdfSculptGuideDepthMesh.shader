Shader "Hidden/SdfSculptGuideDepthMesh"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "SdfSculptGuideDepthMesh"
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            Texture2D<float> _DepthSmooth;
            SamplerState sampler_point_clamp;
            float2 _DepthSize;
            int _MeshStep;
            float4x4 _InvDepthViewProj;
            float4x4 _TrackingToWorld;
            float _Alpha;
            float4x4 _WorldToWorkspace;

            TEXTURE3D(_GlobalTsdf3D);
            SAMPLER(sampler_linear_clamp);
            float3 _GlobalCorner;
            float3 _GlobalSize;
            float _GlobalMu;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 posWS : TEXCOORD0;
                float valid : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                int step = max(1, _MeshStep);
                int2 depthSize = (int2)_DepthSize;
                int gridW = max(2, depthSize.x / step);
                int gridH = max(2, depthSize.y / step);
                uint cellIndex = IN.vertexID / 6;
                uint localVID = IN.vertexID % 6;

                uint cellX = cellIndex % (uint)(gridW - 1);
                uint cellY = cellIndex / (uint)(gridW - 1);

                int2 corners[6] = {
                    int2(0,0), int2(1,0), int2(1,1),
                    int2(0,0), int2(1,1), int2(0,1)
                };

                int2 corner = corners[localVID];
                int2 pixel = int2(cellX * step, cellY * step) + corner * step;
                pixel = clamp(pixel, int2(0,0), depthSize - 1);

                float depth01 = _DepthSmooth.Load(int3(pixel, 0));
                if (depth01 <= 1e-6)
                {
                    OUT.positionCS = float4(0,0,0,0);
                    OUT.posWS = 0;
                    OUT.valid = 0;
                    return OUT;
                }

                float2 uv = (float2(pixel) + 0.5) / max(_DepthSize, 1.0);
                float2 ndcXY = uv * 2.0 - 1.0;
                float zNdc = depth01 * 2.0 - 1.0;
                float4 clip = float4(ndcXY.x, ndcXY.y, zNdc, 1.0);

                float4 trackingH = mul(_InvDepthViewProj, clip);
                float3 tracking = trackingH.xyz / max(1e-6, trackingH.w);
                float4 worldH = mul(_TrackingToWorld, float4(tracking, 1.0));
                float3 posWS = worldH.xyz;

                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.posWS = posWS;
                OUT.valid = 1.0;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                if (IN.valid < 0.5)
                    discard;

                float3 posWS = mul(_WorldToWorkspace, float4(IN.posWS, 1.0)).xyz;
                float3 uvw = (posWS - _GlobalCorner) / max(_GlobalSize, 1e-6);
                if (any(uvw < 0.0) || any(uvw > 1.0))
                    discard;

                float distTsdf = SAMPLE_TEXTURE3D(_GlobalTsdf3D, sampler_linear_clamp, uvw);
                float mu = max(_GlobalMu, 1e-6);
                float t = saturate(abs(distTsdf) / mu);
                half3 color = distTsdf >= 0.0
                    ? lerp(half3(0, 1, 0), half3(1, 0, 0), t)
                    : lerp(half3(0, 1, 0), half3(0, 0, 1), t);
                return half4(color, _Alpha);
            }
            ENDHLSL
        }
    }
}
