using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public enum RenderQueueType
{
    Opaque,
    Transparent,
}
[System.Serializable]
public class CustomCameraSettings
{
    public bool overrideCamera = false;
    public bool restoreCamera = true;
    public Vector4 offset;
    public float cameraFieldOfView = 60.0f;
}
public class RenderObjectsFeature : ScriptableRendererFeature
{
    SetRenderTargetPass m_SetRenderTargetPass;
    DrawRenderersPass m_DrawRendererPass;
    //@@@RenderFeature 设置
    //Event
    public RenderPassEvent Event;

    //-------Filter Setting-------
    public RenderQueueType m_RenderQueueType;
    public LayerMask m_LayerMask;
    public LayerMask m_LayerMask1;
    public string[] PassNames;
    //----------------------------

    //-------Render State Block-------
    public Material override_Material;
    public int overrideMaterialPassIndex = 0;
    //Depth
    public bool overriderDepthState = false;
    public bool enableWrite = true;
    public CompareFunction depthCompareFunction = CompareFunction.LessEqual;
    //Stencil
    public StencilStateData stencilSettings = new StencilStateData();
    /*
    public class StencilStateData
    {
        public bool overrideStencilState = false;
        public int stencilReference = 0;
        public CompareFunction stencilCompareFunction = CompareFunction.Always;
        public StencilOp passOperation = StencilOp.Keep;
        public StencilOp failOperation = StencilOp.Keep;
        public StencilOp zFailOperation = StencilOp.Keep;
    }
    */

    //Camera
    public CustomCameraSettings cameraSettings = new CustomCameraSettings();

    //--------------------------------
    public override void Create()
    {
        //如果需要当前CameraColor的Id需要把Renderer传进去
        m_SetRenderTargetPass = new SetRenderTargetPass(this.name, Event, m_RenderQueueType, m_LayerMask, m_LayerMask1, PassNames,
        override_Material, overrideMaterialPassIndex,
        overriderDepthState, enableWrite, depthCompareFunction,
        this.stencilSettings,
        cameraSettings);

        //如果需要当前CameraColor的Id需要把Renderer传进去 Override material使用Lit_RenderStateBlockTest测试[直接搬的URP的Lit]
        m_DrawRendererPass = new DrawRenderersPass(this.name, Event, m_RenderQueueType, m_LayerMask, PassNames,
        override_Material, overrideMaterialPassIndex,
        overriderDepthState, enableWrite, depthCompareFunction,
        this.stencilSettings,
        cameraSettings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_SetRenderTargetPass);
        //renderer.EnqueuePass(m_DrawRendererPass);
    }
}


