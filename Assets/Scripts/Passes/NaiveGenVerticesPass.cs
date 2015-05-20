using UnityEngine;
using System.Collections;

public class NaiveGenVerticesPass : BasePass {

    public NaiveGenVerticesPass()
    {
        base.LoadComputeShader("Assets/Shaders/gen_vertices_naive.compute");
    }
    public override bool DoPass(ref Chunk chunk, ref RenderTexture densityTexture)
    {
        computeShader.SetTexture(0, "densityTexture", densityTexture);
        computeShader.SetBuffer(0, "case_to_numpolys", Helper.GetCaseToNumPolyBuffer());
        computeShader.SetBuffer(0, "edge_connect_list", Helper.GetTriangleConnectionTable());
        computeShader.SetBuffer(0, "triangleBuffer", chunk.triangleBuffer);

        float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
        computeShader.SetFloat("invVoxelDim", invVoxelDim);
        computeShader.SetVector("wsChunkPosLL", chunk.wsPosLL);
        computeShader.SetVector("wsChunkDim", chunk.wsChunkDim);
        computeShader.SetInt("voxelDim", chunk.voxelDim);
        int N = chunk.voxelDim;

        computeShader.Dispatch(0, N / 4, N / 4, N / 4);
        return true;
    }
    public override bool DoPass(ref Chunk2 chunk, ref RenderTexture densityTexture)
    {
      //  float startTime = Time.realtimeSinceStartup;
        int MAX_TRIANGLES = chunk.voxelDim * chunk.voxelDim * chunk.voxelDim * 5;
        chunk.CreateTriangleBuffer(MAX_TRIANGLES);
        computeShader.SetTexture(0, "densityTexture", densityTexture);
        computeShader.SetBuffer(0, "case_to_numpolys", Helper.GetCaseToNumPolyBuffer());
        computeShader.SetBuffer(0, "edge_connect_list", Helper.GetTriangleConnectionTable());
        computeShader.SetBuffer(0, "triangleBuffer", chunk.triangleBuffer);

        float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
        computeShader.SetFloat("invVoxelDim", invVoxelDim);
        computeShader.SetVector("wsChunkPosLL", chunk.wsPosLL);
        computeShader.SetVector("wsChunkDim", chunk.wsChunkDim);
        computeShader.SetInt("voxelDim", chunk.voxelDim);
        int N = chunk.voxelDim;

        computeShader.Dispatch(0, N / 4, N / 4, N / 4);
       // PassTime = Time.realtimeSinceStartup - startTime;
        return true;
    }
}
