Shader "Custom/PixelFontOutline_Glitch_IndependentColors"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Main Color (Vertex)", Color) = (1,1,1,1)
        
        // --- UIGlowController ---
        [HDR] _FaceColor ("Face Color (Glow)", Color) = (1,1,1,1)
        // ------------------------

        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _AtlasWidth ("Atlas Width (Pixel)", Float) = 512.0
        [Range(0, 5)] _OutlineThickness ("Outline Thickness", Float) = 1.0

        // --- GLITCH & CHROMATIC AYARLARI ---
        [Header(Glitch Settings)]
        [Range(0, 1)] _GlitchStrength ("Glitch Strength", Float) = 0.0
        [Range(0, 50)] _GlitchFrequency ("Glitch Speed", Float) = 10.0
        [Range(1, 100)] _GlitchVertical ("Vertical Noise Density", Float) = 20.0
        [Range(0, 0.1)] _GlitchColorSplit ("Color Split Amount", Float) = 0.01 

        // --- MASK FIX STANDARTLARI ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
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
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

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
            fixed4 _FaceColor; 

            float _AtlasWidth;
            float _OutlineThickness;
            float4 _ClipRect;

            float _GlitchStrength;
            float _GlitchFrequency;
            float _GlitchVertical;
            float _GlitchColorSplit;

            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                // Vertex Color'ý burada alýyoruz (Text Animator rengi buraya gönderir)
                OUT.color = IN.color * _Color; 
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. GLITCH HESAPLAMALARI
                float2 glitchUV = IN.texcoord;
                float splitAmount = 0.0;

                if (_GlitchStrength > 0.0)
                {
                    float timeStep = floor(_Time.y * _GlitchFrequency);
                    float noiseVal = random(float2(timeStep, floor(IN.texcoord.y * _GlitchVertical)));

                    if (noiseVal > (1.0 - _GlitchStrength * 0.5)) 
                    {
                        float shift = (random(float2(noiseVal, timeStep)) - 0.5) * 2.0; 
                        glitchUV.x += shift * _GlitchStrength * 0.1;
                        splitAmount = shift * _GlitchColorSplit * _GlitchStrength;
                    }
                }
                
                // 2. TEXTURE OKUMALARI
                // R ve B kanallarýný merkezden ayýrýyoruz (Offset)
                float2 uvR = glitchUV + float2(splitAmount, 0); 
                float2 uvG = glitchUV; 
                float2 uvB = glitchUV - float2(splitAmount, 0); 
                
                float aR = tex2D(_MainTex, uvR).a;
                float aG = tex2D(_MainTex, uvG).a;
                float aB = tex2D(_MainTex, uvB).a;

                fixed4 finalColor = fixed4(0,0,0,0);
                bool hasColor = false;

                // Maksimum alpha alaný (Hem glitch hem yazý görünsün diye)
                float maxAlpha = max(aR, max(aG, aB));

                // 3. RENK KARIÞTIRMA (BURASI DEÐÝÞTÝ)
                if (maxAlpha > 0.1)
                {
                    // A. Baz Metin Rengi:
                    // Sadece MERKEZ (aG) piksellerini kullanýcýnýn istediði renge boyuyoruz.
                    fixed4 baseText = IN.color * _FaceColor;
                    
                    // Baþlangýç rengimiz metnin kendi rengi (Sadece yeþil kanalý hizasýnda var)
                    float3 finalRGB = baseText.rgb * aG;

                    // B. Glitch Efektleri (Additive / Ekleme):
                    // Eðer glitch varsa ve renkler ayrýþýyorsa, kenarlara SAF KIRMIZI ve MAVÝ ekliyoruz.
                    // "max(0, aR - aG)" formülü þu iþe yarar:
                    // Sadece merkezin DIÞINDA kalan kýrmýzýlarý al. Böylece metnin ortasý boyanmaz.
                    
                    if (_GlitchStrength > 0.0)
                    {
                        // Kýrmýzý Hayalet (Texture'ýn kaymýþ hali - Orijinal hali)
                        float redGhost = max(0.0, aR - aG);
                        // Mavi Hayalet
                        float blueGhost = max(0.0, aB - aG);

                        // Bu hayaletleri final renge EKLE (Add). 
                        // Metin siyah olsa bile (0,0,0), bu deðerler (1,0,0) olduðu için görünür.
                        finalRGB.r += redGhost; 
                        finalRGB.b += blueGhost;
                        
                        // Ýstersen yeþil kanalýyla da oynayabilirsin ama genelde R ve B yeterlidir.
                    }

                    finalColor.rgb = finalRGB;
                    
                    // Alpha ayarý: Text Animator "Fade Out" yaptýðýnda (IN.color.a düþtüðünde)
                    // glitch efektleri de þeffaflaþmalý.
                    finalColor.a = baseText.a * maxAlpha;
                    
                    hasColor = true;
                }

                // 4. OUTLINE KONTROLÜ
                // Outline hala sadece merkez (Green) koordinatýna göre çalýþýr.
                else if (_OutlineThickness > 0.0)
                {
                    // ... (Burada deðiþiklik yok, ayný optimizasyon)
                    float baseStep = 1.0 / _AtlasWidth;
                    float offset = baseStep * max(0.0, _OutlineThickness);
                    float alphaSum = 0.0;
                    
                    alphaSum += tex2D(_MainTex, glitchUV + float2(offset, 0)).a;
                    alphaSum += tex2D(_MainTex, glitchUV - float2(offset, 0)).a;
                    alphaSum += tex2D(_MainTex, glitchUV + float2(0, offset)).a;
                    alphaSum += tex2D(_MainTex, glitchUV - float2(0, offset)).a;
                    
                    alphaSum += tex2D(_MainTex, glitchUV + float2(offset, offset)).a;
                    alphaSum += tex2D(_MainTex, glitchUV + float2(offset, -offset)).a;
                    alphaSum += tex2D(_MainTex, glitchUV + float2(-offset, offset)).a;
                    alphaSum += tex2D(_MainTex, glitchUV + float2(-offset, -offset)).a;

                    if (alphaSum > 0.1)
                    {
                        finalColor = fixed4(_OutlineColor.rgb, _OutlineColor.a * IN.color.a);
                        hasColor = true;
                    }
                }

                if (hasColor)
                {
                    finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                    return finalColor;
                }

                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}