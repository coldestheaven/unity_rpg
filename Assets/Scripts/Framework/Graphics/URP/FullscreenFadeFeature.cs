// ────────────────────────────────────────────────────────────────────────────
//  FullscreenFadeFeature — URP 全屏淡化 ScriptableRendererFeature
//
//  前置条件：同 OutlineFeature（URP_ENABLED 宏定义 + URP 包安装）。
//  Shader：Assets/Shaders/URP/RPGFullscreenFade.shader（Hidden/RPG/FullscreenFade）
//
//  用途：
//    • 场景切换时的淡入 / 淡出（黑屏、白屏、颜色过渡）
//    • 玩家死亡时的红色闪烁叠加
//    • 技能使用时的全屏色调变化
//
//  运行时驱动方式：
//    在 MonoBehaviour 中修改 FullscreenFadeState.Alpha / Color，
//    本 Pass 每帧读取静态状态并执行全屏绘制，无需场景引用 RendererFeature。
//
//  性能特征：
//    • Alpha <= 0.001 时完全跳过 Pass（零开销）
//    • 单次 DrawProcedural（全屏三角形），无临时 RT，无纹理采样
// ────────────────────────────────────────────────────────────────────────────
#if URP_ENABLED
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Framework.Graphics.URP
{
    // ── ScriptableRendererFeature ─────────────────────────────────────────────

    public sealed class FullscreenFadeFeature : ScriptableRendererFeature
    {
        [Tooltip("Pass 插入时机。AfterRenderingPostProcessing 保证在后处理之后叠加淡化色。")]
        [SerializeField]
        private RenderPassEvent _passEvent = RenderPassEvent.AfterRenderingPostProcessing;

        private FullscreenFadePass _pass;

        public override void Create()
        {
            _pass = new FullscreenFadePass
            {
                renderPassEvent = _passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            if (!FullscreenFadeState.IsActive) return; // Alpha 为 0 时跳过

            _pass.Setup(renderer.cameraColorTargetHandle,
                        renderer.cameraDepthTargetHandle);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
            => _pass?.Dispose();
    }

    // ── RenderPass（内部实现） ────────────────────────────────────────────────

    internal sealed class FullscreenFadePass : ScriptableRenderPass, IDisposable
    {
        private const string k_Tag = "RPG.FullscreenFade";

        private readonly ProfilingSampler _sampler = new ProfilingSampler(k_Tag);

        private Material _fadeMat;     // Hidden/RPG/FullscreenFade
        private RTHandle _cameraColor;
        private RTHandle _cameraDepth;

        internal FullscreenFadePass()
            => _fadeMat = CoreUtils.CreateEngineMaterial("Hidden/RPG/FullscreenFade");

        internal void Setup(RTHandle cameraColor, RTHandle cameraDepth)
        {
            _cameraColor = cameraColor;
            _cameraDepth = cameraDepth;
        }

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (_fadeMat == null) return;

            // 每帧从共享状态读取最新的颜色 + 透明度
            _fadeMat.SetColor(URPShaderIds.FadeColor, FullscreenFadeState.Color);
            _fadeMat.SetFloat(URPShaderIds.FadeAlpha, FullscreenFadeState.Alpha);

            var cmd = CommandBufferPool.Get(k_Tag);
            using (new ProfilingScope(cmd, _sampler))
            {
                cmd.SetRenderTarget(_cameraColor, _cameraDepth);
                // 全屏三角形，Blend SrcAlpha OneMinusSrcAlpha
                // 片元着色器输出 (_FadeColor.rgb, _FadeAlpha)
                // → 画面 = FadeColor * Alpha + OriginalPixel * (1 - Alpha)
                cmd.DrawProcedural(Matrix4x4.identity, _fadeMat, 0,
                    MeshTopology.Triangles, vertexCount: 3);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
            => CoreUtils.Destroy(_fadeMat);
    }
}
#endif
