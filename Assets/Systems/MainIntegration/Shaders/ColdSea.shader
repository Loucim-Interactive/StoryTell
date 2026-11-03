Shader "StoryTell/ColdSea"
{
 Properties{
  _DeepColor("Deep cold water",Color)=(.025,.085,.105,1)
  _ShallowColor("Shallow water",Color)=(.12,.23,.24,1)
  _FoamColor("Foam",Color)=(.55,.62,.61,1)
 }
 SubShader{
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass{
   Name "Cold sea"
   Tags{"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _LIGHT_COOKIES
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _DeepColor,_ShallowColor,_FoamColor;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;};
   struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float fog:TEXCOORD1;};
   V Vert(A i){V o;float3 p=TransformObjectToWorld(i.positionOS.xyz);o.world=p;o.positionCS=TransformWorldToHClip(p);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    float2 p=i.world.xz;float t=_Time.y;
    float a=dot(p,float2(.8,.6))*.6-t*.75;
    float b=dot(p,float2(-.45,.89))*1.7+t*1.1;
    float c=dot(p,float2(.1,.99))*4.5-t*1.6;
    float2 slope=cos(a)*float2(.8,.6)*.12+cos(b)*float2(-.45,.89)*.07+cos(c)*float2(.1,.99)*.035;
    float3 n=normalize(float3(-slope.x,1,-slope.y));float3 view=normalize(GetWorldSpaceViewDir(i.world));
    float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
    float raw=SampleSceneDepth(uv);float scene=LinearEyeDepth(raw,_ZBufferParams);float surface=-TransformWorldToView(i.world).z;
    float depth=max(0,scene-surface);float fresnel=.04+.96*pow(1-saturate(dot(n,view)),5);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world),i.world,half4(1,1,1,1));
    half3 reflected=GlossyEnvironmentReflection(reflect(-view,n),.16,1);
    half3 color=lerp(_ShallowColor.rgb,_DeepColor.rgb,saturate(depth/7));
    color*=SampleSH(n)+sun.color*saturate(dot(n,sun.direction))*.6;
    color=lerp(color,reflected,fresnel*.8);
    float3 h=normalize(view+sun.direction);color+=sun.color*pow(saturate(dot(n,h)),180)*.75;
    float foam=(1-smoothstep(.08,.65,depth))*smoothstep(-.3,.8,sin(a+b*.35));color=lerp(color,_FoamColor.rgb,foam*.65);
    return half4(MixFog(color,i.fog),lerp(.65,.97,saturate(depth/2)));
   }
   ENDHLSL
  }
 }
}
