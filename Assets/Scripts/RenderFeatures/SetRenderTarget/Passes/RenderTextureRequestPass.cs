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
        RTRequestTest0_0(cmd, ref renderingData);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer commandBuffer = CommandBufferPool.Get();

        //RTRequestTest0_4(commandBuffer, context, ref renderingData);

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
        //1.renderTexture == null必要性 只创建一张RenderTexture
        //2.IsCreated 防止未被创建
        //3.CoreUtils.Destroy 安全Destory RenderTexture
        //4.编辑器上面的bug 
        //从哪里参考:ShadowsMidtonesHighlightsEditor.cs
        //5.renderTexture.Create();
        if (renderTexture == null || !renderTexture.IsCreated())
        {
            CoreUtils.Destroy(renderTexture);
            renderTexture = new RenderTexture(cameraDescriptor);
            renderTexture.name = "RequestRT";
        }
        //配置RenderTarget[渲染目标]
        ConfigureTarget(renderTexture);
    }

    void RTRequestTest0_1(CommandBuffer commandBuffer, ref RenderingData renderingData)
    {
        RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        //1.申请临时的RenderTexture 必须手动Release掉
        //需要时刻关心生命周期！！
        //2.释放资源的区别:RenderTexture.Release和Destroy。https://zhuanlan.zhihu.com/p/41251356 
        //RenderTexture.Release释放显存，内存不释放
        //Destroy会把Object销毁的同时连带显存释放掉
        //所以出于性能考虑，频繁使用Destory会加重申请内存的负担
        //从哪里参考:ShadowUtils.cs
        renderTexture = RenderTexture.GetTemporary(cameraDescriptor);
        renderTexture.name = "RequestRT";
        ConfigureTarget(renderTexture);
    }

    void RTRequestTest0_2(CommandBuffer commandBuffer, ref RenderingData renderingData)
    {
        RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        //1.Shader.PropertyToID的作用：
        //用字符串获取一个唯一的HashID
        //相当于一个全局的id，需要保证其他Material着色使用这张RenderTexture时，这张RenderTexture渲染完成，没有被Release掉
        //就不用手动setTexture了
        renderTextureID = Shader.PropertyToID("Request_ID");
        //2.RenderTexture.GetTemporary和commandBuffer.GetTemporaryRT[申请RenderTexture的区别]
        //1.RenderTexture.GetTemporary需要手动释放掉
        //2.使用CommandBuffer申请的临时的RenderTexture如果都没有显式地使用(Release)TemporaryRT，在相机完成渲染后或Graphics.ExecuteCommandBuffer完成后被移除(Remove)(Destory?)
        commandBuffer.GetTemporaryRT(renderTextureID, cameraDescriptor, FilterMode.Bilinear);

        ConfigureTarget(renderTextureID);
    }

    //使用URP的renderTargetHandle获取PropertyToID
    void RTRequestTest0_3(CommandBuffer commandBuffer, ref RenderingData renderingData)
    {
        RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;

        renderTargetHandle.Init("Request_Handle");
        //commandBuffer.GetTemporaryRT(renderTargetHandle.Identifier(), cameraDescriptor, FilterMode.Bilinear);//错误示范
        commandBuffer.GetTemporaryRT(renderTargetHandle.id, cameraDescriptor, FilterMode.Bilinear);

        ConfigureTarget(renderTargetHandle.Identifier());
    }

    //错误对比用放置于Excute中
    void RTRequestTest0_4(CommandBuffer commandBuffer, ScriptableRenderContext context, ref RenderingData renderingData)
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
        //@@@记得释放Pass中创建RenderTexture,不然会内存泄露
        // if (renderTexture)
        // {
        //     RenderTexture.ReleaseTemporary(renderTexture);
        //     renderTexture = null;
        // }
        cmd.ReleaseTemporaryRT(renderTextureID);
        cmd.ReleaseTemporaryRT(renderTargetHandle.id);
    }
}
