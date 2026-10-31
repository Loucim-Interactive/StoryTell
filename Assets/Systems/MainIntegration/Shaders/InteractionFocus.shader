Shader "StoryTell/InteractionFocus"
{
    Properties{_Color("Quiet warm highlight",Color)=(1,.82,.48,.18)}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent"}
        Blend SrcAlpha One ZWrite Off ZTest LEqual Cull Back Offset -1,-1
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.world=TransformObjectToWorld(i.positionOS.xyz);o.normal=TransformObjectToWorldNormal(i.normalOS);return o;}
            half4 Frag(V i):SV_Target{float edge=pow(1-saturate(dot(normalize(i.normal),GetWorldSpaceNormalizeViewDir(i.world))),2);return half4(_Color.rgb,_Color.a*(.28+.72*edge));}
            ENDHLSL
        }
    }
}
