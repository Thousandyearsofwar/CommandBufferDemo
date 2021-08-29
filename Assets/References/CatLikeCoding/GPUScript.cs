using UnityEngine;

[ExecuteInEditMode]
public class GPUScript : MonoBehaviour
{

    const int MaxResolution = 300;
    [SerializeField, Range(10, MaxResolution)]
    int resolution = 10;

    [SerializeField]
    FunctionLibrary.FunctionName function;

    public enum TransitionMode { Cycle, Random }

    [SerializeField]
    TransitionMode transitionMode;

    [SerializeField, Min(0f)]
    float functionDuration = 1f, transitionDuration = 1f;

    ComputeBuffer positionBuffer;

    float duration;

    bool transitioning;

    FunctionLibrary.FunctionName transitionFunction;
    [SerializeField]
    ComputeShader computeShader;
    [SerializeField]
    Material material;
    [SerializeField]
    Mesh mesh;


    static readonly int positionId = Shader.PropertyToID("_Positions"),
    resolutionId = Shader.PropertyToID("_Resolution"),
    stepId = Shader.PropertyToID("_Step"),
    timeId = Shader.PropertyToID("_Time");

    private void OnEnable()
    {
        //float3 3个float[4 bytes]
        positionBuffer = new ComputeBuffer(MaxResolution * MaxResolution, 3 * 4);
    }

    private void OnDisable()
    {
        //释放positionBuffer
        positionBuffer.Release();
        positionBuffer = null;
    }

    private void OnInspectorUpdate()
    {
        Debug.Log(Time.time);
    }
    void Update()
    {
        duration += Time.deltaTime;
        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            }
        }
        else if (duration >= functionDuration)
        {
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }
        UpdateFunctionOnGPU();
    }

    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }

    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);
        computeShader.SetBuffer(0, positionId, positionBuffer);
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(0, groups, groups, 1);

        material.SetBuffer(positionId, positionBuffer);
        material.SetFloat(stepId, step);

        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
    }


}