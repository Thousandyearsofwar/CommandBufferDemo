using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
//用Draw指令取代Blit
public class DrawAndBlitTestRendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;

    public Material material;


    public float _Hue;
    public float _Saturation;
    public float _Value;


    DrawAndBlitTestPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        if (material != null)
        {
            m_ScriptablePass = new DrawAndBlitTestPass(material, _Hue, _Saturation, _Value);

            // Configures where the render pass should be injected.
            m_ScriptablePass.renderPassEvent = Event;
        }

    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
        m_ScriptablePass.renderer = renderer;
    }
}


