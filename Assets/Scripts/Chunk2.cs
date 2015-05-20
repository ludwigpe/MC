using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
public class Chunk2 {
    public static int vCount = 0;
    public static int tCount = 0;
    const int MAX_TRIANGLES = 21666; // 64 998 vertices max is 65 k
    const int MAX_VERTICES = 64998;
    const int SIZE = 32 * 32 * 32 * 5;
    public Vector3 wsPosLL;
    public Vector3 wsChunkDim;
    public int voxelDim;
    public int lod;
    public int triangleCount { get; set; }
    public int vertexCount { get; set; }
    public int indexCount { get; set;}
    public int numNonemptyCells { get; set; }
    public bool isEmpty { get; set; }
    public Material chunkMatProcedural {get; set;}
    public Material chunkMatMesh { get; set; }
    public ComputeBuffer nonemptyListBuffer { get; set; }
    public ComputeBuffer triangleBuffer {get; set;}
    public ComputeBuffer vertexBuffer { get; set; }
    public ComputeBuffer indexBuffer { get; set; }
    public RenderTexture VertexIDVol { get; set; }

    private List<Mesh> m_meshes;
    
    public Chunk2(Vector3 posLL, Vector3 chunkDim, int voxDim, int lod)
    {
        this.wsPosLL = posLL;
        this.wsChunkDim = chunkDim;
        this.voxelDim = voxDim;
        this.lod = lod;

        nonemptyListBuffer = new ComputeBuffer(voxDim * voxDim * voxDim, sizeof(int), ComputeBufferType.Default);
        m_meshes = new List<Mesh>();
    }

    public void Draw()
    {
        //chunkMatProcedural.SetPass(0);
        //chunkMatProcedural.SetBuffer("triangleBuffer", triangleBuffer);
        //Graphics.DrawProcedural(MeshTopology.Points, triangleCount);
        Matrix4x4 matrix = new Matrix4x4();
        //matrix = Matrix4x4.TRS(wsPosLL, Quaternion.identity, wsChunkDim);
        matrix = Matrix4x4.identity;
        foreach(Mesh m in m_meshes)
        {
            
            //Graphics.DrawMesh(m, wsPosLL, Quaternion.identity, chunkMatMesh, 0);
            Graphics.DrawMesh(m, matrix, chunkMatMesh, 0);
        }
    }
    public void DrawProcedural()
    {
        chunkMatProcedural.SetPass(0);
        chunkMatProcedural.SetBuffer("indexBuffer", indexBuffer);
        chunkMatProcedural.SetBuffer("vertexBuffer", vertexBuffer);
        chunkMatProcedural.SetMatrix("modelMat", Matrix4x4.TRS(wsPosLL, Quaternion.identity, wsChunkDim));
        ComputeBuffer argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.DrawIndirect);

