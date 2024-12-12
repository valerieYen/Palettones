Shader "Custom/AnimatedWaves"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _WaveCount ("Number of Waves", Range(1, 10)) = 4
        _WaveThickness ("Wave Thickness", Range(0.1, 0.9)) = 0.4
        _WaveFrequency ("Wave Frequency", Range(0, 20)) = 2
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1
        _HorizontalOffset ("Horizontal Offset", Range(0, 2)) = 1
        _ForegroundColor ("Foreground Color", Color) = (0,0,0,1)
        _BackgroundColor ("Background Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float _WaveCount;
            float _WaveThickness;
            float _WaveFrequency;
            float _WaveSpeed;
            float _HorizontalOffset;
            float4 _ForegroundColor;
            float4 _BackgroundColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y * _WaveSpeed;
                
                // Scale UV to create proper wave count
                float2 uv = i.uv;
                uv.y *= _WaveCount;
                
                // Get the wave row index
                int waveIndex = floor(uv.y);
                float waveY = frac(uv.y);
                
                // Calculate horizontal offset for each row
                float xOffset = waveIndex * _HorizontalOffset;
                
                // Create the wave
                float wave = sin((uv.x + xOffset + time) * _WaveFrequency) * 0.5 + 0.5;
                
                // Create thick bands by comparing wave position with y position
                float thickness = _WaveThickness;
                float band = abs(wave - waveY) < thickness;
                
                // Lerp between background and foreground colors
                float4 color = lerp(_BackgroundColor, _ForegroundColor, band);
                color *= i.color; // Multiply by vertex color for UI tinting
                
                return color;
            }
            ENDCG
        }
    }
}