using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
class DrawAndBlitTestPass : ScriptableRenderPass
{
    public ScriptableRenderer renderer;
    private ProfilingSampler m_ProfilingSampler;
    string m_ProfilerTag = "HSVAdjust";
    public Material material;

    public float _Hue;
    public float _Saturation;
    public float _Value;

    private static readonly int renderTextureID = Shader.PropertyToID("HSVAdjustRT");

    public DrawAndBlitTestPass(Material mat, float _Hue, float _Saturation, float _Value)
    {
        this.material = mat;
        this._Hue = _Hue;
        this._Saturation = _Saturation;
        this._Value = _Value;
        m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);
    }

    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        CameraData cameraData = renderingData.cameraData;
        RenderTextureDescriptor camDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        cmd.GetTemporaryRT(renderTextureID, camDescriptor.width, camDescriptor.height, 0, FilterMode.Bilinear, UnityEngine.Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32);
        RenderTargetIdentifier renderTargetIdentifier = new RenderTargetIdentifier(renderTextureID, 0, CubemapFace.Unknown, 0);
        ConfigureTarget(renderTextureID);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer commandBuffer = CommandBufferPool.Get();

        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            commandBuffer.SetViewport(renderingData.cameraData.camera.pixelRect);

            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetFloat("_Hue", _Hue);
            materialPropertyBlock.SetFloat("_Saturation", _Saturation);
            materialPropertyBlock.SetFloat("_Value", _Value);

            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 0, materialPropertyBlock);

            commandBuffer.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);

            //commandBuffer.Blit(renderTextureID, renderer.cameraColorTarget, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            commandBuffer.Blit(renderTextureID, renderer.cameraColorTarget);
        }

        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        CommandBufferPool.Release(commandBuffer);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (cmd == null)
        {
            throw new ArgumentNullException("cmd");
        }
        cmd.ReleaseTemporaryRT(renderTextureID);
    }
}

