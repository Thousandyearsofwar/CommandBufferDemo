using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Profiling;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public class BlockRangeTest : MonoBehaviour
    {

        public struct BlockRange : IDisposable
        {
            int m_Current;
            int m_End;
            public BlockRange(int begin, int end)
            {
                Assertions.Assert.IsTrue(begin <= end);
                m_Current = begin < end ? begin : end;
                m_End = end >= begin ? end : begin;
                m_Current -= 1;
            }

            public BlockRange GetEnumerator() { return this; }
            public bool MoveNext() { return ++m_Current < m_End; }
            public int Current { get => m_Current; }
            public void Dispose() { }
        }

        public BlockRange GetRange(int index)
        {
            return new BlockRange(0, 3);
        }

        // Start is called before the first frame update
        void Start()
        {
            foreach (int currIndex in GetRange(0))
            {
                Debug.Log(currIndex);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}