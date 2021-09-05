using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class DrawRenderersPass : ScriptableRenderPass
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
    //Test0:CullingResults+DrawingSettings+FilteringSettings
    CullingResults m_CullingResult;
    DrawingSettings m_DrawingSetting;
    FilteringSettings m_FilteringSetting;


    //Test1:Override RenderStateBlock
    //Depth state
    bool overrideDepthState;
    bool enableWrite;
    CompareFunction depthCompareFunction;
    //Stencil state
    StencilStateData stencilSettings;
    RenderStateBlock m_RenderStateBlock;
    NativeArray<RenderStateBlock> m_RenderStateBlocks;

    public DrawRenderersPass(string profilerTag, RenderPassEvent renderPassEvent, RenderQueueType renderQueueType, int layerMask, string[] shaderTags,
    Material overrideMaterial, int overrideMaterialPassIndex,
    bool overrideDepthState, bool enableWrite, CompareFunction depthCompareFunction,
    StencilStateData stencilSettings,
    CustomCameraSettings cameraSettings)
    {
        this.m_ProfilingSampler = new ProfilingSampler(profilerTag);
        this.m_ProfilerTag = profilerTag;

        this.renderPassEvent = renderPassEvent;
        this.m_RenderQueueType = renderQueueType;

        //shaderTags数组转成 List<ShaderTagId> 
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

        RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent) ? RenderQueueRange.transparent : RenderQueueRange.opaque;
        //@@@Filtering Setting  
        //1.renderQueueRange渲染队列：不透明队列还是透明队列过滤  
        //2.layerMask Layer层级过滤
        this.m_FilteringSetting = new FilteringSettings(renderQueueRange, layerMask);

        this.overrideMaterial = overrideMaterial;
        this.overrideMaterialPassIndex = overrideMaterialPassIndex;


        //@@@RenderBlockState:Depth
        this.overrideDepthState = overrideDepthState;
        this.enableWrite = enableWrite;
        this.depthCompareFunction = depthCompareFunction;

        //@@@RenderBlockState:Stencil
        this.stencilSettings = stencilSettings;

        m_CameraSettings = cameraSettings;

        m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
    }

    // Here you can implement the rendering logic.
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Test3(context, ref renderingData);
    }

    //Test0:CullingResults+DrawingSettings+FilteringSettings
    public void Test0(ScriptableRenderContext context, ref RenderingData renderingData)
    {

        Camera camera = renderingData.cameraData.camera;
        ref CameraData cameraData = ref renderingData.cameraData;
        Rect pixelRect = camera.pixelRect;
        float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

        //@@@Drawing setting 渲染排序顺序
        SortingCriteria sortingCriteria = (m_RenderQueueType == RenderQueueType.Transparent) ?
        SortingCriteria.CommonTransparent :
        renderingData.cameraData.defaultOpaqueSortFlags;
        SortingSettings sortingSettings = new SortingSettings(camera)
        {
            criteria = sortingCriteria
        };

        //
        //等价于
        //m_DrawingSetting = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
        m_DrawingSetting = new DrawingSettings(m_ShaderTagIdList[0], sortingSettings)
        {
            perObjectData = renderingData.perObjectData,
            mainLightIndex = renderingData.lightData.mainLightIndex,
            enableDynamicBatching = renderingData.supportsDynamicBatching,

            enableInstancing = camera.cameraType == CameraType.Preview ? false : true,
        };
        for (int i = 1; i < m_ShaderTagIdList.Count; ++i)
        {
            m_DrawingSetting.SetShaderPassName(i, m_ShaderTagIdList[i]);
        }
        //Debug.Log(((string)m_DrawingSetting.GetShaderPassName(0)));
        m_DrawingSetting.overrideMaterial = overrideMaterial;
        m_DrawingSetting.overrideMaterialPassIndex = overrideMaterialPassIndex;

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

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting, ref m_RenderStateBlock);

            if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.xrRendering)
            {
                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
            }
        }
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);

    }

    //Test1:Override RenderStateBlock
    public void Test1(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        SortingCriteria sortingCriteria = (m_RenderQueueType == RenderQueueType.Transparent) ?
SortingCriteria.CommonTransparent :
renderingData.cameraData.defaultOpaqueSortFlags;

        //m_DrawingSetting = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
        Camera camera = renderingData.cameraData.camera;
        ref CameraData cameraData = ref renderingData.cameraData;
        Rect pixelRect = camera.pixelRect;
        float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

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


            // commandBuffer.SetRenderTarget(renderTextureID,
            // RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            // RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting, ref m_RenderStateBlock);

            //commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,BuiltinRenderTextureType.CameraTarget);
            if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.xrRendering)
            {
                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
            }
        }
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
    }

    //Test2: Match Subshader RenderType Tags
    public void Test2(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        SortingCriteria sortingCriteria = (m_RenderQueueType == RenderQueueType.Transparent) ?
        SortingCriteria.CommonTransparent :
        renderingData.cameraData.defaultOpaqueSortFlags;

        //m_DrawingSetting = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
        Camera camera = renderingData.cameraData.camera;
        ref CameraData cameraData = ref renderingData.cameraData;
        Rect pixelRect = camera.pixelRect;
        float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

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

        //@@@Custom RenderStateBlock
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

        //@@@Default RenderStateBlock
        RenderStateBlock defaultRenderStateBlock = new RenderStateBlock(RenderStateMask.Depth);
        defaultRenderStateBlock.depthState = new DepthState(true, CompareFunction.Always);

        m_RenderStateBlocks = new NativeArray<RenderStateBlock>(2, Allocator.Temp);
        m_RenderStateBlocks[0] = m_RenderStateBlock;
        m_RenderStateBlocks[1] = defaultRenderStateBlock;


        //@@@ShaderTagId
        ShaderTagId renderType = new ShaderTagId("Opaque");
        ShaderTagId fallBack = new ShaderTagId();//Catch all
        NativeArray<ShaderTagId> renderTypes = new NativeArray<ShaderTagId>(2, Allocator.Temp);
        renderTypes[0] = renderType;
        renderTypes[1] = fallBack;
        //测试内容:
        //更改Shader中的RenderType，改为"Opaque0",再改回去，说明renderType=Opaque时,使用m_RenderStateBlock
        //所以我们可以根据RenderType去批量指定RenderStateBlock

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

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            //@@@Only work for RenderType Tag
            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting, renderTypes, m_RenderStateBlocks);

            if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.xrRendering)
            {
                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
            }
        }

        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
        m_RenderStateBlocks.Dispose();
        renderTypes.Dispose();
    }

    //Test3: Match Subshader or Pass’s TagName+TagVlaues 
    public void Test3(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        SortingCriteria sortingCriteria = (m_RenderQueueType == RenderQueueType.Transparent) ?
SortingCriteria.CommonTransparent :
renderingData.cameraData.defaultOpaqueSortFlags;

        //m_DrawingSetting = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
        Camera camera = renderingData.cameraData.camera;
        ref CameraData cameraData = ref renderingData.cameraData;
        Rect pixelRect = camera.pixelRect;
        float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

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

        //@@@Custom RenderStateBlock
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

        //@@@Default RenderStateBlock
        RenderStateBlock defaultRenderStateBlock = new RenderStateBlock(RenderStateMask.Depth);
        defaultRenderStateBlock.depthState = new DepthState(true, CompareFunction.Always);

        m_RenderStateBlocks = new NativeArray<RenderStateBlock>(2, Allocator.Temp);
        m_RenderStateBlocks[0] = m_RenderStateBlock;
        m_RenderStateBlocks[1] = defaultRenderStateBlock;


        //@@@ShaderTagId
        ShaderTagId tagName = new ShaderTagId("LightMode");
        bool isPassTagName = true;

        ShaderTagId renderType = new ShaderTagId("UniversalForward");
        ShaderTagId fallBack = new ShaderTagId();
        NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(2, Allocator.Temp);
        tagValues[0] = renderType;
        tagValues[1] = fallBack;


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

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            //@@@Not Only work for RenderType Tag
            context.DrawRenderers(renderingData.cullResults, ref m_DrawingSetting, ref m_FilteringSetting, tagName, isPassTagName, tagValues, m_RenderStateBlocks);

            if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.xrRendering)
            {
                RenderingUtils.SetViewAndProjectionMatrices(commandBuffer, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
            }
        }

        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
        m_RenderStateBlocks.Dispose();
        tagValues.Dispose();
    }

    // Cleanup any allocated resources that were created during the execution of this render pass.
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
    }
}