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

    const int MaxResolution = 300;
    [Range(10, MaxResolution)]
    public uint resolution = 10;
    RenderInstancesIndirectPass m_IndirectPass;
    RenderInstancesProceduralPass m_ProceduralPass;
    private static ComputeBuffer positionBuffer;
    private static ComputeBuffer particleBuffer;

    private static ComputeBuffer bufferWithArgs;

    public override void Create()
    {
        SetUpIndirectPass();
    }

    void SetUpIndirectPass()
    {
        if (isActive && positionBuffer == null)
        {
            positionBuffer = new ComputeBuffer(MaxResolution * MaxResolution, 3 * 4);
        }
        if (!isActive && positionBuffer != null)
        {
            //释放positionBuffer
            positionBuffer.Release();
            positionBuffer = null;
        }

        if (isActive && particleBuffer == null)
        {
            particleBuffer = new ComputeBuffer(MaxResolution * MaxResolution, 4 * 4 * 2);
        }
        if (!isActive && particleBuffer != null)
        {
            //释放particleBuffer
            particleBuffer.Release();
            particleBuffer = null;
        }


        if (isActive)
        {
            if (bufferWithArgs == null)
                bufferWithArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            else
            if (InstanceMesh != null)
                bufferWithArgs.SetData(new uint[] { InstanceMesh.GetIndexCount(0), resolution * resolution, 0, 0, 0 });
        }
        else
        {
            if (bufferWithArgs != null)
            {
                //释放bufferWithArgsBuffer
                bufferWithArgs.Release();
                bufferWithArgs = null;
            }
        }

        m_IndirectPass = new RenderInstancesIndirectPass(GPUComputeShader, InstanceMaterial, InstanceMesh, positionBuffer, bufferWithArgs, resolution);
        m_IndirectPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    void SetUpProceduralPass()
    {
        if (isActive && particleBuffer == null)
        {
            particleBuffer = new ComputeBuffer(MaxResolution * MaxResolution, 4 * 4 * 2);
        }
        if (!isActive && particleBuffer != null)
        {
            //释放particleBuffer
            particleBuffer.Release(); 
            particleBuffer = null;
        }

        if (isActive)
        {
            if (bufferWithArgs == null)
                bufferWithArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            else
            if (InstanceMesh != null)
                bufferWithArgs.SetData(new uint[] { resolution * resolution, 5, 0, 0, 0 });
        }
        else
        {
            if (bufferWithArgs != null)
            {
                //释放bufferWithArgsBuffer
                bufferWithArgs.Release();
                bufferWithArgs = null;
            }
        }

        m_ProceduralPass = new RenderInstancesProceduralPass(GPUProceduralCS, ProceduralMaterial, InstanceMesh, particleBuffer, bufferWithArgs, resolution);

        m_ProceduralPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (GPUComputeShader != null && InstanceMaterial != null && InstanceMesh != null && positionBuffer != null)
            renderer.EnqueuePass(m_IndirectPass);
        // if (GPUProceduralCS != null && ProceduralMaterial != null && InstanceMesh != null && particleBuffer != null)
        //     renderer.EnqueuePass(m_ProceduralPass);
    }

    

    public new void Dispose()
    {
        Dispose(true);
        positionBuffer.Release();
        particleBuffer.Release();
        bufferWithArgs.Release(); 
        GC.SuppressFinalize(this);
    }
}


