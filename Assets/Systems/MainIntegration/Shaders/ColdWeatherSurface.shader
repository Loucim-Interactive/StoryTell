Shader "StoryTell/ColdWeatherSurface"
{
    Properties
    {
        _BaseMap("Original albedo",2D)="white"{}
        _BaseColor("Original tint",Color)=(1,1,1,1)
        [Normal] _BumpMap("Original normal",2D)="bump"{}
        _BumpScale("Normal strength",Float)=1
        _MetallicGlossMap("Original metallic",2D)="white"{}
        _Metallic("Metallic",Range(0,1))=0
        _Smoothness("Dry smoothness",Range(0,1))=.2
        _HasMetallicMap("Metallic texture assigned",Float)=0
        _OcclusionMap("Original occlusion",2D)="white"{}
        _OcclusionStrength("Occlusion strength",Range(0,1))=1
        _ColdTint("Weather tint",Color)=(.85,.92,.9,1)
        _Saturation("Retained saturation",Range(0,1))=.5
        _Wetness("Patchy dampness",Range(0,1))=.2
        _MudAmount("Base grime",Range(0,1))=.15
        _MudHeight("Grime height in metres",Float)=.35
        _DetailStrength("Fine surface grain",Range(0,.2))=0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2
    }
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
    TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
    TEXTURE2D(_MetallicGlossMap);SAMPLER(sampler_MetallicGlossMap);
    TEXTURE2D(_OcclusionMap);SAMPLER(sampler_OcclusionMap);
    CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST,_BaseColor,_ColdTint;
    float _BumpScale,_Metallic,_Smoothness,_HasMetallicMap,_OcclusionStrength;
    float _Saturation,_Wetness,_MudAmount,_MudHeight,_Cull,_DetailStrength;
    CBUFFER_END
    float Hash(float2 p){p=frac(p*float2(123.34,456.21));p+=dot(p,p+45.32);return frac(p.x*p.y);}
    float Noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
    ENDHLSL
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Cull [_Cull]
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForwardOnly"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;};
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 tangent:TEXCOORD2;float2 uv:TEXCOORD3;float fog:TEXCOORD4;float3 localMetres:TEXCOORD5;};
            V Vert(A i)
            {
                V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);VertexNormalInputs n=GetVertexNormalInputs(i.normalOS,i.tangentOS);
                o.positionCS=p.positionCS;o.world=p.positionWS;o.normal=n.normalWS;o.tangent=float4(n.tangentWS,i.tangentOS.w*GetOddNegativeScale());o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);
                o.localMetres=i.positionOS.xyz*float3(length(GetObjectToWorldMatrix()._m00_m10_m20),length(GetObjectToWorldMatrix()._m01_m11_m21),length(GetObjectToWorldMatrix()._m02_m12_m22));return o;
            }
            half4 Frag(V i):SV_Target
            {
                half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                half luminance=dot(base,half3(.2126,.7152,.0722));base=lerp(luminance.xxx,base,_Saturation)*_ColdTint.rgb;
                float patch=Noise(i.localMetres.xz*1.7+i.localMetres.y*.4)*.7+Noise(i.localMetres.xy*7.3)*.3;
                float damp=smoothstep(.28,.76,patch)*_Wetness;
                float grime=(1-smoothstep(0,max(.01,_MudHeight),i.localMetres.y))*(.55+patch*.45)*_MudAmount;
                base=lerp(base*(1-damp*.28),half3(.13,.115,.085),grime);
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                float detailFade=saturate((15-distance(_WorldSpaceCameraPos,i.world))/10);
                normalTS.xy+=(float2(Noise(i.localMetres.xz*35),Noise(i.localMetres.xy*41))-.5)*2*_DetailStrength*detailFade;
                normalTS=normalize(normalTS);
                half3 n=normalize(i.normal),t=normalize(i.tangent.xyz);half3 b=cross(n,t)*i.tangent.w;
                InputData input=(InputData)0;input.positionWS=i.world;input.positionCS=i.positionCS;input.normalWS=NormalizeNormalPerPixel(TransformTangentToWorld(normalTS,half3x3(t,b,n)));input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                input.shadowCoord=ComputeScreenPos(TransformWorldToHClip(i.world));
                #else
                input.shadowCoord=TransformWorldToShadowCoord(i.world);
                #endif
                input.bakedGI=SampleSH(input.normalWS);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);input.shadowMask=half4(1,1,1,1);input.vertexLighting=VertexLighting(i.world,input.normalWS);
                half4 metal=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,i.uv);
                SurfaceData surface=(SurfaceData)0;surface.albedo=base;surface.alpha=1;surface.metallic=lerp(_Metallic,metal.r,_HasMetallicMap);surface.specular=half3(.04,.04,.04);surface.smoothness=saturate(_Smoothness*lerp(1,metal.a,_HasMetallicMap)+damp*.2-grime*.12);surface.normalTS=normalTS;surface.occlusion=lerp(1,SAMPLE_TEXTURE2D(_OcclusionMap,sampler_OcclusionMap,i.uv).g,_OcclusionStrength);
                half4 color=UniversalFragmentPBR(input,surface);color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment EmptyFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection,_LightPosition;
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            float4 ShadowVert(A i):SV_POSITION
            {
                float3 world=TransformObjectToWorld(i.positionOS.xyz),normal=TransformObjectToWorldNormal(i.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 direction=normalize(_LightPosition-world);
                #else
                float3 direction=_LightDirection;
                #endif
                float4 p=TransformWorldToHClip(ApplyShadowBias(world,normal,direction));
                #if UNITY_REVERSED_Z
                p.z=min(p.z,UNITY_NEAR_CLIP_VALUE*p.w);
                #else
                p.z=max(p.z,UNITY_NEAR_CLIP_VALUE*p.w);
                #endif
                return p;
            }
            half4 EmptyFrag():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            float4 DepthVert(float4 positionOS:POSITION):SV_POSITION{return TransformObjectToHClip(positionOS.xyz);}
            half4 DepthFrag():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode"="DepthNormalsOnly"}
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex NormalVert
            #pragma fragment NormalFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;};
            struct V{float4 positionCS:SV_POSITION;float3 normal:TEXCOORD0;float4 tangent:TEXCOORD1;float2 uv:TEXCOORD2;};
            V NormalVert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);VertexNormalInputs n=GetVertexNormalInputs(i.normalOS,i.tangentOS);o.normal=n.normalWS;o.tangent=float4(n.tangentWS,i.tangentOS.w*GetOddNegativeScale());o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
            half4 NormalFrag(V i):SV_Target
            {
                float3 n=normalize(i.normal),t=normalize(i.tangent.xyz),b=cross(n,t)*i.tangent.w;
                float3 normal=NormalizeNormalPerPixel(TransformTangentToWorld(UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale),half3x3(t,b,n)));
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=saturate(PackNormalOctQuadEncode(normal)*.5+.5);return half4(PackFloat2To888(oct),0);
                #else
                return half4(normal,0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
