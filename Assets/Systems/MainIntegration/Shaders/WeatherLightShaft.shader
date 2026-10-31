Shader "StoryTell/WeatherLightShaft"
{
    Properties{_Color("Light",Color)=(.65,.74,.85,.035)}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent"}
        Blend SrcAlpha One ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.world=TransformObjectToWorld(i.positionOS.xyz);o.uv=i.uv;return o;}
            half4 Frag(V i):SV_Target
            {
                float depth=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS)),_ZBufferParams);
                float own=-TransformWorldToView(i.world).z;
                float soft=saturate((depth-own)*.4);
                float bands=.8+.2*sin(i.uv.x*41+_Time.y*.14+i.uv.y*8);
                float edge=pow(saturate(sin(i.uv.x*3.14159265)),3)*sin(saturate(i.uv.y)*3.14159265);
                float nearFade=saturate(distance(_WorldSpaceCameraPos,i.world)/5);
                float opening=1;
                #if defined(_LIGHT_COOKIES)
                opening=smoothstep(.5,.9,SampleMainLightCookie(i.world).r);
                #endif
                return half4(_Color.rgb,_Color.a*edge*bands*soft*nearFade*opening);
            }
            ENDHLSL
        }
    }
}
