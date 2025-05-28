Shader "Hidden/EdgeDetectionOutline"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Float) = 1.0
        _DepthSensitivity ("Depth Sensitivity", Range(0, 1)) = 0.5
        _NormalSensitivity ("Normal Sensitivity", Range(0, 1)) = 0.4
        _ColorSensitivity ("Color Sensitivity", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        ZWrite Off
        Cull Off
        ZTest Always
        
        Pass
        {
            Name "EdgeDetectionOutline"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // Outline object textures
            TEXTURE2D(_OutlineColorTexture);
            SAMPLER(sampler_OutlineColorTexture);
            TEXTURE2D(_OutlineDepthTexture);
            SAMPLER(sampler_OutlineDepthTexture);
            TEXTURE2D(_OutlineNormalTexture);
            SAMPLER(sampler_OutlineNormalTexture);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _OutlineColor;
                float _OutlineThickness;
                float _DepthSensitivity;
                float _NormalSensitivity;
                float _ColorSensitivity;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            
            // Sobel edge detection kernels
            static const float3x3 sobelX = float3x3(
                -1, 0, 1,
                -2, 0, 2,
                -1, 0, 1
            );
            
            static const float3x3 sobelY = float3x3(
                -1, -2, -1,
                 0,  0,  0,
                 1,  2,  1
            );
            
            // Sample depth with edge detection from outline objects only
            float SampleDepthEdge(float2 uv)
            {
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineThickness;
                
                float depth[9];
                depth[0] = SAMPLE_TEXTURE2D(_OutlineDepthTexture, sampler_OutlineDepthTexture, uv + float2(-texelSize.x, -texelSize.y)).r;
                depth[1] = SAMPLE_TEXTURE2D(_OutlineDepthTexture, sampler_OutlineDepthTexture, uv + float2(0, -texelSize.y)).r;
                depth[2] = SAMPLE_TEXTURE2D(_OutlineDepthTexture, sampler_OutlineDepthTexture, uv + float2(texelSize.x, -texelSize.y)).r;
                depth[3] = SAMPLE_TEXTURE2D(_OutlineDepthTexture, sampler_OutlineDepthTexture, uv + float2(-texelSize.x, 0)).r;
                depth[4] = SAMPLE_TEXTURE2D(_OutlineDepthTexture, sampler_OutlineDepthTexture, uv).r;
                depth[5] = SAMPLE_TEXTURE2D(_OutlineDepthTexture, sampler_OutlineDepthTexture, uv + float2(texelSize.x, 0)).r;
                depth[6] = SAMPLE_TEXTURE2D(_OutlineDepthTexture, sampler_OutlineDepthTexture, uv + float2(-texelSize.x, texelSize.y)).r;
                depth[7] = SAMPLE_TEXTURE2D(_OutlineDepthTexture, sampler_OutlineDepthTexture, uv + float2(0, texelSize.y)).r;
                depth[8] = SAMPLE_TEXTURE2D(_OutlineDepthTexture, sampler_OutlineDepthTexture, uv + float2(texelSize.x, texelSize.y)).r;
                
                // Apply Sobel operator
                float edgeX = 0;
                float edgeY = 0;
                
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = i * 3 + j;
                        edgeX += depth[index] * sobelX[i][j];
                        edgeY += depth[index] * sobelY[i][j];
                    }
                }
                
                return sqrt(edgeX * edgeX + edgeY * edgeY);
            }
            
            // Sample normals with edge detection from outline objects only
            float SampleNormalEdge(float2 uv)
            {
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineThickness;
                
                float3 normal[9];
                normal[0] = SAMPLE_TEXTURE2D(_OutlineNormalTexture, sampler_OutlineNormalTexture, uv + float2(-texelSize.x, -texelSize.y)).rgb;
                normal[1] = SAMPLE_TEXTURE2D(_OutlineNormalTexture, sampler_OutlineNormalTexture, uv + float2(0, -texelSize.y)).rgb;
                normal[2] = SAMPLE_TEXTURE2D(_OutlineNormalTexture, sampler_OutlineNormalTexture, uv + float2(texelSize.x, -texelSize.y)).rgb;
                normal[3] = SAMPLE_TEXTURE2D(_OutlineNormalTexture, sampler_OutlineNormalTexture, uv + float2(-texelSize.x, 0)).rgb;
                normal[4] = SAMPLE_TEXTURE2D(_OutlineNormalTexture, sampler_OutlineNormalTexture, uv).rgb;
                normal[5] = SAMPLE_TEXTURE2D(_OutlineNormalTexture, sampler_OutlineNormalTexture, uv + float2(texelSize.x, 0)).rgb;
                normal[6] = SAMPLE_TEXTURE2D(_OutlineNormalTexture, sampler_OutlineNormalTexture, uv + float2(-texelSize.x, texelSize.y)).rgb;
                normal[7] = SAMPLE_TEXTURE2D(_OutlineNormalTexture, sampler_OutlineNormalTexture, uv + float2(0, texelSize.y)).rgb;
                normal[8] = SAMPLE_TEXTURE2D(_OutlineNormalTexture, sampler_OutlineNormalTexture, uv + float2(texelSize.x, texelSize.y)).rgb;
                
                // Apply Sobel operator to each component
                float3 edgeX = float3(0, 0, 0);
                float3 edgeY = float3(0, 0, 0);
                
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = i * 3 + j;
                        edgeX += normal[index] * sobelX[i][j];
                        edgeY += normal[index] * sobelY[i][j];
                    }
                }
                
                float3 edge = sqrt(edgeX * edgeX + edgeY * edgeY);
                return length(edge);
            }
            
            // Sample color with edge detection from outline objects only
            float SampleColorEdge(float2 uv)
            {
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineThickness;
                
                float3 color[9];
                color[0] = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv + float2(-texelSize.x, -texelSize.y)).rgb;
                color[1] = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv + float2(0, -texelSize.y)).rgb;
                color[2] = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv + float2(texelSize.x, -texelSize.y)).rgb;
                color[3] = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv + float2(-texelSize.x, 0)).rgb;
                color[4] = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv).rgb;
                color[5] = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv + float2(texelSize.x, 0)).rgb;
                color[6] = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv + float2(-texelSize.x, texelSize.y)).rgb;
                color[7] = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv + float2(0, texelSize.y)).rgb;
                color[8] = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv + float2(texelSize.x, texelSize.y)).rgb;
                
                // Convert to luminance and apply Sobel operator
                float lum[9];
                for (int k = 0; k < 9; k++)
                {
                    lum[k] = dot(color[k], float3(0.299, 0.587, 0.114));
                }
                
                float edgeX = 0;
                float edgeY = 0;
                
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = i * 3 + j;
                        edgeX += lum[index] * sobelX[i][j];
                        edgeY += lum[index] * sobelY[i][j];
                    }
                }
                
                return sqrt(edgeX * edgeX + edgeY * edgeY);
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                
                // Sample original color from main scene
                half4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // Check if there's any outline object at this pixel
                float4 outlineObjectColor = SAMPLE_TEXTURE2D(_OutlineColorTexture, sampler_OutlineColorTexture, uv);
                float outlineObjectAlpha = outlineObjectColor.a;
                
                // Only apply edge detection where outline objects exist
                if (outlineObjectAlpha > 0.01)
                {
                    // Calculate edge detection for each buffer
                    float depthEdge = SampleDepthEdge(uv);
                    float normalEdge = SampleNormalEdge(uv);
                    float colorEdge = SampleColorEdge(uv);
                    
                    // Apply sensitivity thresholds
                    depthEdge = depthEdge > _DepthSensitivity ? 1.0 : 0.0;
                    normalEdge = normalEdge > _NormalSensitivity ? 1.0 : 0.0;
                    colorEdge = colorEdge > _ColorSensitivity ? 1.0 : 0.0;
                    
                    // Combine edges using max operator
                    float edge = max(depthEdge, max(normalEdge, colorEdge));
                    
                    // Blend outline with original color
                    return lerp(originalColor, _OutlineColor, edge * _OutlineColor.a);
                }
                
                // No outline object here, return original color
                return originalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
} 