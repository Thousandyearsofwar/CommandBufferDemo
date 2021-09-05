using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//@@@测试内容0：RenderTexture申请方式：GetTemporaryRT/正常new RenderTexture/使用RenderTargetHandle

//@@@区分下：RenderTargetIdentifier和RenderTexture[完全不同概念,可以理解为RenderTexture的指示器]
//RenderTexture:渲染纹理
//RenderTargetIdentifier:配合SetRenderTarget能够指定RenderTexture的某一个Mip或者CubeMap作为渲染目标
//RenderTargetHandle:https://zhuanlan.zhihu.com/p/215718642
// URP对RenderTargetIdentifier的一个封装
// 保存shader变量的id，提升性能，避免多次hash计算
// 真正用rt的时候，才会创建RenderTargetIdentifier
// 定义了一个静态CameraTarget

//所以在GetTemporaryRT时只能够用int
//SetRenderTarget 就可以用int//RenderTexture [which has implicit conversion operators to save on typing.]隐式转换到RenderTargetIdentifier

//@@@测试内容1：切换SetRenderTarget的时间节点，说明ConfigureTarget的必要性


class RenderTextureRequestPass : ScriptableRenderPass
{
    //Profile use sampler
    ProfilingSampler m_ProfilingSampler;
    string m_ProfilerTag;

    private RenderTexture renderTexture;
    private static int renderTextureID;
    private RenderTargetHandle renderTargetHandle;

    private RenderTargetIdentifier renderTargetIdentifier;


    public RenderTextureRequestPass(string profilerTag)
    {
        this.m_ProfilingSampler = new ProfilingSampler(profilerTag);
        this.m_ProfilerTag = profilerTag;
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        renderTexture = new RenderTexture(renderingData.cameraData.cameraTargetDescriptor);

        RTRequestTest0_0(cmd, ref renderingData);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer commandBuffer = CommandBufferPool.Get();

        //RTRequestTest0_2(commandBuffer, context, ref renderingData);

        //Do something at here...
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            commandBuffer.ClearRenderTarget(true, true, Color.blue, 1);
        }
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
        CommandBufferPool.Release(commandBuffer);
    }

    void RTRequestTest0_0(CommandBuffer commandBuffer, ref RenderingData renderingData)
    {
        RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        renderTextureID = Shader.PropertyToID("Request_ID");
        commandBuffer.GetTemporaryRT(renderTextureID, cameraDescriptor, FilterMode.Bilinear);

        ConfigureTarget(renderTextureID);
    }

    void RTRequestTest0_1(CommandBuffer commandBuffer, ref RenderingData renderingData)
    {
        RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;

        renderTargetHandle.Init("Request_Handle");
        //commandBuffer.GetTemporaryRT(renderTargetHandle.Identifier(), cameraDescriptor, FilterMode.Bilinear);//错误示范
        commandBuffer.GetTemporaryRT(renderTargetHandle.id, cameraDescriptor, FilterMode.Bilinear);

        ConfigureTarget(renderTargetHandle.Identifier());
    }

    //错误对比用放置于Excute中
    void RTRequestTest0_2(CommandBuffer commandBuffer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;

        renderTextureID = Shader.PropertyToID("Request_ID");
        commandBuffer.GetTemporaryRT(renderTextureID, cameraDescriptor, FilterMode.Bilinear);
        // commandBuffer.GetTemporaryRT(renderTextureID, cameraDescriptor.width, cameraDescriptor.height, 16, FilterMode.Bilinear,
        // UnityEngine.Experimental.Rendering.GraphicsFormat.A2B10G10R10_UNormPack32, 1, false, RenderTextureMemoryless.None, false);//Full of parameters version

        commandBuffer.SetRenderTarget(renderTextureID);

        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }


    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        //@@@记得销毁Pass中创建RenderTexture,不然会内存泄露
        UnityEngine.Object.DestroyImmediate(renderTexture);
        cmd.ReleaseTemporaryRT(renderTextureID);
        cmd.ReleaseTemporaryRT(renderTargetHandle.id);
    }
}
