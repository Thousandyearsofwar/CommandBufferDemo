using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

[Serializable]
public class LightMapData : ScriptableObject
{
#if UNITY_EDITOR
    [MenuItem("Tools/CreateLightMapData")]
    static void CreateLightMapData()
    {
        if (Selection.activeTransform)
        {
            var instance = CreateInstance<LightMapData>();
            instance.lightMapUVs = new PositionAndLightMapUVs();
            MeshRenderer[] m_MeshRenderer = Selection.activeTransform.GetComponentsInChildren<MeshRenderer>(false);

            for (int i = 0; i < m_MeshRenderer.Length; i++)
            {
                instance.lightMapUVs.m_Position.Add(m_MeshRenderer[i].GetComponent<Transform>().position);
                instance.lightMapUVs.m_LightMapUV.Add(m_MeshRenderer[i].lightmapScaleOffset);
            }

            AssetDatabase.CreateAsset(instance, string.Format("Assets/RenderingData/LightMapDatas/{0}.asset", typeof(LightMapData)));
            Selection.activeObject = instance;
        }
    }
#endif
    [Serializable]
    public sealed class PositionAndLightMapUVs
    {
        public List<Vector3> m_Position = new List<Vector3>();
        public List<Vector4> m_LightMapUV = new List<Vector4>();
    }
    public PositionAndLightMapUVs lightMapUVs;

}
