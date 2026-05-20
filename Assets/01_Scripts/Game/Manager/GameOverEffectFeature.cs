using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace TrainDefense.Game.Manager
{
    /// <summary>
    /// GameOverEffectController.IsActive = true 일 때만 패스를 실행한다.
    /// Renderer2D.asset에 이 Feature를 추가하고 GameOverEffectController를 씬에 배치하면 된다.
    /// </summary>
    public class GameOverEffectFeature : ScriptableRendererFeature
    {
        public static bool IsActive { get; set; }
        public static Material Material { get; set; }

        private GameOverEffectPass _pass;

        public override void Create()
        {
            _pass = new GameOverEffectPass(RenderPassEvent.AfterRenderingTransparents);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!IsActive || Material == null)
                return;

            _pass.Setup(Material);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
        }

        private class GameOverEffectPass : ScriptableRenderPass, IDisposable
        {
            private Material _material;
            private RTHandle _tempRT;

            public GameOverEffectPass(RenderPassEvent passEvent)
            {
                renderPassEvent = passEvent;
            }

            public void Setup(Material material) => _material = material;

            // ── URP 17 Render Graph ──────────────────────────────────────

            private class CopyPassData
            {
                public TextureHandle Src;
            }

            private class EffectPassData
            {
                public TextureHandle Src;
                public Material Mat;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_material == null)
                    return;

                // Overlay 카메라(UI 전용 등)에는 적용하지 않음
                var cameraData = frameData.Get<UniversalCameraData>();
                if (cameraData.renderType != CameraRenderType.Base)
                    return;

                var resourceData = frameData.Get<UniversalResourceData>();
                var activeColor  = resourceData.activeColorTexture;

                var desc = renderGraph.GetTextureDesc(activeColor);
                desc.name            = "_GameOverEffectTemp";
                desc.clearBuffer     = false;
                desc.depthBufferBits = 0;
                TextureHandle temp   = renderGraph.CreateTexture(desc);

                // 1) Copy activeColor → temp (원본 보존)
                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>(
                    "GameOverEffect_Copy", out var pd))
                {
                    pd.Src = activeColor;
                    builder.UseTexture(pd.Src, AccessFlags.Read);
                    builder.SetRenderAttachment(temp, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (CopyPassData d, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, d.Src, new Vector4(1, 1, 0, 0), 0, false);
                    });
                }

                // 2) Blit temp → activeColor (효과 적용)
                using (var builder = renderGraph.AddRasterRenderPass<EffectPassData>(
                    "GameOverEffect_Apply", out var pd))
                {
                    pd.Src = temp;
                    pd.Mat = _material;
                    builder.UseTexture(pd.Src, AccessFlags.Read);
                    builder.SetRenderAttachment(activeColor, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (EffectPassData d, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, d.Src, new Vector4(1, 1, 0, 0), d.Mat, 0);
                    });
                }
            }

            // ── 레거시 패스 (Compatibility Mode 시 사용) ────────────────

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, FilterMode.Bilinear,
                    name: "_GameOverEffectTemp");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null)
                    return;

                var cmd    = CommandBufferPool.Get("GameOverEffect");
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                Blitter.BlitCameraTexture(cmd, source, _tempRT, _material, 0);
                Blitter.BlitCameraTexture(cmd, _tempRT, source);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                _tempRT?.Release();
                _tempRT = null;
            }
        }
    }
}
