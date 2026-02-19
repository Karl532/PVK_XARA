Shader "Hidden/SdfDepthError"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "SdfDepthError"
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

            Texture2DArray<float> _DepthTex;
            SamplerState sampler_point_clamp;
            float2 _DepthSize;
            int _EyeSlice;
            int _FlipY;
            int _Step;
            float _Alpha;
            float _ErrorScale;
            float _RayStep;
            float _MaxDistance;
            int _MaxSteps;
            float _HitThreshold;
            float4x4 _InvDepthViewProj;
            float4x4 _TrackingToWorld;
            float4x4 _WorldToWorkspace;
            float3 _CameraOriginWS;

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
                float3 posWS      : TEXCOORD0;
                float  valid      : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 ReconstructWorld(int2 pixel, float depth01)
            {
                float2 uv = (float2(pixel) + 0.5) / max(_DepthSize, 1.0);
                float2 ndcXY = uv * 2.0 - 1.0;
                float zNdc = depth01 * 2.0 - 1.0;
                float4 clip = float4(ndcXY.x, ndcXY.y, zNdc, 1.0);

                float4 trackingH = mul(_InvDepthViewProj, clip);
                float3 tracking = trackingH.xyz / max(1e-6, trackingH.w);
                float4 worldH = mul(_TrackingToWorld, float4(tracking, 1.0));
                return worldH.xyz;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                int step = max(1, _Step);
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
                int y = _FlipY != 0 ? (depthSize.y - 1 - pixel.y) : pixel.y;
                float depth01 = _DepthTex.Load(int4(pixel.x, y, _EyeSlice, 0));
                if (depth01 <= 1e-6)
                {
                    OUT.positionCS = float4(0,0,0,0);
                    OUT.posWS = 0;
                    OUT.valid = 0;
                    return OUT;
                }

                float3 posWS = ReconstructWorld(pixel, depth01);
                float4 clip = TransformWorldToHClip(posWS);
                if (clip.w <= 0.0)
                {
                    OUT.positionCS = float4(0,0,0,0);
                    OUT.posWS = posWS;
                    OUT.valid = 0;
                    return OUT;
                }

                OUT.positionCS = clip;
                OUT.posWS = posWS;
                OUT.valid = 1.0;
                return OUT;
            }

            float RaymarchModel(float3 originWS, float3 posWS, float depthDist)
            {
                float3 origin = mul(_WorldToWorkspace, float4(originWS, 1.0)).xyz;
                float3 target = mul(_WorldToWorkspace, float4(posWS, 1.0)).xyz;
                float3 dir = target - origin;
                if (depthDist <= 1e-6)
                    return -1.0;

                dir /= depthDist;

                float3 boxMin = _GlobalCorner;
                float3 boxMax = _GlobalCorner + _GlobalSize;
                float3 invDir = 1.0 / max(abs(dir), 1e-6) * sign(dir);
                float3 t0 = (boxMin - origin) * invDir;
                float3 t1 = (boxMax - origin) * invDir;
                float3 tMin3 = min(t0, t1);
                float3 tMax3 = max(t0, t1);
                float tMin = max(max(tMin3.x, tMin3.y), tMin3.z);
                float tMax = min(min(tMax3.x, tMax3.y), tMax3.z);
                if (tMax < max(tMin, 0.0))
                    return -1.0;

                float maxDist = max(_MaxDistance, 0.01);
                float t = max(tMin, 0.0);
                float tEnd = min(tMax, maxDist + depthDist);
                float stepMin = max(_RayStep, 1e-4);
                float hitEps = max(_HitThreshold, 1e-4);

                [loop]
                for (int i = 0; i < _MaxSteps; i++)
                {
                    if (t > tEnd)
                        break;

                    float3 p = origin + dir * t;
                    float3 uvw = (p - _GlobalCorner) / max(_GlobalSize, 1e-6);
                    if (any(uvw < 0.0) || any(uvw > 1.0))
                    {
                        t += stepMin;
                        continue;
                    }

                    float distTsdf = SAMPLE_TEXTURE3D(_GlobalTsdf3D, sampler_linear_clamp, uvw);
                    float ad = abs(distTsdf);
                    if (ad <= hitEps)
                        return t;

                    float stepMax = max(stepMin, _GlobalMu * 0.5);
                    t += min(stepMax, max(stepMin, ad));
                }

                return -1.0;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                if (IN.valid < 0.5)
                    discard;

                float3 originWS = _CameraOriginWS;
                float depthDist = length(IN.posWS - originWS);
                float hitDist = RaymarchModel(originWS, IN.posWS, depthDist);
                if (hitDist < 0.0)
                    discard;
                float err = hitDist - depthDist;

                float t = saturate(abs(err) / max(_ErrorScale, 1e-4));
                half3 baseColor = half3(0, 1, 0);
                half3 addColor = half3(0, 0.4, 1);
                half3 removeColor = half3(1, 0.2, 0.2);
                half3 color = err < 0.0 ? lerp(baseColor, removeColor, t) : lerp(baseColor, addColor, t);
                return half4(color, _Alpha);
            }
            ENDHLSL
        }
    }
}
