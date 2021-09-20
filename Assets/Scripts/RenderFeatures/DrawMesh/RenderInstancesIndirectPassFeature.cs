using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using UnityEngine.Scripting.APIUpdating;
//Draw Command Indirect test
public class RenderInstancesIndirectPassFeature : ScriptableRendererFeature
{
    public ComputeShader GPUComputeShader;
    public ComputeShader GPUProceduralCS;
    public Material InstanceMaterial;
    public Material ProceduralMaterial;
    public Mesh InstanceMesh;

    const int MaxResolution = 288;
    [Range(16, MaxResolution)]
    public uint resolution = 16;
    RenderInstancesIndirectPass m_IndirectPass;
    RenderInstancesProceduralPass m_ProceduralPass;
    private static ComputeBuffer positionBuffer;
    private static ComputeBuffer particleBuffer;

    private static ComputeBuffer bufferWithArgs_Indirect;
    private static ComputeBuffer bufferWithArgs_Procedural;
    public bool isInstancesIndirect;
    public override void Create()
    {
        // positionBuffer.Release();
        // particleBuffer.Release();
        // bufferWithArgs_Indirect.Release();
        // bufferWithArgs_Procedural.Release();

        resolution = (uint)(resolution / 16) * 16;
        SetUpIndirectPass();
        SetUpProceduralPass();
    }

    void SetUpIndirectPass()
    {
        if (isActive)
        {
            if (positionBuffer == null)
                positionBuffer = new ComputeBuffer(MaxResolution * MaxResolution, 3 * 4);
            if (bufferWithArgs_Indirect == null)
                bufferWithArgs_Indirect = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            else
            if (InstanceMesh != null)
                bufferWithArgs_Indirect.SetData(new uint[] { InstanceMesh.GetIndexCount(0), resolution * resolution, 0, 0, 0 });
        }

        m_IndirectPass = new RenderInstancesIndirectPass(GPUComputeShader, InstanceMaterial, InstanceMesh, positionBuffer, bufferWithArgs_Indirect, resolution);
        m_IndirectPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    void SetUpProceduralPass()
    {

        if (isActive)
        {
            if (particleBuffer == null)
                particleBuffer = new ComputeBuffer(MaxResolution * MaxResolution, 4 * 4);
            if (bufferWithArgs_Procedural == null)
                bufferWithArgs_Procedural = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            else
            if (InstanceMesh != null)
                bufferWithArgs_Procedural.SetData(new uint[] { resolution * resolution * 4, 5, 0, 0, 0 });
        }

        m_ProceduralPass = new RenderInstancesProceduralPass(GPUProceduralCS, ProceduralMaterial, InstanceMesh, particleBuffer, bufferWithArgs_Procedural, resolution);
        m_ProceduralPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (isInstancesIndirect)
        {
            if (GPUComputeShader != null && InstanceMaterial != null && InstanceMesh != null && positionBuffer != null)
                renderer.EnqueuePass(m_IndirectPass);
        }
        else
        if (GPUProceduralCS != null && ProceduralMaterial != null && InstanceMesh != null && particleBuffer != null)
            renderer.EnqueuePass(m_ProceduralPass);
    }

    // private void OnDisable()
    // {
    //     positionBuffer.Release();
    //     particleBuffer.Release();
    //     bufferWithArgs_Indirect.Release();
    //     bufferWithArgs_Procedural.Release();
    // }

}


