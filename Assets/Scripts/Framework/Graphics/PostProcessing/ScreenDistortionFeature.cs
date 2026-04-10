// ─────────────────────────────────────────────────────────────────────────────
//  ScreenDistortionFeature — URP 屏幕空间扰曲 ScriptableRendererFeature
//
//  前置条件：项目已安装 URP 包，已在 Player Settings 添加 URP_ENABLED 宏定义。
//  Shader：Assets/Shaders/URP/RPGScreenDistortion.shader（Hidden/RPG/ScreenDistortion）
//
//  用途：
//    • 爆炸/魔法冲击波扩散的环状 UV 扰曲
//    • 强力技能落地的全屏震动效果
//    • 任何需要非线性屏幕空间扭曲的视觉效果
//
//  渲染原理：
//    1. 将当前摄像机颜色缓冲 Blit 到一张临时 RT（_SourceTex）
//    2. 全屏三角形读取 _SourceTex，按冲击波数学位移 UV，输出到摄像机颜色缓冲
//    → 实现无缝的 UV 扭曲，不依赖深度或法线
//
//  性能特征：
//    • Strength <= 0.001 时完全跳过 Pass（零开销）
//    • 一次 Blit（复制）+ 一次 DrawProcedural（扭曲），共两次渲染
//    • 临时 RT 帧结束自动释放，无常驻内存
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
    /// 屏幕空间扰曲 URP 渲染特性。
    ///
    /// 在 URP Renderer 的 "Renderer Features" 列表中添加此项后，通过
    /// <see cref="ScreenDistortionController"/> 或直接修改
    /// <see cref="ScreenDistortionState"/> 驱动效果。
    ///
    /// 渲染顺序：<c>AfterRenderingOpaques</c>（在透明物体之后、后处理之前）。
    /// </summary>
    public sealed class ScreenDistortionFeature : ScriptableRendererFeature
    {
        [Tooltip("Pass 插入时机。AfterRenderingOpaques 使扰曲作用于不透明物体；" +
                 "AfterRenderingPostProcessing 则作用于完整后处理画面。")]
        [SerializeField]
        private RenderPassEvent _passEvent = RenderPassEvent.AfterRenderingPostProcessing;

        private ScreenDistortionPass _pass;

        public override void Create()
        {
            _pass = new ScreenDistortionPass
            {
                renderPassEvent = _passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            if (!ScreenDistortionState.IsActive) return;

            _pass.Setup(renderer.cameraColorTargetHandle,
                        renderer.cameraDepthTargetHandle);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
            => _pass?.Dispose();
    }

    // ── RenderPass（内部实现） ────────────────────────────────────────────────

    internal sealed class ScreenDistortionPass : ScriptableRenderPass, IDisposable
    {
        private const string k_Tag     = "RPG.ScreenDistortion";
        private const string k_TempRT  = "_RPGDistortionSource";

        private readonly ProfilingSampler _sampler = new ProfilingSampler(k_Tag);

        private Material _distortMat;
        private RTHandle _cameraColor;
        private RTHandle _cameraDepth;

        // 用于持有临时 RT 的 RenderTextureDescriptor
        private RTHandle _sourceRT;

        internal ScreenDistortionPass()
            => _distortMat = CoreUtils.CreateEngineMaterial("Hidden/RPG/ScreenDistortion");

        internal void Setup(RTHandle cameraColor, RTHandle cameraDepth)
        {
            _cameraColor = cameraColor;
            _cameraDepth = cameraDepth;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 分配与摄像机相同尺寸的临时颜色 RT
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _sourceRT, desc, name: k_TempRT);
        }

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (_distortMat == null || _sourceRT == null) return;

            // 上传每帧扰曲参数
            _distortMat.SetVector(URPShaderIds.DistortionCenter,
                new Vector4(
                    ScreenDistortionState.Center.x,
                    ScreenDistortionState.Center.y, 0, 0));
            _distortMat.SetFloat(URPShaderIds.DistortionStrength, ScreenDistortionState.Strength);
            _distortMat.SetFloat(URPShaderIds.DistortionRadius,   ScreenDistortionState.Radius);
            _distortMat.SetFloat(URPShaderIds.DistortionRingWidth, ScreenDistortionState.RingWidth);

            var cmd = CommandBufferPool.Get(k_Tag);
            using (new ProfilingScope(cmd, _sampler))
            {
                // Step 1：将当前摄像机颜色复制到 _sourceRT
                Blitter.BlitCameraTexture(cmd, _cameraColor, _sourceRT);

                // Step 2：以扰曲材质将 _sourceRT 渲染回 _cameraColor
                cmd.SetGlobalTexture(URPShaderIds.DistortionSourceTex, _sourceRT);
                cmd.SetRenderTarget(_cameraColor, _cameraDepth);
                cmd.DrawProcedural(Matrix4x4.identity, _distortMat, 0,
                    MeshTopology.Triangles, vertexCount: 3);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
        {
            _sourceRT?.Release();
            _sourceRT = null;
            CoreUtils.Destroy(_distortMat);
        }
    }
}
#endif
