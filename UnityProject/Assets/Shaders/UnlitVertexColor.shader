// Break to Escape - unlit vertex-colour shader for URP: punch preview lines, ghost previews and
// code-built particles. Alpha-blended, no lighting.
// NOT TESTED OR UNVERIFIED: written as text, never compiled in Unity.
Shader "BreakToEscape/UnlitVertexColor"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.color = input.color * _BaseColor;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target { return i.color; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