        int[] args = new int[] { vertexCount, 1, 0, 0 };
        argBuffer.SetData(args);
        Graphics.DrawProceduralIndirect(MeshTopology.Points, argBuffer, 1);

    }
    public void Release()
    {
        if(nonemptyListBuffer != null)
            nonemptyListBuffer.Release();
        if(triangleBuffer != null)
            triangleBuffer.Release();
        if (VertexIDVol != null)
            VertexIDVol.Release();
        if (vertexBuffer != null)
            vertexBuffer.Release();
        if (indexBuffer != null)
            indexBuffer.Release();
    }
    //public void CreateChunkMeshIndexed()
    //{
        
    //    m_meshes = new List<Mesh>(1);
    //    Vector3[] vertBuffer = new Vector3[vertexCount * 2];
    //    vertexBuffer.GetData(vertBuffer);
    //    Vector3[] vertices = new Vector3[vertexCount];
    //    Vector3[] normals = new Vector3[vertexCount];
    //    int[] indices = new int[indexCount];
    //    indexBuffer.GetData(indices);
    //    Mesh m = new Mesh();
    //    int offset = 0;
    //    for (int i = 0; i < vertexCount; i++)
    //    {
    //        vertices[i] = vertBuffer[offset];
    //        normals[i] = vertBuffer[offset + 1];
    //        offset += 2;
    //    }
    //    m.vertices = vertices;
    //    m.normals = normals;
    //    m.triangles = indices;
    //    m_meshes.Add(m);
    //    Release();

    //}
    public void CreateChunkMeshIndexed(ref Vector3[] vertBuffer, ref int[] indices)
    {
        int numTriangles = indexCount / 3;
        numTriangles = triangleCount;
        int numMeshes = (vertexCount / MAX_VERTICES) / 2 + 1;
        int numTrianglesLeft = numTriangles;
        m_meshes = new List<Mesh>(numMeshes);
        int numVerticesLeft = vertexCount;
        int buffIndex = 0;
        for (int m = 0; m < numMeshes; m++)
        {

            m_meshes.Add(GenerateMeshIndexed(ref vertBuffer, ref indices, ref buffIndex, ref numTrianglesLeft, ref numVerticesLeft));
        }

        Release();


    }

    public void CreateChunkMesh(ref Vector3[] triBuffer)
    {
        
        int numTrianglesLeft = triangleCount;
        
        int buffIndex = 0;
        while (numTrianglesLeft > 0)
        {
            
            m_meshes.Add( GenerateMesh(ref triBuffer, ref buffIndex, ref numTrianglesLeft) );

        }
        Release();
      //  triangleBuffer.Release();

    }
    private Mesh GenerateMeshIndexed(ref Vector3[] vertBuffer, ref int[] indices, ref int startIndex, ref int numTrianglesLeft, ref int numVerticesLeft)
    {
        Mesh mesh = new Mesh();
        
       // Vector3[] vertices = new Vector3[numVertices];
        //Vector3[] normals= new Vector3[numVertices];
        // initialize vertex and normal list
        List<Vector3> vertexList = new List<Vector3>(Mathf.Min(vertexCount, MAX_VERTICES));
        List<Vector3> normalList = new List<Vector3>(Mathf.Min(vertexCount, MAX_VERTICES));

        // create a dictionary for vertices, key is vertex id and value is
        Dictionary<int, int> vMap = new Dictionary<int, int>();
        int numTriangles = Mathf.Min(numTrianglesLeft, MAX_TRIANGLES);
        int numVerticesAdded = 0;
        int vSize = 2;
        int[] triangles = new int[numTriangles * 3];
        int i = 0;

        for (int t = 0; t < numTriangles; t++ )
        {
            for(int v = 0; v < 3; v++)
            {
                i = 3*t + v;
                int vID = indices[startIndex + i];
                if (vMap.ContainsKey(vID))
                {
                    // we have already added this vertex to the vertices array, get the index of that vertex
                    triangles[i] = vMap[vID];
                }
                else
                {
                    //vertices[numVerticesAdded] = vertBuffer[vID * vSize];
                    try
                    {
                        vertexList.Add(vertBuffer[vID * vSize]);
                        //normals[numVerticesAdded] = vertBuffer[vID * vSize + 1];
                        normalList.Add(vertBuffer[vID * vSize + 1]);
                        // add the index to this vertex into the dictionary, the key is the vertexID and the value is the index into the vertices array
                        vMap.Add(vID, numVerticesAdded);
                        // add the index to the vertex into the vIndices array
                        triangles[i] = numVerticesAdded;
                        numVerticesAdded++;
                    }
                    catch(IndexOutOfRangeException e)
                    {
                        //Debug.Log("error" + e);
                        //Debug.Log("vID: " + vID);
                        //Debug.Log("vertexCount" + vertexCount);
                    }
                    
                }
            }
        }
   

        //mesh.vertices = vertices;
        //mesh.normals = normals;
        mesh.vertices = vertexList.ToArray();
        mesh.normals = normalList.ToArray();
        mesh.triangles = triangles;
        startIndex += numTriangles*3;
        numTrianglesLeft -= numTriangles;
        numVerticesLeft -= numVerticesAdded;
        return mesh;
    }
    private Mesh GenerateMesh(ref Vector3[] triBuffer, ref int buffIndex, ref int numTrianglesLeft)
    {
        Mesh mesh = new Mesh();
        int triCount = Mathf.Min(numTrianglesLeft, MAX_TRIANGLES);
        Vector3[] vertices = new Vector3[3 * triCount];
        Vector3[] normals = new Vector3[3 * triCount];
        int[] indices = new int[3 * triCount];
        int vertIndex = 0;
        // loop through each triangle in the triangle buffer
        for (int i = 0; i < triCount; i++)
        {
            // 1 triangle consists of 3 vertices so do 3 loops to extract 1 triangle
            // advances the buffIndex by one since vertices and normals are interleaved
            // each loop advancess the vertexIndex by one.
            for (int j = 0; j < 3; j++)
            {
                vertices[vertIndex] = triBuffer[buffIndex++];
                normals[vertIndex] = triBuffer[buffIndex++];
                indices[vertIndex] = vertIndex;
                vertIndex++;
            }

        }
        // decrease the number of triangles left by the amount consumed by this mesh.
        numTrianglesLeft -= triCount;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = indices;
        
        return mesh;

    }
    public void CreateTriangleBuffer(int numTriangles)
    {
        triangleBuffer = new ComputeBuffer(numTriangles, 3 * (6 * sizeof(float)), ComputeBufferType.Default);
        triangleCount = numTriangles;
    }
    public void CreateVertexBuffer(int numVertices)
    {
        vertexBuffer = new ComputeBuffer(numVertices, 6 * sizeof(float), ComputeBufferType.Default);
        vertexCount = numVertices;
    }
    public void CreateIndexBuffer(int numTriangles)
    {
        indexBuffer = new ComputeBuffer(numTriangles * 3, sizeof(int));
        indexCount = numTriangles * 3;
    }
    public void CreateIndexVol()
    {
        // create the vertex index volume for index lookup.
        VertexIDVol = new RenderTexture(voxelDim+1, voxelDim+1, 0, RenderTextureFormat.ARGBInt);
        VertexIDVol.enableRandomWrite = true;
        VertexIDVol.isVolume = true;
        VertexIDVol.volumeDepth = voxelDim+1;
        VertexIDVol.Create();
    }
    public int GetVertexCount()
    {
        int v = 0;
        foreach(Mesh m in m_meshes)
        {
            v += m.vertexCount;
        }
        return v;
    }

    public int GetVRAMUsage()
    {
        int m = 0;
        foreach(Mesh mesh in m_meshes)
        {
            m += Profiler.GetRuntimeMemorySize(mesh);
        }
        return m;
    }

}
