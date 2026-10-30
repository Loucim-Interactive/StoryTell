Shader "StoryTell/ColdOvercastSky"
{
    Properties
    {
        _Zenith("Zenith",Color)=(.22,.28,.34,1)
        _Horizon("Horizon",Color)=(.53,.59,.62,1)
        _CloudLight("Cloud edges",Color)=(.59,.62,.64,1)
        _CloudShadow("Cloud body",Color)=(.31,.35,.39,1)
        _CloudScale("Cloud scale",Range(1,8))=3.8
        _WindSpeed("Cloud drift",Range(0,.08))=.018
        _WindDirection("Wind direction",Vector)=(.85,0,.35,0)
        _SunDirection("Light opening",Vector)=(-.35,.8,-.3,0)
    }
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
            float4 _Zenith, _Horizon, _CloudLight, _CloudShadow;
            float _CloudScale,_WindSpeed;
            float4 _WindDirection,_SunDirection;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            V vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.direction=v.positionOS.xyz;return o;}
            float hash(float3 p)
            {
                p=frac(p*.1031);p+=dot(p,p.yzx+33.33);
                return frac((p.x+p.y)*p.z);
            }
            float noise(float3 p)
            {
                float3 cell=floor(p),f=frac(p);f=f*f*f*(f*(f*6-15)+10);
                float a=lerp(lerp(hash(cell),hash(cell+float3(1,0,0)),f.x),lerp(hash(cell+float3(0,1,0)),hash(cell+float3(1,1,0)),f.x),f.y);
                float b=lerp(lerp(hash(cell+float3(0,0,1)),hash(cell+float3(1,0,1)),f.x),lerp(hash(cell+float3(0,1,1)),hash(cell+1),f.x),f.y);
                return lerp(a,b,f.z);
            }
            float cloudNoise(float3 p)
            {
                return noise(p)*.53+noise(p*2.03+17.1)*.27+noise(p*4.11+31.7)*.13+noise(p*8.23+47.3)*.07;
            }
            half4 frag(V i):SV_Target
            {
                float3 d=normalize(i.direction);float h=saturate(d.y);
                // Spherical noise avoids stretching near the horizon and seams.
                float3 p=d*_CloudScale+_WindDirection.xyz*_Time.y*_WindSpeed;
                float broad=cloudNoise(p);
                float detail=noise(p*13.7+float3(5.2,_Time.y*.0007,9.1));
                float density=smoothstep(.30,.69,broad*.91+detail*.09);
                float3 baseSky=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,.55));
                float3 cloudColor=lerp(_CloudLight.rgb,_CloudShadow.rgb,saturate(density*1.05));
                float fade=smoothstep(.015,.30,h);
                float lightBreak=pow(saturate(dot(d,normalize(_SunDirection.xyz))),38)*(1-density)*.34;
                return half4(lerp(baseSky,cloudColor,density*fade*.88)+lightBreak*float3(.69,.75,.81),1);
            }
            ENDHLSL
        }
    }
}
