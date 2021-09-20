using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class RenderInstancesProceduralPass : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler;
    string m_ProfilerTag = "DrawMesh";
    private ComputeBuffer particleBuffer;
    private ComputeBuffer bufferWithArgs;
    public ComputeShader GPUProceduralCS;
    public Material instanceMaterial;
    public Mesh instanceMesh;
    const int MaxResolution = 300;
    public uint resolution;


    //Shader properties ID
    static readonly int positionId = Shader.PropertyToID("_Positions"),
    resolutionId = Shader.PropertyToID("_Resolution"),
    stepId = Shader.PropertyToID("_Step"),
    timeId = Shader.PropertyToID("_Time");


    public RenderInstancesProceduralPass(ComputeShader GPUProceduralCS, Material instanceMaterial, Mesh instanceMesh, ComputeBuffer positionBuffer, ComputeBuffer bufferWithArgs, uint resolution)
    {
        this.GPUProceduralCS = GPUProceduralCS;
        this.instanceMaterial = instanceMaterial;
        this.instanceMesh = instanceMesh;
        this.particleBuffer = positionBuffer;
        this.bufferWithArgs = bufferWithArgs;
        this.resolution = resolution;
        m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        UpdateFunctionOnGPU(context, ref renderingData);
    }

    void UpdateFunctionOnGPU(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        float step = 2f / resolution;
        GPUProceduralCS.SetInt(resolutionId, ((int)resolution));
        GPUProceduralCS.SetFloat(stepId, step);

#if UNITY_EDITOR
        float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        GPUProceduralCS.SetFloat(timeId, time);
#else
        float time = Time.time;
        GPUProceduralCS.SetFloat(timeId, time);
#endif

        GPUProceduralCS.SetBuffer(0, positionId, particleBuffer);
        int groups = Mathf.CeilToInt(resolution / 8f);
        GPUProceduralCS.Dispatch(0, groups, groups, 1);

        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            MaterialPropertyBlock m_MatBlock = new MaterialPropertyBlock();
            m_MatBlock.SetBuffer(positionId, particleBuffer);
            m_MatBlock.SetFloat(stepId, step);
            //Instance绘制
            //commandBuffer.DrawProcedural(Matrix4x4.identity, instanceMaterial, 0, MeshTopology.Points,((int)resolution) * ((int)resolution) * 4, 1, m_MatBlock);
            //commandBuffer.DrawProcedural(Matrix4x4.identity, instanceMaterial, 0, MeshTopology.Lines, ((int)resolution) * ((int)resolution) * 4, 1, m_MatBlock);
            //commandBuffer.DrawProcedural(Matrix4x4.identity, instanceMaterial, 0, MeshTopology.LineStrip, ((int)resolution) * ((int)resolution)* 4, 1, m_MatBlock);
            //commandBuffer.DrawProcedural(Matrix4x4.identity, instanceMaterial, 0, MeshTopology.Triangles, ((int)resolution) * ((int)resolution)* 4, 1, m_MatBlock);
            commandBuffer.DrawProcedural(Matrix4x4.identity, instanceMaterial, 0, MeshTopology.Quads, ((int)resolution) * ((int)resolution) * 4, 1, m_MatBlock);

            //commandBuffer.DrawProceduralIndirect(Matrix4x4.identity, instanceMaterial, 0, MeshTopology.Lines, bufferWithArgs, 0, m_MatBlock);//Quad会报错,Quad不支持使用BufferArgs控制
        }

        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
        CommandBufferPool.Release(commandBuffer);
    }


    // Cleanup any allocated resources that were created during the execution of this render pass.
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
    }

    private void OnDisable()
    {

    }
}
