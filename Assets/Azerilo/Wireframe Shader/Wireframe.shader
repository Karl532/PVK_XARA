Shader "Azerilo/Wireframe"
{
	Properties
	{
	    [HDR]  _WireColor ("Wire color", Color) = (0, 0, 0, 1)
	    _WireSize ("Wire size", float) = 0.3
	    _ZMode("Z Mode", Range(-1,1)) = 1
 	   _Cull("Cull Mode", Float) = 2.0
	}

	SubShader
	{
		// Draw the wireframe as an overlay so it remains visible even when
		// inside or behind other geometry (e.g., the placement block).
		Tags { "RenderType"="Transparent" "Queue"="Overlay+10" }

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Cull[_Cull]
			ZWrite Off
			// Force depth test to always pass so edges are visible through
			// opaque geometry. The _ZMode property is still used internally
			// by Core.cginc for other calculations.
			ZTest Always

			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Core.cginc"
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
