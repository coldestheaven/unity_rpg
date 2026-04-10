// ─────────────────────────────────────────────────────────────────────────────
//  Hidden/RPG/ScreenDistortion
//
//  屏幕空间冲击波扰曲 Shader，供 ScreenDistortionFeature 使用。
//
//  渲染原理：
//    在屏幕 UV 空间中计算从中心点向外扩散的冲击波环，对采样坐标施加径向位移。
//    环形扰曲范围由 _DistortionRadius 和 _DistortionRingWidth 控制：
//      • Radius 由 C# 协程每帧递增，模拟冲击波向外扩散
//      • 距环中心越近，扰曲越强（高斯权重）
//      • 最终从临时 RT（_SourceTex）的扰曲后 UV 采样，覆盖摄像机颜色缓冲
//
//  驱动：
//    C# 每帧通过 Material.SetXxx 传入 ScreenDistortionState 中的参数。
//    ScreenDistortionFeature 负责：
//      1. Blit 摄像机颜色到 _SourceTex（临时 RT）
//      2. DrawProcedural 调用本 Shader 采样 _SourceTex 并输出到摄像机颜色缓冲
// ─────────────────────────────────────────────────────────────────────────────
Shader "Hidden/RPG/ScreenDistortion"
{
    Properties
    {
        _SourceTex             ("Source Texture",  2D)            = "white" {}
        _DistortionCenter      ("Center (XY)",     Vector)        = (0.5, 0.5, 0, 0)
        _DistortionStrength    ("Strength",        Range(0, 0.2)) = 0.03
        _DistortionRadius      ("Ring Radius",     Range(0, 1.5)) = 0
        _DistortionRingWidth   ("Ring Width",      Range(0.01, 0.5)) = 0.08
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "RPG_ScreenDistortion"

            ZTest  Always
            ZWrite Off
            Cull   Off
            Blend  Off   // 无混合：直接覆盖摄像机 RT

            HLSLPROGRAM
            #pragma vertex   DistortVert
            #pragma fragment DistortFrag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── 贴图与采样器 ─────────────────────────────────────────────────
            TEXTURE2D(_SourceTex);
            SAMPLER(sampler_SourceTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _DistortionCenter;
                float  _DistortionStrength;
                float  _DistortionRadius;
                float  _DistortionRingWidth;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── 全屏三角形（含 UV） ──────────────────────────────────────────
            Varyings DistortVert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float2 uv;
                uv.x = (vertexID << 1) & 2u;
                uv.y =  vertexID       & 2u;
                o.positionCS = float4(uv * 2.0 - 1.0, 0, 1);

                // 将裁剪空间 UV 转换为纹理采样 UV（DX 平台 Y 轴翻转）
                o.texcoord = float2(uv.x, 1.0 - uv.y);
                return o;
            }

            // ── 冲击波环扰曲 ─────────────────────────────────────────────────
            half4 DistortFrag(Varyings i) : SV_Target
            {
                float2 uv     = i.texcoord;
                float2 center = _DistortionCenter.xy;

                // 屏幕到中心的向量（考虑宽高比）
                float2 offset = uv - center;

                // 到中心的距离（归一化，不考虑宽高比，以 UV 空间计算）
                float  dist   = length(offset);

                // ── 冲击波环权重（在 Radius ± RingWidth/2 范围内为峰值）──────
                // 使用平滑阶跃函数构造梯形权重，使环形边缘柔和
                float  ringCenter = _DistortionRadius;
                float  halfWidth  = _DistortionRingWidth * 0.5;
                float  weight = smoothstep(ringCenter - halfWidth, ringCenter, dist)
                              * smoothstep(ringCenter + halfWidth, ringCenter, dist);

                // 权重归一化（避免 _DistortionRingWidth 影响峰值幅度）
                // 在 dist == ringCenter 时 weight = smoothstep(0,halfWidth,halfWidth)
                //   = 1；此处不需要额外归一化

                // ── UV 位移 ──────────────────────────────────────────────────
                // 向外方向：normalize(offset)；在中心附近（dist≈0）取屏幕中心方向
                float2 dir        = dist > 0.001 ? offset / dist : float2(0, 1);
                float2 distortedUV = saturate(uv + dir * weight * _DistortionStrength);

                return SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, distortedUV);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
