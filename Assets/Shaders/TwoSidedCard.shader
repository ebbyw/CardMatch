// Hand-written replacement for TwoSidedCard.shadergraph.
//
// A two-sided sprite-sheet card for UGUI (UnityEngine.UI.Image on a Canvas).
// The card art lives in one 14x4 atlas (cardsLarge_tilemap_packed, 64px cells).
// The quad shows _FrontFrameNo on its front face and _BackFrameNo on its back
// face, so a 180-degree Y flip reveals the other card. The back face's UV is
// mirrored vertically so its art reads upright once the quad is flipped.
//
// Frame->UV math matches Unity's Flipbook node with Width=14, Height=4,
// Invert=(0,1); the back branch matches a TilingAndOffset of (1,-1)/(0,1).
// Card.cs drives it via material.SetFloat("_FrontFrameNo"/"_BackFrameNo", ...).
Shader "CardMatch/TwoSidedCard" {
  Properties {
    [PerRendererData] _MainTex ("Sprite (unused, for UGUI)", 2D) = "white" {}
    _Spritesheet ("Card Atlas", 2D) = "white" {}
    _FrontFrameNo ("Front Frame", Float) = 27
    _BackFrameNo ("Back Frame", Float) = 0
    _Color ("Tint", Color) = (1,1,1,1)

    // Standard UGUI stencil/mask plumbing so the shader behaves under a Mask.
    _StencilComp ("Stencil Comparison", Float) = 8
    _Stencil ("Stencil ID", Float) = 0
    _StencilOp ("Stencil Operation", Float) = 0
    _StencilWriteMask ("Stencil Write Mask", Float) = 255
    _StencilReadMask ("Stencil Read Mask", Float) = 255
    _ColorMask ("Color Mask", Float) = 15
  }

  SubShader {
    Tags {
      "Queue" = "Transparent"
      "IgnoreProjector" = "True"
      "RenderType" = "Transparent"
      "PreviewType" = "Plane"
      "CanUseSpriteAtlas" = "True"
    }

    Stencil {
      Ref [_Stencil]
      Comp [_StencilComp]
      Pass [_StencilOp]
      ReadMask [_StencilReadMask]
      WriteMask [_StencilWriteMask]
    }

    Cull Off            // two-sided: we need the back face
    Lighting Off
    ZWrite Off
    ZTest [unity_GUIZTestMode]
    Blend SrcAlpha OneMinusSrcAlpha
    ColorMask [_ColorMask]

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      struct appdata {
        float4 vertex : POSITION;
        float4 color  : COLOR;
        float2 uv     : TEXCOORD0;
      };

      struct v2f {
        float4 vertex : SV_POSITION;
        fixed4 color  : COLOR;
        float2 uv     : TEXCOORD0;
      };

      sampler2D _Spritesheet;
      fixed4 _Color;
      float _FrontFrameNo;
      float _BackFrameNo;

      // Atlas layout: 14 columns x 4 rows of 64px cells.
      static const float ATLAS_COLS = 14.0;
      static const float ATLAS_ROWS = 4.0;

      v2f vert (appdata v) {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        o.color = v.color * _Color;
        return o;
      }

      // Unity Flipbook node, Width=14 Height=4 Invert=(0,1).
      float2 Flipbook (float2 uv, float tile) {
        tile = fmod(tile, ATLAS_COLS * ATLAS_ROWS);
        float2 tileCount = float2(1.0, 1.0) / float2(ATLAS_COLS, ATLAS_ROWS);
        float row = floor(tile * tileCount.x);
        float col = tile - ATLAS_COLS * row;
        float tileX = col;                          // Invert.x = 0
        float tileY = abs(ATLAS_ROWS - (row + 1.0)); // Invert.y = 1
        return (uv + float2(tileX, tileY)) * tileCount;
      }

      fixed4 frag (v2f i, float facing : VFACE) : SV_Target {
        float2 atlasUv;
        if (facing > 0) {
          // Front face: show the front frame directly.
          atlasUv = Flipbook(i.uv, _FrontFrameNo);
        }
        else {
          // Back face: show the back frame, V-mirrored (TilingAndOffset 1,-1 / 0,1).
          atlasUv = Flipbook(i.uv, _BackFrameNo);
          atlasUv.y = 1.0 - atlasUv.y;
        }

        fixed4 col = tex2D(_Spritesheet, atlasUv) * i.color;
        return col;
      }
      ENDCG
    }
  }
}
