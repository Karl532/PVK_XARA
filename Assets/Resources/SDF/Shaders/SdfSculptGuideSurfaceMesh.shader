Shader "Hidden/SdfSculptGuideSurfaceMesh"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "SdfSculptGuideSurfaceMesh"
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_DepthTex);
            SAMPLER(sampler_linear_clamp);

            float4 _DepthSize; // xy
            int _EyeSlice;
            int _FlipY;
            float4x4 _InvDepthViewProj;
            float4x4 _TrackingToWorld;
            float4x4 _WorldToWorkspace;

            TEXTURE3D(_GlobalTsdf3D);
            float3 _GlobalCorner;
            float3 _GlobalSize;
            float _GlobalMu;
            Texture3D<float> _CacheTex;
            int _CacheResolution;
            int _UseCache;

            float _Alpha;
            float4 _InsideColor;
            float4 _OutsideColor;
            float4 _SurfaceColor;

            bool RayBoxIntersect(float3 ro, float3 rd, float3 bmin, float3 bmax, out float t0, out float t1)
            {
                float3 inv = 1.0 / rd;
                float3 tmin = (bmin - ro) * inv;
                float3 tmax = (bmax - ro) * inv;
                float3 t1v = min(tmin, tmax);
                float3 t2v = max(tmin, tmax);
                t0 = max(max(t1v.x, t1v.y), t1v.z);
                t1 = min(min(t2v.x, t2v.y), t2v.z);
                return t1 >= max(t0, 0.0);
            }

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 ws : TEXCOORD1;
                float valid : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.uv = IN.uv;
                OUT.valid = 0.0;
                OUT.ws = 0;

                float2 uv = IN.uv;
                if (_FlipY != 0) uv.y = 1.0 - uv.y;

                float depth01 = _DepthTex.SampleLevel(sampler_linear_clamp, float3(uv, _EyeSlice), 0);
                if (depth01 <= 1e-6)
                {
                    OUT.positionCS = float4(0,0,0,0);
                    return OUT;
                }

                float2 ndcXY = uv * 2.0 - 1.0;
                float zNdc = depth01 * 2.0 - 1.0;
                float4 clip = float4(ndcXY.x, ndcXY.y, zNdc, 1.0);

                float4 trackingH = mul(_InvDepthViewProj, clip);
                float3 tracking = trackingH.xyz / max(1e-6, trackingH.w);
                float4 worldH = mul(_TrackingToWorld, float4(tracking, 1.0));
                float3 world = worldH.xyz;

                OUT.ws = mul(_WorldToWorkspace, float4(world, 1.0)).xyz;
                OUT.positionCS = TransformWorldToHClip(world);
                OUT.valid = 1.0;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                if (IN.valid < 0.5)
                    discard;

                float3 uvw = (IN.ws - _GlobalCorner) / max(_GlobalSize, 1e-6);
                if (any(uvw < 0.0) || any(uvw > 1.0))
                    discard;

                float dist = SAMPLE_TEXTURE3D(_GlobalTsdf3D, sampler_linear_clamp, uvw);

                // Cache currently does not gate rendering; accumulation happens elsewhere.

                float t = saturate(abs(dist) / max(_GlobalMu, 1e-6));
                float4 color = dist >= 0.0
                    ? lerp(_SurfaceColor, _OutsideColor, t)
                    : lerp(_SurfaceColor, _InsideColor, t);

                color.a *= _Alpha;
                return color;
            }
            ENDHLSL
        }
    }
}
