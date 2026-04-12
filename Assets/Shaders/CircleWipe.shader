Shader "Custom/CircleWipe"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 1.0
        _Softness ("Softness", Range(0, 0.1)) = 0.005
        _Aspect ("Aspect Ratio", Float) = 1.7778
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay+100"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _Center;
            float _Radius;
            float _Softness;
            float _Aspect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = _Center.xy;
                float2 diff = i.uv - center;

                // 用宽高比校正 x 轴，使圆形在任何分辨率下都保持正圆
                diff.x *= _Aspect;

                float dist = length(diff);

                // 圆内透明 (alpha=0)，圆外黑色 (alpha=1)
                // smoothstep 在 _Radius 附近产生平滑过渡
                float halfSoft = _Softness * 0.5;
                float mask = smoothstep(_Radius - halfSoft, _Radius + halfSoft, dist);

                return fixed4(0, 0, 0, mask);
            }
            ENDCG
        }
    }
}
