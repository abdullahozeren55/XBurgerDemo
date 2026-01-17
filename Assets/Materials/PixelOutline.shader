Shader "Custom/PixelFontOutline_AnimSupport_Fixed_Thick"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        
        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _AtlasWidth ("Atlas Width (Pixel)", Float) = 512.0
        // YENÝ EKLENEN KALINLIK AYARI (Slider olarak 0-5 arasý)
        [Range(0, 5)] _OutlineThickness ("Outline Thickness", Float) = 1.0

        // --- MASK FIX STANDARTLARI ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        // -----------------------------
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        // --- STENCIL BLOK (Maskeleme için gerekli) ---
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        // --------------------------------------------

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _AtlasWidth;
            // Yeni kalýnlýk deðiþkeni
            float _OutlineThickness;
            float4 _ClipRect;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                // Text Animator'dan gelen renk ve alpha deðiþimlerini alýyoruz
                OUT.color = IN.color * _Color; 
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1 pikselin UV'deki karþýlýðý
                float baseStep = 1.0 / _AtlasWidth;
                // Kalýnlýk ile çarpýlmýþ nihai ofset deðeri
                float offset = baseStep * max(0.0, _OutlineThickness);

                // Texture Alpha deðerini al
                float mainAlpha = tex2D(_MainTex, IN.texcoord).a;

                fixed4 finalColor = fixed4(0,0,0,0);
                bool hasColor = false;

                // 1. ANA YAZI
                if (mainAlpha > 0.1)
                {
                    finalColor = fixed4(IN.color.rgb, mainAlpha * IN.color.a);
                    hasColor = true;
                }
                // 2. OUTLINE KONTROLÜ (Eðer kalýnlýk 0'dan büyükse)
                else if (_OutlineThickness > 0.0)
                {
                    float alphaSum = 0.0;
                    
                    // 4 Yön + Çaprazlar (Artýk 'offset' deðiþkenini kullanýyor)
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(offset, 0)).a;
                    alphaSum += tex2D(_MainTex, IN.texcoord - float2(offset, 0)).a;
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(0, offset)).a;
                    alphaSum += tex2D(_MainTex, IN.texcoord - float2(0, offset)).a;
                    
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(offset, offset)).a;
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(offset, -offset)).a;
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(-offset, offset)).a;
                    alphaSum += tex2D(_MainTex, IN.texcoord + float2(-offset, -offset)).a;

                    if (alphaSum > 0.1)
                    {
                        // Outline rengini al, Alpha'sýný Text Animator'ýn gönderdiði Alpha ile çarp.
                        finalColor = fixed4(_OutlineColor.rgb, _OutlineColor.a * IN.color.a);
                        hasColor = true;
                    }
                }

                if (hasColor)
                {
                    // --- MASK CHECK --- (RectMask2D ve Normal Mask desteði)
                    finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                    return finalColor;
                }

                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}