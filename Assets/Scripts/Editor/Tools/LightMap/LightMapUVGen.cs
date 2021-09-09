using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
//https://www.xuanyusong.com/archives/4633 LightMapIndex,LightMapScaleOffset,UV2关系 [uv,uv2,uv3.....]
//LightMap UV2 http://edu.wumn.net/?id=24
#if UNITY_EDITOR
public class LightMapUVGen
{
    [MenuItem("Tools/SetUV2")]
    private static void SetUV2()
    {
        if (Selection.activeTransform)
        {
            Mesh mesh = GameObject.Instantiate<Mesh>(Selection.activeTransform.GetComponent<MeshFilter>().sharedMesh);

            Vector4 lightmapOffsetAndScale = Selection.activeTransform.GetComponent<MeshRenderer>().lightmapScaleOffset;

            //Mesh uv2重新赋值
            Vector2[] modifiedUV2s = mesh.uv2;
            for (int i = 0; i < mesh.uv2.Length; i++)
            {
                modifiedUV2s[i] = new Vector2(mesh.uv2[i].x * lightmapOffsetAndScale.x +
                lightmapOffsetAndScale.z, mesh.uv2[i].y * lightmapOffsetAndScale.y +
                lightmapOffsetAndScale.w);
            }
            mesh.uv2 = modifiedUV2s;
            AssetDatabase.CreateAsset(mesh, "Assets/Models/newMesh.asset");
            Selection.activeTransform.GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
#endif