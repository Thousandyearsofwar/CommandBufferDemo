using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class SetRenderTargetPass : ScriptableRenderPass
{
    //Profile use sampler
    ProfilingSampler m_ProfilingSampler;
    string m_ProfilerTag;

    //ShaderTagId
    List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
    //Camera setting override
    CustomCameraSettings m_CameraSettings;

    //Render queue type
    RenderQueueType m_RenderQueueType;

    //Override material
    public Material overrideMaterial { get; set; }
    public int overrideMaterialPassIndex { get; set; }

    //Pass parameter
    //CullingResults+DrawingSettings+FilteringSettings
    DrawingSettings m_DrawingSetting;
    FilteringSettings m_FilteringSetting;
    FilteringSettings m_FilteringSetting1;


    //Override RenderStateBlock
    //Depth state
    bool overrideDepthState;
    bool enableWrite;
    CompareFunction depthCompareFunction;
    //Stencil state
    StencilStateData stencilSettings;

    static int renderTextureID = Shader.PropertyToID("Request_RT");
    RenderStateBlock m_RenderStateBlock;

    //SetRenderTarget Test parameter
    RenderTexture mipTexture;//TODO:RenderTexture.GetTemporary
    RenderTexture[] mipTextures = new RenderTexture[3];//MRT use
    RenderTexture depthTexture;
    RenderTexture[] tempResolveRenderTexture = new RenderTexture[3];//Temp Resolve RT

    RenderTargetIdentifier[] renderTargetIdentifiers = new RenderTargetIdentifier[3];//MRT Color RenderTargetIdentifier
    RenderTargetIdentifier[] tempRenderTargetIdentifiers = new RenderTargetIdentifier[3];//MRT MSAA temp to use resolve
    RenderTargetIdentifier renderTargetDepthIdentifier;//MRT Depth RenderTargetIdentifier
    RenderTargetIdentifier tempRenderTargetDepthIdentifier;//MRT MSAA temp Depth RenderTargetIdentifier
    //MRT Binding color Load/Store Action
    RenderBufferLoadAction[] m_ColorLoadAction = new RenderBufferLoadAction[3];
    RenderBufferStoreAction[] m_ColorStoreAction = new RenderBufferStoreAction[3];

    public SetRenderTargetPass(string profilerTag, RenderPassEvent renderPassEvent, RenderQueueType renderQueueType, int layerMask, int LayerMask1, string[] shaderTags,
    Material overrideMaterial, int overrideMaterialPassIndex,
    bool overrideDepthState, bool enableWrite, CompareFunction depthCompareFunction,
    StencilStateData stencilSettings,
    CustomCameraSettings cameraSettings)
    {
        this.m_ProfilingSampler = new ProfilingSampler(profilerTag);
        this.m_ProfilerTag = profilerTag;

        this.renderPassEvent = renderPassEvent;
        this.m_RenderQueueType = renderQueueType;

        this.overrideMaterial = overrideMaterial;
        this.overrideMaterialPassIndex = overrideMaterialPassIndex;

        RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent) ? RenderQueueRange.transparent : RenderQueueRange.opaque;
        //@@@Filtering Setting
        this.m_FilteringSetting = new FilteringSettings(renderQueueRange, layerMask);
        this.m_FilteringSetting1 = new FilteringSettings(renderQueueRange, LayerMask1);
        if (shaderTags != null && shaderTags.Length > 0)
        {
            foreach (var passName in shaderTags)
            {
                m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }
        }
        else
        {
            m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
        }
        m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        m_CameraSettings = cameraSettings;

        //@@@RenderBlockState:Depth
        this.overrideDepthState = overrideDepthState;
        this.enableWrite = enableWrite;
        this.depthCompareFunction = depthCompareFunction;

        //@@@RenderBlockState:Stencil
        this.stencilSettings = stencilSettings;


        Request_RenderTexture();
    }

    public void Request_RenderTexture()
    {
        //补充说明：New RenderTexture能够控制的变量很多，tempRenderTexture的方式Mip目前没有办法控制
        mipTexture = new RenderTexture(1024, 1024, 16, UnityEngine.RenderTextureFormat.DefaultHDR, 8);
        mipTexture.name = "RequestRT";
        mipTexture.useMipMap = true;
        mipTexture.autoGenerateMips = false;

        if (mipTextures[0] == null)
        {
            mipTextures[0] = new RenderTexture(1024, 1024, 16, UnityEngine.RenderTextureFormat.ARGB2101010, 3);
            mipTextures[0].name = "RequestRT" + 0;
            mipTextures[0].useMipMap = true;
            mipTextures[0].autoGenerateMips = false;
            mipTextures[0].antiAliasing = 1;
            for (int i = 1; i < mipTextures.Length; ++i)
            {
                mipTextures[i] = new RenderTexture(1024, 1024, 0, UnityEngine.RenderTextureFormat.ARGB2101010, 3);
                mipTextures[i].name = "RequestRT" + i;
                mipTextures[i].useMipMap = true;
                mipTextures[i].autoGenerateMips = false;
                mipTextures[i].antiAliasing = 1;
            }
        }

        depthTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.R16, 3);

        RenderTextureDescriptor mipTextureDescriptor0 = mipTextures[0].descriptor;
        RenderTextureDescriptor mipTextureDescriptor1 = mipTextures[1].descriptor;

        if (tempResolveRenderTexture[0] == null)
        {
            tempResolveRenderTexture[0] = new RenderTexture(mipTextureDescriptor0);
            tempResolveRenderTexture[0].name = "tempRequestRT" + 0;
            tempResolveRenderTexture[0].useMipMap = true;
            tempResolveRenderTexture[0].autoGenerateMips = false;
            tempResolveRenderTexture[0].antiAliasing = 2;
            //mipTextures[i].memorylessMode = RenderTextureMemoryless.MSAA;
            tempResolveRenderTexture[0].bindTextureMS = true;

            for (int i = 1; i < tempResolveRenderTexture.Length; ++i)
            {
                tempResolveRenderTexture[i] = new RenderTexture(mipTextureDescriptor1);
                tempResolveRenderTexture[i].name = "tempRequestRT" + i;
                tempResolveRenderTexture[i].useMipMap = true;
                tempResolveRenderTexture[i].autoGenerateMips = false;
                tempResolveRenderTexture[i].antiAliasing = 2;
                //mipTextures[i].memorylessMode = RenderTextureMemoryless.MSAA;
                tempResolveRenderTexture[i].bindTextureMS = true;
            }
        }
    }

    void Test4_Setup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        cmd.GetTemporaryRT(renderTextureID, renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height, 8,
        FilterMode.Bilinear, UnityEngine.Experimental.Rendering.GraphicsFormat.A2B10G10R10_UNormPack32, 1, false, RenderTextureMemoryless.Depth, false);
        ConfigureTarget(renderTextureID, renderTextureID);
        ConfigureInput(ScriptableRenderPassInput.Depth);
        //看情况调用DepthOnly Pass,Opaque之后调用Pass直接Copy，如果在这之前调用DepthOnly pass
        //ConfigureInput(ScriptableRenderPassInput.Normal);
    }

    void Test5_6_Setup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        renderTargetIdentifiers[0] = new RenderTargetIdentifier(mipTextures[0], 1, CubemapFace.Unknown, 0);
        renderTargetIdentifiers[1] = new RenderTargetIdentifier(mipTextures[1], 1, CubemapFace.Unknown, 0);
        renderTargetIdentifiers[2] = new RenderTargetIdentifier(mipTextures[2], 1, CubemapFace.Unknown, 0);

        //renderTargetDepthIdentifier = new RenderTargetIdentifier(mipTextures[0].depthBuffer, 1, CubemapFace.Unknown, 0);
        renderTargetDepthIdentifier = new RenderTargetIdentifier(depthTexture, 1, CubemapFace.Unknown, 0);

        ConfigureTarget(renderTargetIdentifiers, renderTargetDepthIdentifier);
    }

    void Test7_Setup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        tempRenderTargetIdentifiers[0] = new RenderTargetIdentifier(tempResolveRenderTexture[0], 1, CubemapFace.Unknown, 0);
        tempRenderTargetIdentifiers[1] = new RenderTargetIdentifier(tempResolveRenderTexture[1], 1, CubemapFace.Unknown, 0);
        tempRenderTargetIdentifiers[2] = new RenderTargetIdentifier(tempResolveRenderTexture[2], 1, CubemapFace.Unknown, 0);

        tempRenderTargetDepthIdentifier = new RenderTargetIdentifier(tempResolveRenderTexture[0].depthBuffer, 0, CubemapFace.Unknown, 0);


        renderTargetIdentifiers[0] = new RenderTargetIdentifier(mipTextures[0], 1, CubemapFace.Unknown, 0);
        renderTargetIdentifiers[1] = new RenderTargetIdentifier(mipTextures[1], 1, CubemapFace.Unknown, 0);
        renderTargetIdentifiers[2] = new RenderTargetIdentifier(mipTextures[2], 1, CubemapFace.Unknown, 0);

        renderTargetDepthIdentifier = new RenderTargetIdentifier(mipTextures[0].depthBuffer, 0, CubemapFace.Unknown, 0);
        //@@@Something at here can't set to tempRenderTargetDepthIdentifier? why? life time different?
        ConfigureTarget(renderTargetIdentifiers, renderTargetDepthIdentifier);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        //Request Render Texture


        //Test4
        Test4_Setup(cmd, ref renderingData);

        //Test5/Test6
        //Test5_6_Setup(cmd, ref renderingData);

        //Test7
        //Test7_Setup(cmd, ref renderingData);
    }

    // Here you can implement the rendering logic.
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Test4(context, ref renderingData);
    }

    public void SetRenderSetting(ref RenderingData renderingData, Camera camera)
    {
        SortingCriteria sortingCriteria = (m_RenderQueueType == RenderQueueType.Transparent) ?
        SortingCriteria.CommonTransparent :
        renderingData.cameraData.defaultOpaqueSortFlags;

        //m_DrawingSetting = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

        //@@@Drawing setting
        SortingSettings sortingSettings = new SortingSettings(camera)
        {
            criteria = sortingCriteria
        };

        m_DrawingSetting = new DrawingSettings(m_ShaderTagIdList[0], sortingSettings)
        {
            perObjectData = renderingData.perObjectData,
            mainLightIndex = renderingData.lightData.mainLightIndex,
            enableDynamicBatching = renderingData.supportsDynamicBatching,

            enableInstancing = camera.cameraType == CameraType.Preview ? false : true,
        };
        for (int i = 0; i < m_ShaderTagIdList.Count; ++i)
        {
            m_DrawingSetting.SetShaderPassName(i, m_ShaderTagIdList[i]);
        }

        m_DrawingSetting.overrideMaterial = overrideMaterial;
        m_DrawingSetting.overrideMaterialPassIndex = overrideMaterialPassIndex;

        //@@@RenderStateBlock
        if (overrideDepthState)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(this.enableWrite, this.depthCompareFunction);
        }

        if (stencilSettings.overrideStencilState)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(stencilSettings.stencilCompareFunction);
            stencilState.SetPassOperation(stencilSettings.passOperation);
            stencilState.SetFailOperation(stencilSettings.failOperation);
            stencilState.SetZFailOperation(stencilSettings.zFailOperation);

            m_RenderStateBlock.mask |= RenderStateMask.Stencil;
            m_RenderStateBlock.stencilReference = stencilSettings.stencilReference;
            m_RenderStateBlock.stencilState = stencilState;
        }
    }

    //SetRenderTarget-MipSelectionTest @@@OnCameraSetup ConfigureTarget
    public void Test4(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        ref CameraData cameraData = ref renderingData.cameraData;
        Rect pixelRect = camera.pixelRect;
        float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

        SetRenderSetting(ref renderingData, camera);


        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            if (m_CameraSettings.overrideCamera)
            {
                Matrix4x4 projectionMat = Matrix4x4.Perspective(
                    m_CameraSettings.cameraFieldOfView, cameraAspect,
                    camera.nearClipPlane, camera.farClipPlane);
                projectionMat = GL.GetGPUProjectionMatrix(projectionMat, cameraData.IsCameraProjectionMatrixFlipped());

                Matrix4x4 viewMat = cameraData.GetViewMatrix();
                Vector4 cameraTranslation = viewMat.GetColumn(3);

                viewMat.SetColumn(3, cameraTranslation + m_CameraSettings.offset);

                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, viewMat, projectionMat, false);
            }
            RenderTargetIdentifier renderTargetIdentifier = new RenderTargetIdentifier(mipTexture, 1, CubemapFace.Unknown, 0);
            //渲染目标使用mipTexture的Mip=1级别
            commandBuffer.SetRenderTarget(renderTargetIdentifier);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting, ref m_RenderStateBlock);

            //渲染目标override使用mipTexture的Mip=2级别
            commandBuffer.SetRenderTarget(renderTargetIdentifier, 2, CubemapFace.Unknown);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting1, ref m_RenderStateBlock);


            if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.xrRendering)
            {
                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
            }

        }
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
    }

    //SetRenderTargets- multi-MipSelectionTest
    public void Test5(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        ref CameraData cameraData = ref renderingData.cameraData;
        Rect pixelRect = camera.pixelRect;
        float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

        SetRenderSetting(ref renderingData, camera);

        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            if (m_CameraSettings.overrideCamera)
            {
                Matrix4x4 projectionMat = Matrix4x4.Perspective(
                    m_CameraSettings.cameraFieldOfView, cameraAspect,
                    camera.nearClipPlane, camera.farClipPlane);
                projectionMat = GL.GetGPUProjectionMatrix(projectionMat, cameraData.IsCameraProjectionMatrixFlipped());

                Matrix4x4 viewMat = cameraData.GetViewMatrix();
                Vector4 cameraTranslation = viewMat.GetColumn(3);

                viewMat.SetColumn(3, cameraTranslation + m_CameraSettings.offset);

                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, viewMat, projectionMat, false);
            }

            //@@@全部重置为0,CubemapFace.Unknown,0
            commandBuffer.SetRenderTarget(renderTargetIdentifiers, renderTargetDepthIdentifier);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting, ref m_RenderStateBlock);

            commandBuffer.SetRenderTarget(renderTargetIdentifiers, renderTargetDepthIdentifier, 1, CubemapFace.Unknown, 0);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting1, ref m_RenderStateBlock);


            if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.xrRendering)
            {
                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
            }

        }
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
    }

    //SetRenderTargets-Multiple Render Target Store/Load Action
    public void Test6(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        ref CameraData cameraData = ref renderingData.cameraData;
        Rect pixelRect = camera.pixelRect;
        float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

        SetRenderSetting(ref renderingData, camera);

        #region [Multiple Render Target Store/Load Action]
        //ColorLoadActions
        m_ColorLoadAction[0] = RenderBufferLoadAction.DontCare;
        m_ColorLoadAction[1] = RenderBufferLoadAction.Load;
        m_ColorLoadAction[2] = RenderBufferLoadAction.DontCare;
        //ColorStoreActions
        m_ColorStoreAction[0] = RenderBufferStoreAction.DontCare;
        m_ColorStoreAction[1] = RenderBufferStoreAction.Store;
        m_ColorStoreAction[2] = RenderBufferStoreAction.Store;

        //RenderTargetBinding
        RenderTargetBinding m_RenderTargetBinding = new RenderTargetBinding(renderTargetIdentifiers, m_ColorLoadAction, m_ColorStoreAction,//ColorLoad/StoreAction
        renderTargetDepthIdentifier, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);//DepthLoad/StoreAction
        #endregion

        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            if (m_CameraSettings.overrideCamera)
            {
                Matrix4x4 projectionMat = Matrix4x4.Perspective(
                    m_CameraSettings.cameraFieldOfView, cameraAspect,
                    camera.nearClipPlane, camera.farClipPlane);
                projectionMat = GL.GetGPUProjectionMatrix(projectionMat, cameraData.IsCameraProjectionMatrixFlipped());

                Matrix4x4 viewMat = cameraData.GetViewMatrix();
                Vector4 cameraTranslation = viewMat.GetColumn(3);

                viewMat.SetColumn(3, cameraTranslation + m_CameraSettings.offset);

                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, viewMat, projectionMat, false);
            }

            //commandBuffer.SetRenderTarget(m_RenderTargetBinding);//全部重置为0,CubemapFace.Unknown,0
            commandBuffer.SetRenderTarget(m_RenderTargetBinding, 1, CubemapFace.Unknown, 0);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting, ref m_RenderStateBlock);

            // commandBuffer.ClearRenderTarget(true, true, Color.blue, 1);//@@@this command will overload the Load/Store Action

            commandBuffer.SetRenderTarget(renderTargetIdentifiers, renderTargetDepthIdentifier, 0, CubemapFace.Unknown, 0);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting1, ref m_RenderStateBlock);
            // commandBuffer.ClearRenderTarget(true, true, Color.white, 1);//@@@this command will overload the Load/Store Action

            if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.xrRendering)
            {
                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
            }
        }
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
    }

    //SetRenderTargets-MultipleSample MultipleRenderTarget Mip to use
    public void Test7(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        ref CameraData cameraData = ref renderingData.cameraData;
        Rect pixelRect = camera.pixelRect;
        float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

        SetRenderSetting(ref renderingData, camera);

        #region [Multiple Render Target Store/Load Action]
        //ColorLoadActions
        m_ColorLoadAction[0] = RenderBufferLoadAction.DontCare;
        m_ColorLoadAction[1] = RenderBufferLoadAction.DontCare;
        m_ColorLoadAction[2] = RenderBufferLoadAction.DontCare;
        //ColorStoreActions
        m_ColorStoreAction[0] = RenderBufferStoreAction.Store;
        m_ColorStoreAction[1] = RenderBufferStoreAction.Store;
        m_ColorStoreAction[2] = RenderBufferStoreAction.Store;


        //RenderTargetBinding
        RenderTargetBinding m_RenderTargetBinding = new RenderTargetBinding(tempRenderTargetIdentifiers, m_ColorLoadAction, m_ColorStoreAction,//ColorLoad/StoreAction
        tempRenderTargetDepthIdentifier, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);//DepthLoad/StoreAction

        #endregion

        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            if (m_CameraSettings.overrideCamera)
            {
                Matrix4x4 projectionMat = Matrix4x4.Perspective(
                    m_CameraSettings.cameraFieldOfView, cameraAspect,
                    camera.nearClipPlane, camera.farClipPlane);
                projectionMat = GL.GetGPUProjectionMatrix(projectionMat, cameraData.IsCameraProjectionMatrixFlipped());

                Matrix4x4 viewMat = cameraData.GetViewMatrix();
                Vector4 cameraTranslation = viewMat.GetColumn(3);

                viewMat.SetColumn(3, cameraTranslation + m_CameraSettings.offset);

                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, viewMat, projectionMat, false);
            }

            //commandBuffer.SetRenderTarget(m_RenderTargetBinding);//全部重置为0,CubemapFace.Unknown,0
            //@@@当RenderTexture为Mulit_Sample类型(antiAliasing>1),RenderTarget指定的Mip必须为0[https://www.zhihu.com/question/479915187]
            commandBuffer.SetRenderTarget(m_RenderTargetBinding, 0, CubemapFace.Unknown, 0);
            commandBuffer.ClearRenderTarget(true, true, Color.black, 1);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            if (mipTextures[0].IsCreated())
                mipTextures[0].GenerateMips();
            if (mipTextures[1].IsCreated())
                mipTextures[1].GenerateMips();
            if (mipTextures[2].IsCreated())
                mipTextures[2].GenerateMips();

            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting, ref m_RenderStateBlock);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            //mipTextures[0].ResolveAntiAliasedSurface(); commandBuffer.ResolveAntiAliasedSurface(不能自己resolve自己....)
            //Resolve
            for (int i = 0; i < tempResolveRenderTexture.Length; i++)
            {
                commandBuffer.ResolveAntiAliasedSurface(tempResolveRenderTexture[i], mipTextures[i]);
            }

            if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.xrRendering)
            {
                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
            }
        }
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
    }

    // Cleanup any allocated resources that were created during the execution of this render pass.
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(renderTextureID);
    }
}