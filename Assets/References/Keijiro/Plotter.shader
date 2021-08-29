Shader "Hidden/Plotter"
{
    CGINCLUDE

    #include "UnityCG.cginc"

    float4 _Range;
    half4 _LineColor;
    half4 _GridColor;
    half4 _ZeroColor;
    half4 _RefColor;

    ENDCG

    SubShader
    {
        Pass
        {
            CGPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Vertex(
                uint vid : SV_VertexID,
                uint iid : SV_InstanceID
            ) : SV_Position
            {
                float p = 1.0 / 2048 * vid;

                float x = _Range.x + _Range.z * p;
                float sx = p * 2 - 1;

                float y = smoothstep(0.2, 0.8, x);
                float sy = y* 2 - 1;

                // sx += (iid & 1) * (_ScreenParams.z - 1) * 2;
                // sy += (iid / 2) * (_ScreenParams.w - 1) * 2;

                return float4(sx, -sy, 1, 1);
            }

            half4 Fragment(float4 vertex : SV_Position) : SV_Target
            {
                return _LineColor;
            }

            ENDCG
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Vertex(uint vid : SV_VertexID) : SV_Position
            {
                float x = floor(_Range.x) + (vid / 2);
                x = (x - _Range.x) / _Range.z * 2 - 1;

                float y = (vid & 1) * 2.0 - 1;

                return float4(x, y, 1, 1);
            }

            half4 Fragment(float4 vertex : SV_Position) : SV_Target
            {
                return _GridColor;
            }

            ENDCG
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Vertex(uint vid : SV_VertexID) : SV_Position
            {
                float x = (vid & 1) * 2.0 - 1;

                float y = floor(_Range.y) + (vid / 2);
                y = (y - _Range.y) / _Range.w * 2 - 1;

                return float4(x, -y, 1, 1);
            }

            half4 Fragment(float4 vertex : SV_Position) : SV_Target
            {
                return _GridColor;
            }

            ENDCG
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Vertex(uint vid : SV_VertexID) : SV_Position
            {
                float v = (vid & 1) * 2.0 - 1;

                float2 p1 = float2(v, -_Range.y / _Range.w * 2 - 1);
                float2 p2 = float2(-_Range.x / _Range.z * 2 - 1, v);

                return float4(vid < 2 ? p1 : p2, 1, 1) * float4(1, -1, 1, 1);
            }

            half4 Fragment(float4 vertex : SV_Position) : SV_Target
            {
                return _ZeroColor;
            }

            ENDCG
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Vertex(uint vid : SV_VertexID) : SV_Position
            {
                float y = (vid & 1) * 2.0 - 1;
                float x = (vid < 2) ? 0.2 : 0.8;

                x = (x -_Range.x) / _Range.z * 2 - 1;

                return float4(x, y, 1, 1);
            }

            half4 Fragment(float4 vertex : SV_Position) : SV_Target
            {
                return _RefColor;
            }

            ENDCG
        }
    }
}