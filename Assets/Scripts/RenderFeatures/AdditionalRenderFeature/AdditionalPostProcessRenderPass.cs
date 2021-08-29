using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal.Internal
{
    class AdditionalPostProcessRenderPass : ScriptableRenderPass
    {
        CustomChromaticAberration m_Chromatic;
        //RenderFeature MaterialLib 演示对比用
        static MaterialLibrary m_Material = null;
        Material materialBlockTest_Material;
        public Material FXAAMat;
        Mesh materialBlockTest_Mesh;



        AdditionalPostProcessData m_Data;

        //Input RT
        ScriptableRenderer renderer;
        RenderTargetIdentifier input_ColorAttachment;
        RenderTargetIdentifier input_CameraDepthAttachment;

        //ChromaticAberration LUT
        RenderTexture renderTexture = new RenderTexture(3, 1, 0);
        RenderTexture ChromaticTest;
        Texture2D AberrationLUT = new Texture2D(3, 1);
        public RenderTargetHandle aberrationTex;
        public RenderTargetHandle afterPostProcessTexture;


        //Final Output RT
        RenderTargetIdentifier output_Destination;

        const string RenderPostProcessingTag = "Render AdditionalPostProcessing Effects";
        const string RenderFinalPostProcessingTag = "Render Final AdditionalPostProcessing Effects";


        public AdditionalPostProcessRenderPass(RenderPassEvent @event, AdditionalPostProcessData data)
        {
            renderPassEvent = @event;
            m_Data = data;

            //RenderFeature MaterialLib 演示对比用
            //m_Material = new MaterialLibrary(data);

            AberrationLUT.SetPixel(0, 0, Color.red);
            AberrationLUT.SetPixel(1, 0, Color.green);
            AberrationLUT.SetPixel(2, 0, Color.blue);

            AberrationLUT.filterMode = FilterMode.Bilinear;
            AberrationLUT.Apply();

            aberrationTex.Init("passChromaticAberrationRT");
            

            materialBlockTest_Material = CoreUtils.CreateEngineMaterial("Unlit/UnlitTest");
            materialBlockTest_Mesh = CoreUtils.CreateCubeMesh(new Vector3(0, 0, 0), new Vector3(1, 1, 1));


        }

        public void Setup(ref ScriptableRenderer renderer)
        {

            this.renderer = renderer;
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var stack = VolumeManager.instance.stack;

            m_Chromatic = stack.GetComponent<CustomChromaticAberration>();

            CommandBuffer commandBuffer = CommandBufferPool.Get(RenderPostProcessingTag);

            Render(commandBuffer, ref renderingData);

            context.ExecuteCommandBuffer(commandBuffer);

            CommandBufferPool.Release(commandBuffer);
        }

        void Render(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            ref var CameraData = ref renderingData.cameraData;
            //SetupDrawMesh(commandBuffer, ref renderingData);
            SetupFXAA(commandBuffer, ref renderingData);
            //RenderFeature MaterialLib 演示对比用
            // if (m_Chromatic.IsActive() && !CameraData.isSceneViewCamera)
            // {
            //     SetupChromaticAberration(commandBuffer, ref renderingData);
            // }
        }

        //RenderFeature MaterialLib 演示对比用
        public void SetupChromaticAberration(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            RenderTextureDescriptor opaqueDes = renderingData.cameraData.cameraTargetDescriptor;

            Camera camera = renderingData.cameraData.camera;
            camera.forceIntoRenderTexture = true;

            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetTexture(Shader.PropertyToID("_AberrationLUT"), AberrationLUT);
            materialPropertyBlock.SetFloat("_Intensity", m_Chromatic.Intensity.value);

            // material.SetTexture(Shader.PropertyToID("_AberrationLUT"), AberrationLUT);
            // material.SetFloat(Shader.PropertyToID("_Intensity"), m_Chromatic.Intensity.value);

            commandBuffer.GetTemporaryRT(aberrationTex.id, opaqueDes);

            commandBuffer.BeginSample("ChromaticAberration");

            commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            commandBuffer.SetViewport(renderingData.cameraData.camera.pixelRect);

            // commandBuffer.SetRenderTarget(renderer.cameraColorTarget,
            // RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            // renderer.cameraDepthTarget,
            // RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            commandBuffer.SetRenderTarget(aberrationTex.id);

            //@@@Remove ResolveAA
            //commandBuffer.DrawProcedural(Matrix4x4.identity, m_Material.ChromaticMat, 0, MeshTopology.Quads, 4, 1, null);
            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material.ChromaticMat, 0, 0, materialPropertyBlock);

            commandBuffer.Blit(aberrationTex.id, renderer.cameraColorTarget);
            // commandBuffer.Blit(renderer.cameraColorTarget, aberrationTex.id, m_Material.ChromaticMat, 0);
            // commandBuffer.Blit(renderer.cameraColorTarget, renderer.cameraColorTarget, m_Material.ChromaticMat, 0);
            commandBuffer.EndSample("ChromaticAberration");
        }
        //RenderFeature MaterialLib 演示对比用
        public void SetupChromaticAberration_Blit(CommandBuffer commandBuffer, ref RenderingData renderingData, Material material)
        {
            RenderTextureDescriptor opaqueDes = renderingData.cameraData.cameraTargetDescriptor;

            Camera camera = renderingData.cameraData.camera;
            camera.forceIntoRenderTexture = true;

            m_Material.ChromaticMat.SetTexture("_AberrationLUT", AberrationLUT);
            m_Material.ChromaticMat.SetFloat("_Intensity", m_Chromatic.Intensity.value);

            commandBuffer.GetTemporaryRT(aberrationTex.id, opaqueDes);

            commandBuffer.BeginSample("ChromaticAberration");

            commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            commandBuffer.SetViewport(renderingData.cameraData.camera.pixelRect);

            commandBuffer.SetRenderTarget(aberrationTex.id);

            //@@@Remove ResolveAA
            //commandBuffer.DrawProcedural(Matrix4x4.identity, m_Material.ChromaticMat, 0, MeshTopology.Quads, 4, 1, null);
            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material.ChromaticMat, 0, 0);

            commandBuffer.Blit(aberrationTex.id, renderer.cameraColorTarget);


            commandBuffer.EndSample("ChromaticAberration");
        }

        public void SetupDrawMesh(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            commandBuffer.BeginSample("DrawMesh_MaterialBlockTest");

            //commandBuffer.SetGlobalColor(Shader.PropertyToID("_TestColor"), Color.blue);
            //Shader.SetGlobalColor("_TestColor", Color.blue);
            //materialBlockTest_Material.SetColor(Shader.PropertyToID("_TestColor"), Color.blue);
            commandBuffer.DrawMesh(materialBlockTest_Mesh, Matrix4x4.identity, materialBlockTest_Material, 0, 0, null);
            commandBuffer.DrawMesh(materialBlockTest_Mesh, Matrix4x4.identity, materialBlockTest_Material, 0, 0, null);

            MaterialPropertyBlock materialPropertyBlock0 = new MaterialPropertyBlock();
            materialPropertyBlock0.SetColor(Shader.PropertyToID("_TestColor"), Color.red);
            commandBuffer.DrawMesh(materialBlockTest_Mesh, Matrix4x4.identity, materialBlockTest_Material, 0, 0, materialPropertyBlock0);
            //多余实例化材质对比测试
            MaterialPropertyBlock materialPropertyBlock1 = new MaterialPropertyBlock();
            materialPropertyBlock1.SetColor(Shader.PropertyToID("_TestColor"), Color.green);
            commandBuffer.DrawMesh(materialBlockTest_Mesh, Matrix4x4.identity, materialBlockTest_Material, 0, 0, materialPropertyBlock1);

            //commandBuffer.SetGlobalColor(Shader.PropertyToID("_TestColor"), Color.yellow);
            //Shader.SetGlobalColor("_TestColor", Color.yellow);
            materialBlockTest_Material.SetColor(Shader.PropertyToID("_TestColor"), Color.yellow);
            commandBuffer.DrawMesh(materialBlockTest_Mesh, Matrix4x4.identity, materialBlockTest_Material, 0, 0, null);
            //1.对比测试说明Material.setColor优先级比cmd要高    Material>cmd
            //2.对比测试说明cmd优先级比shader要高               cmd->shader cmd(win)   shader->cmd cmd(win) ∴cmd>shader
            //3.Shader.SetGlobalColor/Material.SetColor会因为生命周期与CommandBuffer不一致导致指令被覆盖[生命周期我瞎猜的]
            //4.CommandBuffer.SetGlobalColor不存在指令被覆盖问题
            //5.materialPropertyBlock不会存在上诉问题，终极解决方法
            //6.材质实例化问题:materialPropertyBlock是不会额外创建出多余的材质，而其他的materialBlockTest_Material.SetColor是存在创建多余的材质的问题。
            //7.材质实例化问题：刚才可以看到，但凡修改完代码之后，这个pass里的material都不会被释放，所以不建议在renderfeature里生成material，还是直接在外面Project里实例化一个让RenderFeature引用
            commandBuffer.EndSample("DrawMesh_MaterialBlockTest");
        }

        public void SetupFXAA(CommandBuffer commandBuffer, ref RenderingData renderingData)
        {
            RenderTextureDescriptor opaqueDes = renderingData.cameraData.cameraTargetDescriptor;

            Camera camera = renderingData.cameraData.camera;
            commandBuffer.GetTemporaryRT(aberrationTex.id, opaqueDes);
            float width = opaqueDes.width;
            float height = opaqueDes.height;

            commandBuffer.BeginSample("FXAA");

            commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            commandBuffer.SetViewport(renderingData.cameraData.camera.pixelRect);

            commandBuffer.SetRenderTarget(aberrationTex.id);
            commandBuffer.SetGlobalVector(Shader.PropertyToID("_SourceSize"), new Vector4(width, height, 1.0f / width, 1.0f / height));
    
            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, FXAAMat, 0, 0);

            commandBuffer.Blit(aberrationTex.id, renderer.cameraColorTarget);
            commandBuffer.EndSample("FXAA");
        }


        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(aberrationTex.id);

            base.FrameCleanup(cmd);
        }


        public void Cleanup() => CoreUtils.Destroy(materialBlockTest_Material);

        #region Internal utilities
        class MaterialLibrary
        {
            public readonly Material ChromaticMat;
            public MaterialLibrary(AdditionalPostProcessData data)
            {
                ChromaticMat = Load(data.shaders.ChromaticShader);
            }

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                    return null;
                }
                else if (!shader.isSupported)
                {
                    return null;
                }
                return CoreUtils.CreateEngineMaterial(shader);
            }

            internal void Cleanup()
            {
                CoreUtils.Destroy(ChromaticMat);
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }

}