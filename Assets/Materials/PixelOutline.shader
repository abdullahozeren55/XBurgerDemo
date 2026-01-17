Shader "Custom/PixelFontOutline_AnimSupport"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _AtlasWidth ("Atlas Width (Pixel)", Float) = 512.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _AtlasWidth;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                // Text Animator'dan gelen renk ve alpha deðiþimlerini alýyoruz
                OUT.color = IN.color * _Color; 
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float step = 1.0 / _AtlasWidth;

                // Texture Alpha deðerini al
                float mainAlpha = tex2D(_MainTex, IN.texcoord).a;

                // 1. ANA YAZI
                if (mainAlpha > 0.1)
                {
                    // Yazýnýn rengini ve O ANKÝ þeffaflýðýný (animasyon dahil) kullan
                    return fixed4(IN.color.rgb, mainAlpha * IN.color.a);
                }

                // 2. OUTLINE KONTROLÜ
                float alphaSum = 0.0;
                
                // 4 Yön + Çaprazlar (Tam kontrol)
                alphaSum += tex2D(_MainTex, IN.texcoord + float2(step, 0)).a;
                alphaSum += tex2D(_MainTex, IN.texcoord - float2(step, 0)).a;
                alphaSum += tex2D(_MainTex, IN.texcoord + float2(0, step)).a;
                alphaSum += tex2D(_MainTex, IN.texcoord - float2(0, step)).a;
                // Köþeler
                alphaSum += tex2D(_MainTex, IN.texcoord + float2(step, step)).a;
                alphaSum += tex2D(_MainTex, IN.texcoord + float2(step, -step)).a;
                alphaSum += tex2D(_MainTex, IN.texcoord + float2(-step, step)).a;
                alphaSum += tex2D(_MainTex, IN.texcoord + float2(-step, -step)).a;

                if (alphaSum > 0.1)
                {
                    // KRÝTÝK DÜZELTME BURADA:
                    // Outline rengini al AMA Alpha'sýný Text Animator'ýn gönderdiði Alpha ile çarp.
                    // Yazý %50 görünürse, Outline da %50 görünür olur.
                    return fixed4(_OutlineColor.rgb, _OutlineColor.a * IN.color.a);
                }

                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}