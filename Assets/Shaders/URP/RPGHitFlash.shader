// ─────────────────────────────────────────────────────────────────────────────
//  Hidden/RPG/HitFlash
//
//  受击全屏闪光 Shader，供 HitFlashFeature 使用。
//
//  渲染方式：
//    DrawProcedural（3 顶点全屏三角形），输出可配置颜色 + 当前帧强度作为透明度。
//    混合模式 SrcAlpha OneMinusSrcAlpha：
//      画面 = FlashColor * Intensity + Original * (1 - Intensity)
//    Intensity = 0 → 画面不变；Intensity = 1 → 完全覆盖为 FlashColor。
//
//  驱动：
//    C# 端每帧通过 Material.SetColor(_FlashColor) + Material.SetFloat(_FlashIntensity)
//    更新参数（来自 HitFlashState 静态桥接类）。
//
//  注意：与 RPGFullscreenFade.shader 架构相同，区别仅在参数名和用途：
//    HitFlash  → 快速脉冲（0.2~0.4 秒），适合受击/治疗反馈
//    Fade      → 慢速渐出（0.5~2 秒），适合场景切换
// ─────────────────────────────────────────────────────────────────────────────
Shader "Hidden/RPG/HitFlash"
{
    Properties
    {
        _FlashColor     ("Flash Color",     Color)         = (1, 1, 1, 1)
        _FlashIntensity ("Flash Intensity", Range(0, 1))   = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "RPG_HitFlash"

            ZTest   Always
            ZWrite  Off
            Cull    Off
            // 画面 = FlashColor * Intensity + Original * (1 - Intensity)
            Blend   SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   HitFlashVert
            #pragma fragment HitFlashFrag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FlashColor;
                float _FlashIntensity;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // 全屏三角形（无需网格，从 SV_VertexID 构造裁剪空间坐标）
            Varyings HitFlashVert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float2 uv;
                uv.x = (vertexID << 1) & 2u;
                uv.y =  vertexID       & 2u;
                o.positionCS = float4(uv * 2.0 - 1.0, 0, 1);
                return o;
            }

            half4 HitFlashFrag(Varyings i) : SV_Target
            {
                // Blend 模式处理与场景的混合，此处只需输出目标颜色 + 强度作为 Alpha
                return half4(_FlashColor.rgb, (half)_FlashIntensity);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
