﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
public abstract class BasePass
{
    public ComputeShader computeShader;
    public float PassTime;
    public abstract bool DoPass(ref Chunk chunk, ref RenderTexture densityTexture);
    public virtual bool DoPass(ref Chunk2 chunk, ref RenderTexture densityTexture)
    {
        return false;
    }
    public virtual void Release()
    {

    }
    public void LoadComputeShader(string path)
    {
        #if UNITY_EDITOR
            computeShader = AssetDatabase.LoadMainAssetAtPath(path) as ComputeShader;
        #endif
    }
}
