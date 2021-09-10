using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
//TODO：Instance drawing


// DrawMesh 1

// DrawMeshInstanced 1
//https://github.com/vanCopper/Unity-GPU-Instancing

// DrawMeshInstanceIndirect: GPGPU Terrain Indirect意义是什么  用bufferArgs控制绘制数量

// DrawMeshInstancedProcedural: 绘制Instance时的InstancePath是 Instance Procedural

// DrawOcclusionMesh: Adds a command onto the commandbuffer to draw the VR Device's occlusion mesh to the current render target.
// DrawProcedural:  专门绘制MeshTopology
// DrawProceduralIndirect: GPGPU绘制DrawProceduralIndirect  

// instancing_options:
// force_same_maxcount_for_gl:在手机平台处理一样的maxcount 1
// forcemaxcount/maxcount:Batch最大数量  1

// procedural:使用函数xxxx 
// assumeuniformscaling:使用统一缩放值 

// lodfade:对lod value也做合批处理 1
// nolodfade:对lod value不做合批处理 1

// nomatrices: 没有M矩阵变换[感觉没什么用(] 1
// nolightprobe:不受lightprobe影响 1
// nolightmap:不使用lightmap 1

class DrawMeshPass : ScriptableRenderPass
{
    private DrawMeshPassSetting passSetting;
    private ProfilingSampler m_ProfilingSampler;
    string m_ProfilerTag = "DrawMesh";
    private static Matrix4x4[] matrices = new Matrix4x4[1024];
    private static Vector4[] colors = new Vector4[1023];
    private static Vector4[] lightMapST = new Vector4[4];

    SphericalHarmonicsL2[] lightProbesSH;
    float[,] lightProbeSHs;
    Vector4[] OcclusionProbes;

    Vector4[] LODFade;
    Vector4[] positions;

    public DrawMeshPass(DrawMeshPassSetting m_DrawMeshPassSetting)
    {
        this.passSetting = m_DrawMeshPassSetting;
        // Configures where the render pass should be injected.
        m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);
        this.renderPassEvent = m_DrawMeshPassSetting.passEvent;

