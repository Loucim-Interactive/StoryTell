Shader "Hidden/Inmersion/InducedVision" {
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        TEXTURE2D_X(_History);
        float4 _Settings;
        float4 _Tunnel;
        half4 ReadColor(float2 uv) { return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv); }
        half4 Temporal(Varyings input) : SV_Target {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half4 current = ReadColor(input.texcoord);
            if (_Settings.x <= 0) return current;
            return lerp(current, SAMPLE_TEXTURE2D_X(_History, sampler_LinearClamp, input.texcoord), _Settings.x);
        }
        half4 Optics(Varyings input) : SV_Target {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float2 offset = _BlitTexture_TexelSize.xy * _Settings.x;
            half4 color = ReadColor(uv);
            if (_Settings.x > 0) {
                color *= .25;
                color += (ReadColor(uv + float2(offset.x, 0)) + ReadColor(uv - float2(offset.x, 0))
                    + ReadColor(uv + float2(0, offset.y)) + ReadColor(uv - float2(0, offset.y))) * .125;
                color += (ReadColor(uv + offset) + ReadColor(uv - offset)
                    + ReadColor(uv + float2(offset.x, -offset.y)) + ReadColor(uv + float2(-offset.x, offset.y))) * .0625;
            }
            float2 centered = uv - .5;
            centered.x *= _Tunnel.z;
            float edge = smoothstep(_Tunnel.x, _Tunnel.x + max(.01, _Tunnel.y), length(centered));
            color.rgb *= 1 - edge * _Settings.y;
            return color;
        }
        half4 Copy(Varyings input) : SV_Target {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return ReadColor(input.texcoord);
        }
        ENDHLSL
        Pass {
            Name "StepPrinting"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Temporal
            ENDHLSL
        }
        Pass {
            Name "Optics"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Optics
            ENDHLSL
        }
        Pass {
            Name "HistoryCopy"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Copy
            ENDHLSL
        }
    }
}
