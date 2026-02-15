Shader "SDF/OverlayComposite"
{
    Properties
    {
        _OverlayTex ("Overlay", 2D) = "black" {}
        _OverlayStrength ("Strength", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _OverlayTex;
            float _OverlayStrength;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }

            fixed4 frag(v2f i):SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv);
                fixed4 ov = tex2D(_OverlayTex, i.uv);
                ov.a *= _OverlayStrength;
                return lerp(baseCol, fixed4(1,0,0,1), ov.a); // tint red for now
            }
            ENDHLSL
        }
    }
}
