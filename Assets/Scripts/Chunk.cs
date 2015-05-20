﻿using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Chunk {
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
    
    public Chunk(Vector3 posLL, Vector3 chunkDim, int voxDim)
    {
        this.wsPosLL = posLL;
        this.wsChunkDim = chunkDim;
        this.voxelDim = voxDim;

        nonemptyListBuffer = new ComputeBuffer(voxDim * voxDim * voxDim, sizeof(int), ComputeBufferType.Default);
        triangleBuffer = new ComputeBuffer(SIZE, 3 * (6 * sizeof(float)), ComputeBufferType.Default);
        triangleCount = SIZE;

        int maxVertices = voxDim * voxDim * voxDim * 3; // every voxel can produce maximum 3 vertices
        vertexBuffer = new ComputeBuffer(maxVertices, 6 * sizeof(float), ComputeBufferType.Default);

        int maxIndices = voxDim * voxDim * voxDim * 15; // every voxel can produce maximum 15 indices into the vertex buffer
        indexBuffer = new ComputeBuffer(maxIndices, sizeof(int), ComputeBufferType.Default);

        // create the vertex index volume for index lookup.
        VertexIDVol = new RenderTexture(32 + 1, 32 + 1, 0, RenderTextureFormat.ARGBInt);
        VertexIDVol.enableRandomWrite = true;
        VertexIDVol.isVolume = true;
        VertexIDVol.volumeDepth = 32 + 1;
        VertexIDVol.Create();
        
    }

    public Chunk()
    {

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
    public GameObject GenerateChunkIndexed()
    {
        GameObject chunkObject = new GameObject("Chunk");
        int numTriangles = indexCount / 3;
        //int numMeshes = Mathf.CeilToInt(numTriangles / MAX_TRIANGLES);
        int numMeshes = (vertexCount / MAX_VERTICES )/2 + 1;
        int numTrianglesLeft = numTriangles;

        Vector3[] vertBuffer = new Vector3[vertexCount * 2];
        vertexBuffer.GetData(vertBuffer);

        int[] indices = new int[indexCount];
        indexBuffer.GetData(indices);
        int numVerticesLeft = vertexCount;
        int buffIndex = 0;
        for (int m = 0; m < numMeshes; m++)
        {
            GameObject obj = new GameObject("Chunk Part");
            Mesh mesh;
            mesh = GenerateMeshIndexed(ref vertBuffer, ref indices, ref buffIndex, ref numTrianglesLeft, ref numVerticesLeft);

            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.GetComponent<Renderer>().material = chunkMatMesh;
            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.transform.parent = chunkObject.transform;
            obj.transform.position = wsPosLL;
            obj.transform.localScale = wsChunkDim;
        }

        Release();
        return chunkObject;
    }
    public GameObject GenerateChunkObject()
    {
        GameObject chunkObject = new GameObject("Chunk");

        int numTriangles = triangleCount;
        
        int numTrianglesLeft = numTriangles;

        Vector3[] triBuffer = new Vector3[numTriangles * 6];
        triangleBuffer.GetData(triBuffer);

        int buffIndex = 0;
        while(numTrianglesLeft > 0)
        {
            GameObject obj = new GameObject("Chunk Part");
            Mesh mesh = GenerateMesh(ref triBuffer, ref buffIndex, ref numTrianglesLeft);
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.GetComponent<Renderer>().material = chunkMatMesh;
            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.transform.parent = chunkObject.transform;
            obj.transform.position = chunkObject.transform.position;
        }
        triangleBuffer.Release();
        return chunkObject;
    }
    public void CreateChunkMeshIndexed()
    {
        int numTriangles = indexCount / 3;
        //int numMeshes = Mathf.CeilToInt(numTriangles / MAX_TRIANGLES);
        int numMeshes = (vertexCount / MAX_VERTICES) / 2 + 1;
        int numTrianglesLeft = numTriangles;
        m_meshes = new List<Mesh>(numMeshes);
        Vector3[] vertBuffer = new Vector3[vertexCount * 2];
        vertexBuffer.GetData(vertBuffer);

        int[] indices = new int[indexCount];
        indexBuffer.GetData(indices);
        int numVerticesLeft = vertexCount;
        int buffIndex = 0;
        for (int m = 0; m < numMeshes; m++)
        {

            m_meshes.Add( GenerateMeshIndexed(ref vertBuffer, ref indices, ref buffIndex, ref numTrianglesLeft, ref numVerticesLeft) );
        }

        Release();

    }

    public void CreateChunkMesh()
    {

        int numTriangles = triangleCount;
        
        int numTrianglesLeft = numTriangles;

        Vector3[] triBuffer = new Vector3[numTriangles * 6];
        triangleBuffer.GetData(triBuffer);
        m_meshes = new List<Mesh>();
        int buffIndex = 0;
        while (numTrianglesLeft > 0)
        {
            
            m_meshes.Add( GenerateMesh(ref triBuffer, ref buffIndex, ref numTrianglesLeft) );

        }
        triangleBuffer.Release();

    }
    private Mesh GenerateMeshIndexed(ref Vector3[] vertBuffer, ref int[] indices, ref int startIndex, ref int numTrianglesLeft, ref int numVerticesLeft)
    {
        Mesh mesh = new Mesh();
        
       // Vector3[] vertices = new Vector3[numVertices];
        //Vector3[] normals= new Vector3[numVertices];
        List<Vector3> vertexList = new List<Vector3>(Mathf.Min(vertexCount, MAX_VERTICES));
        List<Vector3> normalList = new List<Vector3>(Mathf.Min(vertexCount, MAX_VERTICES));
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
                        Debug.Log("error" + e);
                        Debug.Log("vID: " + vID);
                        Debug.Log("vertexCount" + vertexCount);
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

    public void CreateIndexVol()
    {
        // create the vertex index volume for index lookup.
        VertexIDVol = new RenderTexture(voxelDim+1, voxelDim+1, 0, RenderTextureFormat.ARGBInt);
        VertexIDVol.enableRandomWrite = true;
        VertexIDVol.isVolume = true;
        VertexIDVol.volumeDepth = voxelDim+1;
        VertexIDVol.Create();
    }
}
