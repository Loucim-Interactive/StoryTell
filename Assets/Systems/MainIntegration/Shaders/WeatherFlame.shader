Shader "StoryTell/WeatherFlame"
{
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha One ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;return o;}
            half4 Frag(V i):SV_Target
            {
                float y=saturate(i.uv.y);
                float center=.5+sin(y*13+_Time.y*9)*.06*y;
                float width=lerp(.48,.025,y);
                float shape=saturate(1-abs(i.uv.x-center)/width);
                float alpha=shape*shape*saturate(y*8)*saturate((1-y)*5)*i.color.a;
                half3 color=lerp(half3(2.2,1.4,.24),half3(1.8,.15,.012),y);
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
