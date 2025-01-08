Shader "MPS/TwoSidedCard"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Sprite Sheet", 2D) = "white" {}
        _Rows ("Sheet Rows", Integer) = 1
        _Cols ("Sheet Cols", Integer) = 1
        _FrontIndex ("Front Card Index", Integer) = 0
        _BackIndex ("Back Card Index", Integer) = 0
        _FlipThreshold ("Flip Threshold", Range(-1, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "PreviewType"="Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off // Disable culling so both sides are rendered

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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float facing : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            uint _Rows;
            uint _Cols;

            uint _FrontIndex;
            uint _BackIndex;
            float _FlipThreshold;

            // Function to convert index to UV coordinates
            float2 IndexToUV(int index, float2 originalUV)
            {
                int row = index / _Cols;
                int col = index % _Cols;

                float cell_width = 1.0 / _Cols;
                float cell_height = 1.0 / _Rows;

                float2 spriteUV = float2(
                    (col + originalUV.x) * cell_width,
                    1.0 - ((row + originalUV.y) * cell_height)
                    // Flip Y coordinate since texture coords start from bottom
                );

                return spriteUV;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Calculate facing direction in world space
                float3 world_normal = UnityObjectToWorldNormal(v.normal);
                float3 view_dir = normalize(WorldSpaceViewDir(v.vertex));
                o.facing = dot(world_normal, view_dir);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 sprite_uv;

                if (i.facing > _FlipThreshold) {
                    sprite_uv = IndexToUV(_BackIndex, i.uv);
                } else {
                    float2 flippedUV = float2(1.0 - i.uv.x, i.uv.y);
                    sprite_uv = IndexToUV(_FrontIndex, flippedUV);
                }

                return tex2D(_MainTex, sprite_uv);
            }
            ENDCG
        }
    }
}