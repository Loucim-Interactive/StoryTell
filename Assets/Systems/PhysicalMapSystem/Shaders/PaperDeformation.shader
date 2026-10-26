Shader "StoryTell/PhysicalMap/PaperDeformation"
{
    Properties
    {
        _BaseMap ("Map Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BendStrength ("Bend Strength", Range(-1, 1)) = 0.12
        _WindStrength ("Wind Strength", Range(0, 2)) = 0.35
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0)
        _NoiseScale ("Noise Scale", Float) = 8
        _FoldAmount ("Fold Amount", Range(0, 1)) = 0.08
        _PaperMotionSpeed ("Paper Motion Speed", Float) = 1.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            // A physical paper map must remain visible from either side while it
            // follows the camera, folds, or reacts to physics.
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _BendStrength;
                float _WindStrength;
                float4 _WindDirection;
                float _NoiseScale;
                float _FoldAmount;
                float _PaperMotionSpeed;
                int _BendPointCount;
                float4 _BendPoints[4];
                float4 _BendDirections[4];
            CBUFFER_END

            float Wave(float2 value)
            {
                return sin(value.x * 12.9898 + value.y * 78.233);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 position = input.positionOS.xyz;
                float2 centeredUv = input.uv - 0.5;
                float timeValue = _Time.y * _PaperMotionSpeed;

                float baseCurve = centeredUv.x * centeredUv.x - 0.08;
                position.y += baseCurve * _BendStrength;

                float windAxis = dot(centeredUv, normalize(_WindDirection.xz + float2(0.001, 0.001)));
                float windWave = sin((windAxis + timeValue) * _NoiseScale);
                float smallNoise = Wave(centeredUv * _NoiseScale + timeValue);
                position.y += (windWave * 0.018 + smallNoise * 0.006) * _WindStrength;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    if (i >= _BendPointCount)
                        break;

                    float2 bendPosition = _BendPoints[i].xy;
                    float intensity = _BendPoints[i].z;
                    float radius = max(_BendPoints[i].w, 0.001);
                    float2 direction = normalize(_BendDirections[i].xy + float2(0.001, 0.001));
                    float2 toVertex = centeredUv - bendPosition;
                    float distanceMask = saturate(1.0 - length(toVertex) / radius);
                    float side = dot(toVertex, direction);

                    position.y += sin(side * 12.0) * distanceMask * intensity * _FoldAmount;
                }

                output.positionHCS = TransformObjectToHClip(position);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                return color;
            }
            ENDHLSL
        }
    }
}
