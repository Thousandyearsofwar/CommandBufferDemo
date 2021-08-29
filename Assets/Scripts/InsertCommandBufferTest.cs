using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InsertCommandBufferTest : MonoBehaviour
{
    public RenderTexture renderTexture;
    public Material m_Material;
    public CameraEvent m_CameraEvent;
    private void OnValidate()
    {
        if (m_Material != null)
        {
            var renderer = (Renderer)GetComponents<Renderer>().GetValue(0);
            if (renderTexture == null)
            {
                renderTexture = RenderTexture.GetTemporary(Camera.main.pixelHeight, Camera.main.pixelWidth, 16, RenderTextureFormat.R8);
            }
            else
            {
                var commandBuffer = new CommandBuffer();
                commandBuffer.SetRenderTarget(renderTexture);
                commandBuffer.ClearRenderTarget(true, true, Color.black);
                commandBuffer.DrawRenderer(renderer, m_Material);

                Camera.main.AddCommandBuffer(m_CameraEvent, commandBuffer);
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        if (m_Material != null)
        {
            var renderer = (Renderer)GetComponents<Renderer>().GetValue(0);
            if (renderTexture == null)
            {
                renderTexture = RenderTexture.GetTemporary(Camera.main.pixelHeight, Camera.main.pixelWidth, 16, RenderTextureFormat.R8);
            }
            else
            {
                var commandBuffer = new CommandBuffer();
                commandBuffer.SetRenderTarget(renderTexture);
                commandBuffer.ClearRenderTarget(true, true, Color.black);
                commandBuffer.DrawRenderer(renderer, m_Material);

                Camera.main.AddCommandBuffer(m_CameraEvent, commandBuffer);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
