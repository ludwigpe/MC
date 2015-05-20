using UnityEngine;
using System.Collections;

public class ListNonemptyCellsPass : BasePass {

    public ComputeBuffer indexCounterBuffer;
    public ComputeBuffer numVerticesBuffer;
    public ComputeBuffer numTrianglesBuffer;
    public ListNonemptyCellsPass()
    {
        base.LoadComputeShader("Assets/Shaders/list_nonempty_voxels.compute");
        indexCounterBuffer = new ComputeBuffer(1, sizeof(int));
        numVerticesBuffer = new ComputeBuffer(1, sizeof(int));
        numTrianglesBuffer= new ComputeBuffer(1, sizeof(int));
    }

    public override bool DoPass(ref Chunk chunk, ref RenderTexture densityTexture)
    {
        int[] data = new int[1];
        data[0] = 0;

        indexCounterBuffer.SetData(data);
        numVerticesBuffer.SetData(data);

        computeShader.SetBuffer(0, "indexCounter", indexCounterBuffer);
        computeShader.SetTexture(0, "densityTexture", densityTexture);
        computeShader.SetBuffer(0, "nonemptyList", chunk.nonemptyListBuffer);

        computeShader.SetBuffer(0, "numVerts", numVerticesBuffer);

        float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
        computeShader.SetFloat("invVoxelDim", invVoxelDim);
        computeShader.Dispatch(0, 1, 33, 33);
        
        
        
        numVerticesBuffer.GetData(data);
        int numVerts = data[0];
        if(numVerts == 0)
        {
            chunk.isEmpty = true;
            return false;
        }

        indexCounterBuffer.GetData(data);
        int numNonemptyCells = data[0];
        chunk.numNonemptyCells = numNonemptyCells;
        if (numNonemptyCells == 0)
        {
            chunk.isEmpty = true;
            return false;
        }

        //chunk.CreateVertexBuffer(numVerts);
        return true;
    }

    public override bool DoPass(ref Chunk2 chunk, ref RenderTexture densityTexture)
    {
     //   float startTime = Time.realtimeSinceStartup;
        int[] data = new int[1];
        data[0] = 0;

        indexCounterBuffer.SetData(data);
        numVerticesBuffer.SetData(data);
        numTrianglesBuffer.SetData(data);

        computeShader.SetBuffer(0, "indexCounter", indexCounterBuffer);
        computeShader.SetTexture(0, "densityTexture", densityTexture);
        computeShader.SetBuffer(0, "nonemptyList", chunk.nonemptyListBuffer);
        computeShader.SetBuffer(0, "case_to_numpolys", Helper.GetCaseToNumPolyBuffer());
        computeShader.SetBuffer(0, "numVerts", numVerticesBuffer);
        computeShader.SetBuffer(0, "numTris", numTrianglesBuffer);
        float invVoxelDim = 1.0f / ((float)chunk.voxelDim);
        computeShader.SetFloat("invVoxelDim", invVoxelDim);
        computeShader.Dispatch(0, 1, 33, 33);



        numVerticesBuffer.GetData(data);
        int numVerts = data[0];
        if (numVerts == 0)
        {
            chunk.isEmpty = true;
            return false;
        }

        indexCounterBuffer.GetData(data);
        int numNonemptyCells = data[0];
        chunk.numNonemptyCells = numNonemptyCells;
        if (numNonemptyCells == 0)
        {
            chunk.isEmpty = true;
            return false;
        }

        numTrianglesBuffer.GetData(data);
        int numTris = data[0];

        chunk.CreateVertexBuffer(numVerts);
        chunk.CreateIndexBuffer(numTris);
        chunk.CreateTriangleBuffer(numTris);

      //  PassTime = Time.realtimeSinceStartup - startTime;
        return true;
    }

    public override void Release()
    {
        indexCounterBuffer.Release();
        numVerticesBuffer.Release();
        numTrianglesBuffer.Release();
    }
}
