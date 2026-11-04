Shader "StoryTell/ColdBreathVapor"
{
 Properties { _Color("Vapor tint",Color)=(.76,.82,.84,.22) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Color;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float eye:TEXCOORD1;float fog:TEXCOORD2;};
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
   V Vert(A i){V o;float3 world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(world);o.uv=i.uv;o.color=i.color;o.eye=-TransformWorldToView(world).z;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    float2 p=(i.uv-.5)*2;float edge=saturate(1-dot(p,p));edge=edge*edge;
    float noise=Noise(i.uv*5+float2(_Time.y*.3,-_Time.y*.19))*.7+Noise(i.uv*11-_Time.y*.23)*.3;
    float depth=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS)),_ZBufferParams);
    float soft=saturate((depth-i.eye)/.16)*saturate((i.eye-.08)/.15);
    half alpha=edge*lerp(.15,1,smoothstep(.15,.8,noise))*i.color.a*_Color.a*soft;
    return half4(MixFog(_Color.rgb*i.color.rgb,i.fog),alpha);
   }
   ENDHLSL
  }
 }
}
