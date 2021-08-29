using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace UnityEngine.Rendering.Universal.Internal
{
    public class AdditionalPostProcessRenderFeature : ScriptableRendererFeature
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;

        public AdditionalPostProcessData postProcessData;
        public Material FXAAMat;

        AdditionalPostProcessRenderPass postProcessRenderPass = null;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (postProcessData == null)
                return;
            //RenderTargetIdentifier cameraDepthTarget Warning
            postProcessRenderPass.Setup(ref renderer);
            renderer.EnqueuePass(postProcessRenderPass);
        }

        public override void Create()
        {
            postProcessRenderPass = new AdditionalPostProcessRenderPass( Event,postProcessData);
            postProcessRenderPass.FXAAMat=FXAAMat;
        }
        protected override void Dispose(bool disposing)
        {
            postProcessRenderPass.Cleanup(); 
        }

        
    }


}

