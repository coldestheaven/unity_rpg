// ────────────────────────────────────────────────────────────────────────────
//  OutlineFeature — URP 轮廓描边 ScriptableRendererFeature
//
//  前置条件：
//    1. 项目已安装 Universal Render Pipeline 包（com.unity.render-pipelines.universal）
//    2. 在 Player Settings > Scripting Define Symbols 中添加  URP_ENABLED
//    3. 将 "Hidden/RPG/Outline" shader（Assets/Shaders/URP/RPGOutline.shader）
//       放入项目，或加入 Always Included Shaders。
//    4. 在 URP Renderer Data Asset（UniversalRendererData）的 Renderer Features
//       列表中点击 "Add Renderer Feature" → 选择 "Outline Feature"。
//
//  渲染流程（每帧）：
//    Step 1 – 将 OutlineRegistry 中的所有对象渲染到 R8 临时 RT（白色轮廓掩膜）
//             使用 Hidden/RPG/Outline Pass 0（Silhouette）作为 overrideMaterial。
//    Step 2 – 在相机颜色 RT 上以混合模式 DrawProcedural，
//             Pass 1（OutlineComposite）通过 8 方向膨胀 + 遮罩差分计算轮廓边缘，
//             将轮廓颜色叠加至画面（无需临时拷贝相机颜色，零额外带宽开销）。
//
//  性能特征：
//    • 每帧 1 个临时 R8 RT + 2 次 DrawCall（DrawRenderers + DrawProcedural）
//    • 当 OutlineRegistry.Count == 0 时 Pass 完全跳过，零开销
// ────────────────────────────────────────────────────────────────────────────
#if URP_ENABLED
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Framework.Graphics.URP
{
    // ── 配置 ─────────────────────────────────────────────────────────────────

    [Serializable]
    public sealed class OutlineSettings
    {
        [Tooltip("轮廓颜色（Alpha 控制轮廓不透明度）。")]
        public Color OutlineColor = new Color(1f, 0.85f, 0.1f, 1f);

        [Tooltip("轮廓宽度（像素数，膨胀半径）。"), Range(1, 16)]
        public int OutlineWidth = 2;

        [Tooltip("参与渲染轮廓遮罩的 Layer Mask（需与 OutlineController 所在 Layer 匹配）。")]
        public LayerMask OutlineLayers = ~0;

        [Tooltip("Pass 插入时机。AfterRenderingTransparents 保证在透明物体后绘制轮廓。")]
        public RenderPassEvent PassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // ── ScriptableRendererFeature ─────────────────────────────────────────────

    public sealed class OutlineFeature : ScriptableRendererFeature
    {
        [SerializeField] public OutlineSettings Settings = new OutlineSettings();

        private OutlineRenderPass _pass;

        public override void Create()
        {
            _pass = new OutlineRenderPass(Settings)
            {
                renderPassEvent = Settings.PassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            // 编辑器 SceneView 不需要轮廓
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            // 没有要描边的对象时完全跳过
            if (OutlineRegistry.Count == 0) return;

            _pass.Setup(renderer.cameraColorTargetHandle,
                        renderer.cameraDepthTargetHandle);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
        }
    }

    // ── RenderPass（内部实现） ────────────────────────────────────────────────

    internal sealed class OutlineRenderPass : ScriptableRenderPass, IDisposable
    {
        private const string k_Tag = "RPG.Outline";

        // 渲染遮罩时识别的 Shader Pass 标签（URP 的主要不透明/精灵 Pass）
        private static readonly ShaderTagId[] k_ShaderTagIds =
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("Universal2D"),
        };

        private readonly OutlineSettings  _settings;
        private readonly ProfilingSampler _sampler = new ProfilingSampler(k_Tag);

        private Material _outlineMat;        // Hidden/RPG/Outline
        private RTHandle _cameraColor;
        private RTHandle _cameraDepth;

        // 临时遮罩 RT
        private readonly int _maskRTId = Shader.PropertyToID("_RPGOutlineMask");

        internal OutlineRenderPass(OutlineSettings settings)
        {
            _settings   = settings;
            _outlineMat = CoreUtils.CreateEngineMaterial("Hidden/RPG/Outline");
        }

        internal void Setup(RTHandle cameraColor, RTHandle cameraDepth)
        {
            _cameraColor = cameraColor;
            _cameraDepth = cameraDepth;
        }

        // ── 分配临时 RT ──────────────────────────────────────────────────────

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var camDesc = renderingData.cameraData.cameraTargetDescriptor;
            // R8：只需单通道遮罩，最小带宽
            cmd.GetTemporaryRT(_maskRTId,
                camDesc.width, camDesc.height,
                depthBuffer: 0,
                FilterMode.Bilinear,
                RenderTextureFormat.R8);
        }

        // ── 主渲染逻辑 ───────────────────────────────────────────────────────

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (_outlineMat == null) return;

            var cmd = CommandBufferPool.Get(k_Tag);
            using (new ProfilingScope(cmd, _sampler))
            {
                // ── Step 1：渲染剪影到遮罩 RT ─────────────────────────────────
                cmd.SetRenderTarget(_maskRTId);
                cmd.ClearRenderTarget(clearDepth: false, clearColor: true, Color.clear);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // DrawRenderers 会把 OutlineLayers 内所有 Renderer
                // 以 Pass 0 (Silhouette) 渲染到 _maskRTId（白色不透明像素）
                var sortFlags    = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(k_ShaderTagIds,
                    ref renderingData, sortFlags);
                drawSettings.overrideMaterial          = _outlineMat;
                drawSettings.overrideMaterialPassIndex = 0; // Silhouette Pass

                var filterSettings = new FilteringSettings(
                    RenderQueueRange.all, _settings.OutlineLayers);

                context.DrawRenderers(renderingData.cullResults,
                    ref drawSettings, ref filterSettings);

                // ── Step 2：将轮廓叠加至相机颜色 RT ──────────────────────────
                // 设置全局参数（下一帧也有效，Pass 结束前会被覆写回去，安全）
                cmd.SetRenderTarget(_cameraColor, _cameraDepth);
                cmd.SetGlobalTexture(URPShaderIds.MaskTex, _maskRTId);

                // 计算遮罩纹素尺寸（用于着色器中的膨胀偏移）
                int w = renderingData.cameraData.cameraTargetDescriptor.width;
                int h = renderingData.cameraData.cameraTargetDescriptor.height;
                cmd.SetGlobalVector(URPShaderIds.MaskTexelSize,
                    new Vector4(1f / w, 1f / h, w, h));

                // 在材质上更新当前帧的轮廓颜色（单次 MaterialProperty 写入）
                _outlineMat.SetColor(URPShaderIds.OutlineColor, _settings.OutlineColor);
                _outlineMat.SetFloat(URPShaderIds.OutlineWidth,  _settings.OutlineWidth);

                // 全屏三角形 DrawProcedural：Pass 1 (OutlineComposite)
                // Blend：SrcAlpha OneMinusSrcAlpha → 轮廓像素叠加，非轮廓像素透明不影响
                cmd.DrawProcedural(Matrix4x4.identity, _outlineMat, 1,
                    MeshTopology.Triangles, vertexCount: 3);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // ── 释放临时 RT ──────────────────────────────────────────────────────

        public override void OnCameraCleanup(CommandBuffer cmd)
            => cmd.ReleaseTemporaryRT(_maskRTId);

        // ── IDisposable ──────────────────────────────────────────────────────

        public void Dispose()
            => CoreUtils.Destroy(_outlineMat);
    }
}
#endif
