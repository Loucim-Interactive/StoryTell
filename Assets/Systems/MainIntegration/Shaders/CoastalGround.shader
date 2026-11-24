Shader "StoryTell/CoastalGround"
{
    Properties
    {
        _BaseMap("Ground", 2D) = "white" {}
        _BaseColor("Tint", Color) = (.94,.96,.93,1)
        _HorizonColor("Horizon", Color) = (.48,.55,.59,1)
        _FogDensity("Coastal haze", Float) = .0065
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _HorizonColor;
            float _FogDensity;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1; float2 uv:TEXCOORD2; };
            V Vert(A i)
            {
                V o; o.world=TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(i.normalOS);
                o.uv=TRANSFORM_TEX(i.uv,_BaseMap); return o;
            }
            half4 Frag(V i):SV_Target
            {
                half3 normal=normalize(i.normal);
                Light sun=GetMainLight();
                half3 light=max(SampleSH(normal),half3(.08,.08,.08))+sun.color*saturate(dot(normal,sun.direction));
                half3 ground=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb*light;
                // The visual coast must merge into the sky even in isolated review cameras.
                float depth=length(i.world-GetCameraPositionWS())*_FogDensity;
                return half4(lerp(_HorizonColor.rgb,ground,exp(-depth*depth)),1);
            }
            ENDHLSL
        }
    }
}
