Shader "StoryTell/ColdOvercastSky"
{
    Properties { _Zenith("Zenith",Color)=(.22,.28,.34,1) _Horizon("Horizon",Color)=(.53,.59,.62,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Zenith, _Horizon;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            V vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.direction=v.positionOS.xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            half4 frag(V i):SV_Target
            {
                float3 d=normalize(i.direction);float h=saturate(d.y);
                float2 p=d.xz/max(.16,h)*1.7+_Time.y*float2(.002,.001);
                float n=noise(p)*.55+noise(p*2.07)*.28+noise(p*4.13)*.17;
                float cloud=smoothstep(.22,.74,n)*smoothstep(0,.2,h);
                float3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,.55));
                color=lerp(color,float3(.43,.47,.49),cloud*.72);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
