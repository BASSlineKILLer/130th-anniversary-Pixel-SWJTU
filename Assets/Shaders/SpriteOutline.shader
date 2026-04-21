Shader "Custom/SpriteOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,0.7)
        _OutlineWidth ("Outline Width (texels)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref 128
            ReadMask 128
            Comp Equal
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                half4  _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineWidth;

                half centerAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;

                // 透明像素直接丢弃
                clip(centerAlpha - 0.01);

                // 采样上下左右四个邻居
                half aUp    = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, texelSize.y)).a;
                half aDown  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(0, texelSize.y)).a;
                half aLeft  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(texelSize.x, 0)).a;
                half aRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(texelSize.x, 0)).a;

                // 四个邻居都不透明 → 内部像素 → 丢弃
                half minNeighbor = min(min(aUp, aDown), min(aLeft, aRight));
                clip(0.01 - minNeighbor);

                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
