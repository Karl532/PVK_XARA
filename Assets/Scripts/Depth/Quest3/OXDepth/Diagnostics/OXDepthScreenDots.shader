Shader "Hidden/OXDepth/ScreenDotsDistance"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "ScreenDots"
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
            
            StructuredBuffer<float4> _Points;
            float _DotSizePx;
            float3 _DistFromPosWS;
            float  _MaxDistMeters;
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                uint pointIndex = IN.vertexID / 6;
                uint localVID   = IN.vertexID % 6;
                
                float2 corners[6] = {
                    float2(-1,-1), float2( 1,-1), float2( 1, 1),
                    float2(-1,-1), float2( 1, 1), float2(-1, 1)
                };
                float2 c = corners[localVID];
                
                float3 worldPos = _Points[pointIndex].xyz;
                float4 clip = TransformWorldToHClip(worldPos);
                
                // kill behind camera
                if (clip.w <= 0.0)
                {
                    OUT.positionCS = float4(0,0,0,0);
                    OUT.uv = 0;
                    OUT.worldPos = worldPos;
                    return OUT;
                }
                
                float2 ndcPerPixel = 2.0 / _ScreenParams.xy;
                float2 ndcOffset   = c * (_DotSizePx * 0.5) * ndcPerPixel;
                clip.xy += ndcOffset * clip.w;
                
                OUT.positionCS = clip;
                OUT.uv = c;
                OUT.worldPos = worldPos;
                return OUT;
            }
            
            // Heat map color gradient: Blue -> Cyan -> Green -> Yellow -> Red
            half3 HeatMapColor(float t)
            {
                // t is 0 (close) to 1 (far)
                half3 color;
                
                if (t < 0.25)
                {
                    // Blue to Cyan
                    float local_t = t / 0.25;
                    color = lerp(half3(0, 0, 1), half3(0, 1, 1), local_t);
                }
                else if (t < 0.5)
                {
                    // Cyan to Green
                    float local_t = (t - 0.25) / 0.25;
                    color = lerp(half3(0, 1, 1), half3(0, 1, 0), local_t);
                }
                else if (t < 0.75)
                {
                    // Green to Yellow
                    float local_t = (t - 0.5) / 0.25;
                    color = lerp(half3(0, 1, 0), half3(1, 1, 0), local_t);
                }
                else
                {
                    // Yellow to Red
                    float local_t = (t - 0.75) / 0.25;
                    color = lerp(half3(1, 1, 0), half3(1, 0, 0), local_t);
                }
                
                return color;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                // Circular dot shape
                if (length(IN.uv) > 1.0) discard;
                
                // Calculate distance from camera
                float dist = distance(_DistFromPosWS, IN.worldPos);
                float t = saturate(dist / max(1e-3, _MaxDistMeters));
                
                // Get heat map color
                half3 color = HeatMapColor(t);
                
                // Optional: add center highlight to dots
                float centerGlow = 1.0 - length(IN.uv) * 0.3;
                color *= centerGlow;
                
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}