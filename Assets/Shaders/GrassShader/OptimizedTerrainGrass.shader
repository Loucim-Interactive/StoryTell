Shader "StoryTell/Optimized Terrain Grass"
{
    Properties
    {
        [MainTexture] _MainTex ("Grass Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1,1,1,1)
        _BottomColor ("Bottom Tint", Color) = (0.15,0.2,0.1,1)
        _TerrainColorTex ("Terrain Color", 2D) = "white" {}
        _TerrainSize ("Terrain Size", Float) = 1500
        _TerrainOffset ("Terrain Offset", Float) = 0
        _TerrainBlend ("Terrain Blend", Range(0,1)) = 0.72
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _WindSpeed ("Wind Speed", Float) = 1.1
        _WindStrength ("Wind Strength", Float) = 0.08
        _FadeStart ("Fade Start", Float) = 100
        _FadeEnd ("Fade End", Float) = 170
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_TerrainColorTex); SAMPLER(sampler_TerrainColorTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _BottomColor;
                float _TerrainSize;
                float _TerrainOffset;
                half _TerrainBlend;
                half _Cutoff;
                float _WindSpeed;
                float _WindStrength;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float bladeHeight = saturate(input.uv.y);
                float gust = sin(positionWS.x * 0.071 + positionWS.z * 0.053 + _Time.y * _WindSpeed);
                positionWS.xz += gust * _WindStrength * bladeHeight * bladeHeight;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half Hash12(float2 value)
            {
                float3 p3 = frac(value.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 grass = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(grass.a - _Cutoff);

                float distanceToCamera = distance(input.positionWS, _WorldSpaceCameraPos);
                half fade = saturate((_FadeEnd - distanceToCamera) / max(1.0, _FadeEnd - _FadeStart));
                clip(fade - Hash12(floor(input.positionWS.xz * 5.0)));

                float2 terrainUV = input.positionWS.xz / max(1.0, _TerrainSize) + _TerrainOffset;
                half3 terrainColor = SAMPLE_TEXTURE2D(_TerrainColorTex, sampler_TerrainColorTex, terrainUV).rgb;
                half heightBlend = saturate(1.0h - input.uv.y);
                half3 tintedGrass = grass.rgb * lerp(_BottomColor.rgb, _Color.rgb, input.uv.y);
                half3 albedo = lerp(tintedGrass, terrainColor * grass.rgb, heightBlend * _TerrainBlend);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half lighting = 0.42h + 0.58h * mainLight.shadowAttenuation;
                half3 color = albedo * mainLight.color * lighting;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
