Shader "StoryTell/SoftEmberParticle"
{
    Properties { _BaseColor("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct V{float4 positionCS:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float fog:TEXCOORD1;};
            V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color*_BaseColor;o.uv=i.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
            half4 frag(V i):SV_Target{float a=pow(saturate(1-length(i.uv*2-1)),2);return half4(MixFogColor(i.color.rgb,half3(0,0,0),i.fog),a*i.color.a);}
            ENDHLSL
        }
    }
}
