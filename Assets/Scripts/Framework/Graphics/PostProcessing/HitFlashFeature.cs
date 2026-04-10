// ─────────────────────────────────────────────────────────────────────────────
//  HitFlashFeature — URP 受击全屏闪光 ScriptableRendererFeature
//
//  前置条件：项目已安装 URP 包，已在 Player Settings 添加 URP_ENABLED 宏定义。
//  Shader：Assets/Shaders/URP/RPGHitFlash.shader（Hidden/RPG/HitFlash）
//
//  用途：
//    • 受击时叠加全屏白色/红色/橙色半透明闪光（即时视觉反馈）
//    • 治疗时短暂绿色光晕
//    • 任意需要瞬间全屏色调叠加的场合
//
//  运行时驱动方式：
//    在 HitFlashController 中修改 HitFlashState.FlashColor / FlashIntensity，
//    本 Pass 每帧读取静态状态并执行全屏绘制，无需场景引用 RendererFeature。
//
//  性能特征：
//    • FlashIntensity <= 0.002 时完全跳过 Pass（零开销）
//    • 单次 DrawProcedural（全屏三角形），无临时 RT，无纹理采样
//    • 插入在 AfterRenderingPostProcessing，与 FullscreenFadeFeature 兼容叠加
// ─────────────────────────────────────────────────────────────────────────────
#if URP_ENABLED
using System;
using Framework.Graphics.URP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Framework.Graphics.PostProcessing
{
    // ── ScriptableRendererFeature ─────────────────────────────────────────────

    /// <summary>
    /// 受击全屏闪光 URP 渲染特性。
    ///
    /// 在 URP Renderer 的 "Renderer Features" 列表中添加此项后，
    /// 通过 <see cref="HitFlashController"/> 或直接修改 <see cref="HitFlashState"/>
    /// 即可驱动效果。
    ///
    /// 渲染顺序：<c>AfterRenderingPostProcessing + 1</c>（晚于所有后处理，早于 UI）。
    /// </summary>
    public sealed class HitFlashFeature : ScriptableRendererFeature
    {
        [Tooltip("Pass 插入时机。AfterRenderingPostProcessing+1 保证在 URP 后处理之后叠加。")]
        [SerializeField]
        private RenderPassEvent _passEvent = RenderPassEvent.AfterRenderingPostProcessing + 1;

        private HitFlashPass _pass;

        public override void Create()
        {
            _pass = new HitFlashPass
            {
                renderPassEvent = _passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            if (!HitFlashState.IsActive) return;

            _pass.Setup(renderer.cameraColorTargetHandle,
                        renderer.cameraDepthTargetHandle);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
            => _pass?.Dispose();
    }

    // ── RenderPass（内部实现） ────────────────────────────────────────────────

    internal sealed class HitFlashPass : ScriptableRenderPass, IDisposable
    {
        private const string k_Tag = "RPG.HitFlash";

        private readonly ProfilingSampler _sampler = new ProfilingSampler(k_Tag);

        private Material _flashMat;
        private RTHandle _cameraColor;
        private RTHandle _cameraDepth;

        internal HitFlashPass()
            => _flashMat = CoreUtils.CreateEngineMaterial("Hidden/RPG/HitFlash");

        internal void Setup(RTHandle cameraColor, RTHandle cameraDepth)
        {
            _cameraColor = cameraColor;
            _cameraDepth = cameraDepth;
        }

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (_flashMat == null) return;

            _flashMat.SetColor(URPShaderIds.FlashColor,     HitFlashState.FlashColor);
            _flashMat.SetFloat(URPShaderIds.FlashIntensity, HitFlashState.FlashIntensity);

            var cmd = CommandBufferPool.Get(k_Tag);
            using (new ProfilingScope(cmd, _sampler))
            {
                cmd.SetRenderTarget(_cameraColor, _cameraDepth);
                // Blend SrcAlpha OneMinusSrcAlpha
                // 片元输出 (FlashColor.rgb, FlashIntensity)
                // → 画面 = FlashColor * Intensity + Original * (1 - Intensity)
                cmd.DrawProcedural(Matrix4x4.identity, _flashMat, 0,
                    MeshTopology.Triangles, vertexCount: 3);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
            => CoreUtils.Destroy(_flashMat);
    }
}
#endif
