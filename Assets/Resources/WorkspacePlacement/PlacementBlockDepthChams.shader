Shader "PVK/PlacementBlockDepthChams"
{
    Properties
    {
        _FrontColor("Front Color", Color) = (0.2, 0.6, 1.0, 0.35)
        _BackColor("Back Color", Color) = (0.05, 0.2, 0.4, 0.2)
        _Alpha("Alpha", Range(0,1)) = 0.35
        _OcclusionBias("Occlusion Bias (m)", Range(0, 0.2)) = 0.02
        _OcclusionSoftness("Occlusion Softness (m)", Range(0.001, 0.3)) = 0.06
        _UseEnvDepth("Use Env Depth", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _STEREO_INSTANCING_ON _STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FrontColor;
                float4 _BackColor;
                float _Alpha;
                float _OcclusionBias;
                float _OcclusionSoftness;
                float _UseEnvDepth;
            CBUFFER_END

            TEXTURE2D(_EnvironmentDepthTexture);
            SAMPLER(sampler_EnvironmentDepthTexture);

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 baseColor = _FrontColor;
                baseColor.a *= _Alpha;

                if (_UseEnvDepth > 0.5)
                {
                    float2 uv = i.screenPos.xy / i.screenPos.w;
                    uv = UnityStereoTransformScreenSpaceTex(uv);

                    // Environment depth in meters (Oculus depth) if available.
                    float envDepth = SAMPLE_TEXTURE2D(_EnvironmentDepthTexture, sampler_EnvironmentDepthTexture, uv).r;

                    // Fragment depth in meters (linear eye depth).
                    float fragDepth = LinearEyeDepth(i.positionCS.z / i.positionCS.w, _ZBufferParams);

                    // If env depth is invalid/zero, skip occlusion.
                    if (envDepth > 0.0001)
                    {
                        float delta = envDepth - fragDepth;
                        // delta > 0 => fragment is in front of real world.
                        float t = saturate((delta + _OcclusionBias) / max(_OcclusionSoftness, 1e-4));
                        float4 behindColor = _BackColor;
                        behindColor.a *= _Alpha;
                        baseColor = lerp(behindColor, baseColor, t);

                        // When far behind, fade more to avoid full overwrite.
                        float fade = saturate(t + 0.1);
                        baseColor.a *= fade;
                    }
                }

                return baseColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
