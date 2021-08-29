using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class RenderInstancesIndirectPass : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler;
    string m_ProfilerTag = "DrawMesh";
    private ComputeBuffer positionBuffer;
    private ComputeBuffer bufferWithArgs;
    public ComputeShader GPUComputeShader;
    public Material instanceMaterial;
    public Mesh instanceMesh;
    const int MaxResolution = 300;
    public uint resolution;

    //Shader properties ID
    static readonly int positionId = Shader.PropertyToID("_Positions"),
    resolutionId = Shader.PropertyToID("_Resolution"),
    stepId = Shader.PropertyToID("_Step"),
    timeId = Shader.PropertyToID("_Time");


    public RenderInstancesIndirectPass(ComputeShader GPUComputeShader, Material instanceMaterial, Mesh instanceMesh, ComputeBuffer positionBuffer, ComputeBuffer bufferWithArgs, uint resolution)
    {
        this.GPUComputeShader = GPUComputeShader;
        this.instanceMaterial = instanceMaterial;
        this.instanceMesh = instanceMesh;
        this.positionBuffer = positionBuffer;
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
        GPUComputeShader.SetInt(resolutionId, ((int)resolution));
        GPUComputeShader.SetFloat(stepId, step);

#if UNITY_EDITOR
        float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        GPUComputeShader.SetFloat(timeId, time);
#else
        float time = Time.time;
        GPUComputeShader.SetFloat(timeId, time);
#endif

        GPUComputeShader.SetBuffer(0, positionId, positionBuffer);
        int groups = Mathf.CeilToInt(resolution / 8f);
        GPUComputeShader.Dispatch(0, groups, groups, 1);

        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            MaterialPropertyBlock m_MatBlock = new MaterialPropertyBlock();
            m_MatBlock.SetBuffer(positionId, positionBuffer);
            m_MatBlock.SetFloat(stepId, step);

            var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
            commandBuffer.DrawMeshInstancedIndirect(instanceMesh, 0, instanceMaterial, 0, bufferWithArgs, 0, m_MatBlock);
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