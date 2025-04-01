Shader "Custom/SpriteOutlineUV"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineThickness ("Outline Thickness", Range(0, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Name "OUTLINE"
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _OutlineColor;
            float _OutlineThickness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // UV 변환 (기본 UV가 0~1 범위로 매핑되어 있다고 가정)
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 기본 이미지 색상 샘플링
                fixed4 col = tex2D(_MainTex, i.uv);

                // UV 좌표가 경계에 가까운지 판단 (0~_OutlineThickness 또는 1-_OutlineThickness~1)
                if(i.uv.x < _OutlineThickness || i.uv.x > (1.0 - _OutlineThickness) ||
                   i.uv.y < _OutlineThickness || i.uv.y > (1.0 - _OutlineThickness))
                {
                    return _OutlineColor;
                }
                return col;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
