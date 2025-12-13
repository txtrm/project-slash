using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineFeature : ScriptableRendererFeature
{
    class OutlinePass : ScriptableRenderPass
    {
        Material outlineMat;
        public OutlinePass(Material mat)
        {
            outlineMat = mat;
            renderPassEvent = RenderPassEvent.AfterRendering;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (outlineMat == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("OutlinePass");
            cmd.Blit(null, BuiltinRenderTextureType.CameraTarget, outlineMat);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public Material outlineMaterial;
    OutlinePass pass;

    public override void Create()
    {
        pass = new OutlinePass(outlineMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }
}