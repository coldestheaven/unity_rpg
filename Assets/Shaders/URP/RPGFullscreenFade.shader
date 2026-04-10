// ────────────────────────────────────────────────────────────────────────────
//  Hidden/RPG/FullscreenFade
//
//  全屏淡化叠加 Shader，供 FullscreenFadeFeature 使用。
//
//  渲染方式：
//    DrawProcedural (3顶点全屏三角形)，输出固定颜色 + 可变透明度。
//    混合模式 SrcAlpha OneMinusSrcAlpha 使其与相机 RT 内容自然叠加。
//    Alpha = 0 → 画面不变；Alpha = 1 → 完全覆盖为 _FadeColor。
//
//  驱动：
//    C# 端每帧通过 Material.SetColor(_FadeColor) + Material.SetFloat(_FadeAlpha)
//    更新参数（来自 FullscreenFadeState 静态桥接类）。
// ────────────────────────────────────────────────────────────────────────────
Shader "Hidden/RPG/FullscreenFade"
{
    Properties
    {
        _FadeColor ("Fade Color", Color)          = (0, 0, 0, 1)
        _FadeAlpha ("Fade Alpha", Range(0, 1))    = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "RPG_FullscreenFade"

            ZTest   Always
            ZWrite  Off
            Cull    Off
            // 与相机 RT 内容叠加：画面 = FadeColor*Alpha + Original*(1-Alpha)
            Blend   SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   FadeVert
            #pragma fragment FadeFrag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FadeColor;
                float _FadeAlpha;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // 全屏三角形顶点着色器（无需 UV，片元只输出固定颜色）
            Varyings FadeVert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float2 uv;
                uv.x = (vertexID << 1) & 2u;
                uv.y =  vertexID       & 2u;
                o.positionCS = float4(uv * 2.0 - 1.0, 0, 1);
                return o;
            }

            half4 FadeFrag(Varyings i) : SV_Target
            {
                // 输出纯色 + 当前帧透明度
                // Blend 模式将其与相机 RT 叠加，实现渐进遮挡效果
                return half4(_FadeColor.rgb, _FadeAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
