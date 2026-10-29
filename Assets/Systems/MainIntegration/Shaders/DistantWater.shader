Shader "StoryTell/DistantWater"
{
    Properties { _Color("Water",Color)=(.12,.21,.25,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float fog:TEXCOORD1; };
            V vert(A v){V o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
            half4 frag(V i):SV_Target{float ripple=sin(i.world.x*.16+_Time.y*.35)*sin(i.world.z*.24-_Time.y*.22);return half4(MixFog(_Color.rgb+ripple*.025,i.fog),1);}
            ENDHLSL
        }
    }
}
