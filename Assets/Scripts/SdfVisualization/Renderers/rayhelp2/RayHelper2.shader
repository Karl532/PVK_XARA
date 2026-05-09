Shader "rayhelp2/RayHelper2"
{
	Properties
	{
		//float _Alpha = 0.8;
		//fixed4 _ColorCameraCloser = color(0.0, 1.0, 0.0, _Alpha);
		//fixed4 _ColorMatch = color(1.0, );
		//fixed4 _ColorModelCloser;
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }

		Pass
		{
			Cull[_Cull]
			ZWrite On
			ZTest LEqual

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag

			
			// Properties passed from material fixes

			float _ErrorThreshold;
			
			// Depth texture and metadata from script
			Texture2DArray<float> _DepthTexture;
	        SamplerState sampler_point_clamp;
			float4 _DepthTexture_TexelSize;
			float4x4 _InvDepthViewProj;
			float4x4 _TrackingToWorld;
			bool _FlipY;
			float _MinDepth01;
			float _MaxDepth01;
            int _EyeSlice;
			int _DepthsizeX;
			int _DepthsizeY;
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			
			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float4 screenPos : TEXCOORD2;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.screenPos = ComputeScreenPos(o.pos);
				return o;
			}
			
			// Convert screen UV to depth texture UV
			float2 ConvertToDepthUV(float2 screenUV)
			{
				float2 depthUV = screenUV;
				
				// Handle flipY if needed
				if (_FlipY)
				{
					depthUV.y = 1.0 - depthUV.y;
				}
				
				return depthUV;
			}
			
			// Sample depth texture and convert to world space
			float3 GetCameraWorldDepth(float2 screenUV)
			{
				float2 uv = ConvertToDepthUV(screenUV);

				float2 texSize = int2( //Width and height of depth texture
					_DepthsizeX,
					_DepthsizeY
				);

				int2 pixel = int2(uv * texSize);


				float depth01 = _DepthTexture.Load(
					int4(pixel, _EyeSlice, 0)
				);

				float2 centeredUV = (pixel + 0.5) / texSize;

				float4 clip = float4(
					centeredUV * 2.0 - 1.0,
					depth01 * 2.0 - 1.0,
					1.0
				);

				float4 tracking = mul(_InvDepthViewProj, clip);
				tracking.xyz /= tracking.w;

				float4 world = mul(_TrackingToWorld, tracking);

				return world.xyz;
			}

			
			float4 frag (v2f i) : SV_Target
			{
				

				float modelDepth = distance(i.worldPos, _WorldSpaceCameraPos);
				
				// Get camera world position for this screen pixel

				float3 cameraWorldPos = GetCameraWorldDepth(i.screenPos.xy / i.screenPos.w);
				//float2 depthUV = ConvertToDepthUV(i.screenPos.xy / i.screenPos.w);
				// Ibland har rawdepth något värde men verkar som det aldrig ändras?
				float rawDepth = distance(cameraWorldPos, _WorldSpaceCameraPos);
				return half4(rawDepth, modelDepth, modelDepth-rawDepth, 1.0);
				//float cameraWorldPos;
				/*
				// Get model world position 
				float3 modelWorldPos = i.worldPos;
				
				// Calculate depth difference along camera ray
				float3 cameraRayDirection = normalize(cameraWorldPos - _WorldSpaceCameraPos);
				float3 modelToCamera = modelWorldPos - _WorldSpaceCameraPos;
				
				// Project model position onto camera ray to get depth along same ray
				float modelDepthAlongRay = dot(modelToCamera, cameraRayDirection);
				float cameraDepthAlongRay = dot(cameraWorldPos - _WorldSpaceCameraPos, cameraRayDirection);*/
				
				// This is the key comparison - depth difference along the same ray
				float depthError = modelDepth - cameraWorldPos.z;
				
				// Color based on depth error
				float threshold = _ErrorThreshold;
				float t = saturate(abs(depthError) / (threshold * 2.0));
				float Alpha = 0.8;

				if (abs(depthError) <= threshold)
                {
                    // Close enough to the surface: show match color.
                    return half4(1.0, 1.0, 0.0, Alpha);
                }
                half3 addBase = half3(0.0, 0.9, 0.2);     // green for add
                half3 addFar = half3(0.0, 1.0, 1.0);      // green -> cyan
                half3 removeBase = half3(1.0, 0.0, 0.0);  // red for remove
                half3 removeFar = half3(1.0, 0.6, 0.0);   // red -> orange
                half3 color = depthError < 0.0 ? lerp(addBase, addFar, t) : lerp(removeBase, removeFar, t);
                return half4(color, Alpha);
			}

			ENDCG
		}
	}
	
}
