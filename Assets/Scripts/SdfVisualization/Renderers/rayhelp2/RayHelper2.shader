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
		fixed4 _ColorNoDepth = color(0f, );
		fixed4 _ColorCameraCloser;
		fixed4 _ColorMatch;
		fixed4 _ColorModelCloser;
		float _ErrorThreshold;
		
		// Depth texture and metadata from script
		sampler2D _DepthTexture;
		float4 _DepthTexture_TexelSize;
		float4x4 _InvDepthViewProj;
		float4x4 _TrackingToWorld;
		bool _FlipY;
		float _MinDepth01;
		float _MaxDepth01;
		
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
			float2 depthUV = ConvertToDepthUV(screenUV);
			
			// Sample depth texture (assuming it's a single channel depth texture)
			float rawDepth = tex2D(_DepthTexture, depthUV).r;
			
			// Convert from normalized depth to world space
			// Reconstruct camera space position from depth
			float deviceDepth = rawDepth;
			
			// Linearize depth if needed (depends on depth texture format)
			// For Quest 3, this might be different from Unity's depth
			float linearDepth = lerp(_MaxDepth01, _MinDepth01, rawDepth);
			
			// Get device position
			float4 devicePos = float4(screenUV * 2.0 - 1.0, deviceDepth * 2.0 - 1.0, 1.0);
			
			// Transform to world space using inverse depth view projection
			float4 worldPos = mul(_InvDepthViewProj, devicePos);
			worldPos.xyz /= worldPos.w;
			
			return worldPos.xyz;
		}
		
		fixed4 frag (v2f i) : SV_Target
		{
			// Get model depth from z-buffer
			float modelDepth = i.screenPos.z / i.screenPos.w;
			
			// Check if model depth is at far clipping plane (no valid depth)
			if (modelDepth >= 0.999)
			{
				discard;
			}
			
			// Get camera depth in world space
			float3 cameraWorldPos = GetCameraWorldDepth(i.screenPos.xy / i.screenPos.w);
			
			// Convert model position to camera space for comparison
			float3 modelWorldPos = i.worldPos;
			
			// Calculate distance from camera to both points
			float3 cameraForward = normalize(_WorldSpaceCameraPos - cameraWorldPos);
			float3 modelCameraPos = mul(_InvDepthViewProj, float4(modelWorldPos, 1.0)).xyz;
			
			// Compare depths along the camera ray. These rays should overlap, maybe add debug check if it doesn't work
			float cameraDepth = distance(cameraWorldPos, _WorldSpaceCameraPos);
			float modelDepthWorld = distance(modelCameraPos, _WorldSpaceCameraPos);

            float err = hitDist - depthDistWS;

            // Expand the gradient range so it doesn't clamp too quickly.
            float scale = max(_ErrorScale * 4.0, 1e-4);
            float t = saturate(abs(err) / max(scale, 1e-4));
            float matchThreshold = max(_ErrorScale, 1e-4); // fixes Maybe multiply by workspace scaling?
            if (abs(err) <= matchThreshold)
            {
                // Close enough to the surface: show match color.
                return half4(1.0, 1.0, 0.0, _Alpha);
            }
            half3 addBase = half3(0.0, 0.9, 0.2);     // green for add
            half3 addFar = half3(0.0, 1.0, 1.0);      // green -> cyan
            half3 removeBase = half3(1.0, 0.6, 0.0);  // orange for remove
            half3 removeFar = half3(1.0, 0.0, 0.0);   // orange -> red
            half3 color = err < 0.0 ? lerp(addBase, addFar, t) : lerp(removeBase, removeFar, t);
            return half4(color, _Alpha);
			

		}
		ENDCG
	}
}
