using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class RenderTextureRequestRenderFeature : ScriptableRendererFeature
{
    RenderTextureRequestPass m_ScriptablePass;
    public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new RenderTextureRequestPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = Event;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}