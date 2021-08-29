using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class DrawMeshPassSetting
{
    public Mesh m_Mesh;
    public RenderPassEvent passEvent;
    public Material UnlitInstancedMaterial;
    public Material LitInstancedMaterial;
    public Material LitInstancedProceduralMaterial;
    public Texture2D lightMap;
    public LightMapData lightMapData;
}

public class DrawMeshFeature : ScriptableRendererFeature
{
    public DrawMeshPassSetting m_DrawMeshPassSetting = new DrawMeshPassSetting();

    DrawMeshPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new DrawMeshPass(m_DrawMeshPassSetting);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_DrawMeshPassSetting.m_Mesh != null &&
        m_DrawMeshPassSetting.UnlitInstancedMaterial != null &&
        m_DrawMeshPassSetting.LitInstancedMaterial != null &&
        m_DrawMeshPassSetting.LitInstancedProceduralMaterial != null &&
        m_DrawMeshPassSetting.m_Mesh != null
        )
            renderer.EnqueuePass(m_ScriptablePass);
    }
}