        DrawLitMeshInstanced_Setup();

    }

    public void DrawMeshInstanced_Setup()
    {
        //在一个半径为10倍大小的单位球之内随机生成
        // for (int i = 0; i < matrices.Length; i++)
        // {
        //     Vector3 position = Random.onUnitSphere * 10f;
        //     matrices[i] =Matrix4x4.Translate(position);
        // }
        Vector3 pos = new Vector3();
        int index;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                for (int k = 0; k < 16; k++)
                {
                    pos.x = 2 * i - 7;
                    pos.y = 2 * j - 7;
                    pos.z = 2 * k - 15;
                    index = 8 * 8 * k + 8 * j + i;
                    matrices[index] = Matrix4x4.Translate(pos) * Matrix4x4.Rotate(Quaternion.Euler(Random.value * 360, Random.value * 360, Random.value * 360)) * Matrix4x4.Scale(new Vector3(Random.value, Random.value, Random.value));
                    if (index < 1023)
                        colors[index] = new Vector4(pos.x / 7, pos.y / 7, pos.z / 15, 1.0f);
                }
    }

    public void DrawLitMeshInstanced_Setup()
    {
        int Count = passSetting.lightMapData.lightMapUVs.m_Position.Count;
        positions = new Vector4[Count];
        for (int i = 0; i < Count; i++)
        {
            matrices[i] = Matrix4x4.Translate(passSetting.lightMapData.lightMapUVs.m_Position[i]);
            lightMapST[i] = passSetting.lightMapData.lightMapUVs.m_LightMapUV[i];

            positions[i] = new Vector4(passSetting.lightMapData.lightMapUVs.m_Position[i].x, passSetting.lightMapData.lightMapUVs.m_Position[i].y, passSetting.lightMapData.lightMapUVs.m_Position[i].z, 1.0f);
        }
        lightProbesSH = new SphericalHarmonicsL2[Count];
        lightProbeSHs = new float[Count, 7];
        OcclusionProbes = new Vector4[Count];

        //有点为了使用而使用这个Option了，实际上我感觉是用Graphics.DrawInstance会好一点,毕竟能直接用Renderer https://my.oschina.net/u/4589313/blog/4447463
        LODFade = new Vector4[Count];
        for (int i = 0; i < Count; i++)
            LODFade[i] = new Vector4(0.3f * i, 0.0f, 0.0f, 0.0f);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        DrawMeshTest(context, ref renderingData);
    }

    void DrawMeshTest(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            if (passSetting.UnlitInstancedMaterial != null)
            {
                MaterialPropertyBlock m_MatBlock = new MaterialPropertyBlock();
                m_MatBlock.SetColor("_TestColor", Color.white);
                commandBuffer.DrawMesh(passSetting.m_Mesh, Matrix4x4.identity, passSetting.UnlitInstancedMaterial, 0, 0, m_MatBlock);
            }
        }
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
        CommandBufferPool.Release(commandBuffer);
    }

    void DrawMeshInstanced(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            if (passSetting.UnlitInstancedMaterial != null)
            {
                MaterialPropertyBlock m_MatBlock = new MaterialPropertyBlock();

                m_MatBlock.SetVectorArray("_TestColor", colors);
                commandBuffer.DrawMeshInstanced(passSetting.m_Mesh, 0, passSetting.UnlitInstancedMaterial, 0, matrices, 1023, m_MatBlock);
            }
        }
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
        CommandBufferPool.Release(commandBuffer);
    }

    void DrawLitMeshInstanced(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            if (passSetting.LitInstancedMaterial != null)
            {
                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(passSetting.lightMapData.lightMapUVs.m_Position.ToArray(), lightProbesSH, OcclusionProbes);

                passSetting.LitInstancedMaterial.EnableKeyword("LIGHTMAP_ON");//可以自己测试LightMap
                //passSetting.LitInstancedMaterial.DisableKeyword("LIGHTMAP_ON");//可以自己测试LightProbe
                //passSetting.LitInstancedMaterial.EnableKeyword("LOD_FADE_CROSSFADE");
                MaterialPropertyBlock m_MatBlock = new MaterialPropertyBlock();
                m_MatBlock.SetTexture("unity_Lightmap", passSetting.lightMap);
                m_MatBlock.SetVectorArray("unity_LightmapST", lightMapST);
                m_MatBlock.SetVectorArray("unity_LODFade", LODFade);
                m_MatBlock.CopySHCoefficientArraysFrom(lightProbesSH);
                m_MatBlock.CopyProbeOcclusionArrayFrom(OcclusionProbes);

                commandBuffer.DrawMeshInstanced(passSetting.m_Mesh, 0, passSetting.LitInstancedMaterial, 0, matrices, 4, m_MatBlock);
            }
        }
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
        CommandBufferPool.Release(commandBuffer);
    }

    void DrawMeshInstancedProcedural(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //Procedural只能用全局
        CommandBuffer commandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
        {
            if (passSetting.LitInstancedProceduralMaterial != null)
            {
                passSetting.LitInstancedProceduralMaterial.EnableKeyword("LIGHTMAP_ON");

                MaterialPropertyBlock m_MatBlock = new MaterialPropertyBlock();

                m_MatBlock.SetVectorArray("_Position", positions);
                m_MatBlock.SetTexture("unity_Lightmap", passSetting.lightMap);

                m_MatBlock.SetVectorArray("_LightmapST", lightMapST);
                m_MatBlock.SetVectorArray("unity_LODFade", LODFade);

                // m_MatBlock.CopySHCoefficientArraysFrom(lightProbesSH);//LightProbe实现需要手动设置数组
                // m_MatBlock.CopyProbeOcclusionArrayFrom(OcclusionProbes);

                commandBuffer.DrawMeshInstancedProcedural(passSetting.m_Mesh, 0, passSetting.LitInstancedProceduralMaterial, 0, 4, m_MatBlock);
            }
        }
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
        CommandBufferPool.Release(commandBuffer);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
    }
}



