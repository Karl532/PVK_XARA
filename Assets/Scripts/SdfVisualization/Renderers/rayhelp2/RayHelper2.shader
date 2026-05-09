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
			float rawDepth = tex2D(_DepthTexture, depthUV).r;
			
			// Reconstruct camera space position from depth
			float4 devicePos = float4(screenUV * 2.0 - 1.0, rawDepth * 2.0 - 1.0, 1.0);
			float4 cameraSpacePos = mul(_InvDepthViewProj, devicePos);
			cameraSpacePos.xyz /= cameraSpacePos.w;
			
			// Convert to world space
			float4 worldSpacePos = mul(unity_CameraToWorld, cameraSpacePos);
			return worldSpacePos.xyz;
		}

		
		float4 frag (v2f i) : SV_Target
		{
			// Get model depth from z-buffer
			float modelDepth = i.screenPos.z / i.screenPos.w;
			
			if (modelDepth >= 0.999)
			{
				discard;
			}
			
			// Get camera world position for this screen pixel
			float3 cameraWorldPos = GetCameraWorldDepth(i.screenPos.xy / i.screenPos.w);
			
			// Get model world position 
			float3 modelWorldPos = i.worldPos;
			
			// Calculate depth difference along camera ray
			float3 cameraRayDirection = normalize(cameraWorldPos - _WorldSpaceCameraPos);
			float3 modelToCamera = modelWorldPos - _WorldSpaceCameraPos;
			
			// Project model position onto camera ray to get depth along same ray
			float modelDepthAlongRay = dot(modelToCamera, cameraRayDirection);
			float cameraDepthAlongRay = dot(cameraWorldPos - _WorldSpaceCameraPos, cameraRayDirection);
			
			// This is the key comparison - depth difference along the same ray
			float depthError = modelDepthAlongRay - cameraDepthAlongRay;
			
			// Color based on depth error
			float threshold = _ErrorThreshold;
			if (abs(depthError) <= threshold)
			{
				return _ColorMatch; // Yellow for match
			}
			else if (depthError < 0)
			{
				// Model is closer to camera than expected - "adding" to scene
				float t = saturate(abs(depthError) / (threshold * 2.0));
				return lerp(_ColorNoDepth, _ColorCameraCloser, t);
			}
			else
			{
				// Model is farther from camera than expected - "removing" from scene  
				float t = saturate(abs(depthError) / (threshold * 2.0));
				return lerp(_ColorNoDepth, _ColorModelCloser, t);
			}
		}

		ENDCG
	}
}
