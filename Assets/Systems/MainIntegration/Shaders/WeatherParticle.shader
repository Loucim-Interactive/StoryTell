Shader "StoryTell/WeatherParticle"
{
    Properties{_Color("Tint",Color)=(.32,.34,.35,.3)}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;return o;}
            half4 Frag(V i):SV_Target{float edge=saturate(1-length(i.uv*2-1));return half4(i.color.rgb*_Color.rgb,i.color.a*_Color.a*edge*edge);}
            ENDHLSL
        }
    }
}
