Shader "Custom/UIBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 10)) = 3
        _BlurIntensity ("Blur Intensity", Range(0, 1)) = 0.5
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

        GrabPass 
        {
            "_BackgroundTexture"
        }

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
                float4 pos : SV_POSITION;
                float4 grabPos : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _BackgroundTexture;
            float _BlurSize;
            float _BlurIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.color = v.color;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float4 sum = 0;
                float2 grabTexelSize = 1.0 / _ScreenParams.xy;
                
                // 9-tap Gaussian blur
                sum += tex2Dproj(_BackgroundTexture, i.grabPos + float4(-1, -1, 0, 0) * _BlurSize * grabTexelSize.xyxy) * 0.0625;
                sum += tex2Dproj(_BackgroundTexture, i.grabPos + float4(-1,  0, 0, 0) * _BlurSize * grabTexelSize.xyxy) * 0.125;
                sum += tex2Dproj(_BackgroundTexture, i.grabPos + float4(-1,  1, 0, 0) * _BlurSize * grabTexelSize.xyxy) * 0.0625;
                sum += tex2Dproj(_BackgroundTexture, i.grabPos + float4( 0, -1, 0, 0) * _BlurSize * grabTexelSize.xyxy) * 0.125;
                sum += tex2Dproj(_BackgroundTexture, i.grabPos) * 0.25;
                sum += tex2Dproj(_BackgroundTexture, i.grabPos + float4( 0,  1, 0, 0) * _BlurSize * grabTexelSize.xyxy) * 0.125;
                sum += tex2Dproj(_BackgroundTexture, i.grabPos + float4( 1, -1, 0, 0) * _BlurSize * grabTexelSize.xyxy) * 0.0625;
                sum += tex2Dproj(_BackgroundTexture, i.grabPos + float4( 1,  0, 0, 0) * _BlurSize * grabTexelSize.xyxy) * 0.125;
                sum += tex2Dproj(_BackgroundTexture, i.grabPos + float4( 1,  1, 0, 0) * _BlurSize * grabTexelSize.xyxy) * 0.0625;
                
                // Blend between original and blurred based on intensity
                float4 originalColor = tex2Dproj(_BackgroundTexture, i.grabPos);
                float4 finalColor = lerp(originalColor, sum, _BlurIntensity);
                
                return finalColor * i.color;
            }
            ENDCG
        }
    }
}