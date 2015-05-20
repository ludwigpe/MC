using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
public class TerrainCreator : MonoBehaviour {
    public int runs = 2;
    private static int runCounter = 0;
    public bool DEBUG = true;
    //public bool CREATE_GAMEOBJECT = true;
    public bool CONTINOUS_CREATION = true;
    public bool DRAW_PROCEDURAL = true;
    public bool EXTRACT_EVERY_FRAME = false;
    public bool TAKE_TIME = false;
    public BuildDensityMap.Passes m_chosenPass;

    const int SIZE = 32 * 32 * 32 * 5 * 3;
    public Material m_chunkMat;
    public Material m_procMat;
    public Vector3 m_numberOfChunks = new Vector3(1, 1, 1);
    public int m_chunkDim;
    private int m_voxelDim = 32;
    public int m_numChunkPerFrame = 4;
    private RenderTexture m_densityTexture;
    private List<Chunk2> m_chunkList;
    private Queue<Chunk2> m_chunkQueue;

    private BasePass[] m_passes;

    private GameObject m_worldGameObject;
    private static float m_totalChunkCreationTime;
    private static float m_totalMeshCreationTime;
    private static float m_totalCopyTime;
    private static float m_totalCreationTime;
    private static float m_emptyTime;
    private static float m_nonemptyTime;
    private int m_vCount;
    private int m_tCount;

    private float[] m_passTimes;
	// Use this for initialization
	void Start () {
        
        int numChunksTotal = (int)(m_numberOfChunks.x * m_numberOfChunks.y * m_numberOfChunks.z);
        m_chunkList = new List<Chunk2>(numChunksTotal);
        m_chunkQueue = new Queue<Chunk2>(numChunksTotal);
        switch (m_chosenPass)
        {
            case BuildDensityMap.Passes.NAIVE:
                m_passes = new BasePass[2];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new NaiveGenVerticesPass();
                m_passTimes = new float[2];
                break;
           
            case BuildDensityMap.Passes.NONEMPTY:
                m_passes = new BasePass[3];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new NonemptyGenVerticesPass();
                m_passTimes = new float[3];
                break;
            case BuildDensityMap.Passes.NONEMPTY_MEDICAL:
                m_passes = new BasePass[3];
                m_passes[0] = new BuildDensityMedicalPass();
                m_passes[1] = new ListNonemptyCellsPass();
				m_passes[2] = new NonemptyGenVerticesPass();
                break;
            case BuildDensityMap.Passes.INDICES:
                m_passes = new BasePass[4];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new SplatGenVerticesPass();
                m_passes[3] = new GenIndicesPass();
                m_passTimes = new float[4];
                break;
            case BuildDensityMap.Passes.INDICES_MEDICAL:
                m_passes = new BasePass[4];
                m_passes[0] = new BuildDensityMedicalPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new SplatGenVerticesPass();
                m_passes[3] = new GenIndicesPass();
                break;
        }

        m_densityTexture = Helper.CreateDensityTexture(32);

		//Profiler.BeginSample ("MyTotalTime");
        SplitIntoChunks();
        runCounter++;
		//Profiler.EndSample ();

        if(runCounter < runs)
        {
            //runCounter++;
            Application.LoadLevel(0);
            return;

        }
        if (DEBUG)
        {
            PrintTimeInfo();  
        }
        
	}
	
	// Update is called once per frame
	void Update () {
        if (runCounter < runs)
            return;

        if (CONTINOUS_CREATION)
        {
            for (int i = 0; i < m_numChunkPerFrame; i++)
            {
                if (m_chunkQueue.Count > 0)
                {
                    Chunk2 c = m_chunkQueue.Dequeue();
                    CreateChunk(c);
                }
                else
                    break;

            }
        }
        if(!DRAW_PROCEDURAL)
        {
            foreach (Chunk2 c in m_chunkList)
            {
                c.Draw();
            }
        }
  
	}
    
    void OnPostRender()
    {
        if (DRAW_PROCEDURAL)
        {
            
            ComputeBuffer args = new ComputeBuffer(4, sizeof(int), ComputeBufferType.DrawIndirect);
            foreach (Chunk2 c in m_chunkList)
            {
                m_procMat.SetPass(0);  
                //m_procMat.SetBuffer("indexBuffer", c.indexBuffer);
                //m_procMat.SetBuffer("vertexBuffer", c.vertexBuffer);
                //m_procMat.SetMatrix("modelMat", Matrix4x4.TRS(c.wsPosLL, Quaternion.identity, c.wsChunkDim));
                //Graphics.DrawProcedural(MeshTopology.Triangles, c.triangleCount);
               
                int[] data = new int[4] { 5000, 1, 0, 0 };
                
                args.SetData(data);
                m_procMat.SetBuffer("triangleBuffer", c.triangleBuffer);
                Graphics.DrawProceduralIndirect(MeshTopology.Points, args);
                
                /*
                data[0] = c.triangleCount * 3;
                args.SetData(data);
                m_procMat.SetBuffer("vertexBuffer", c.triangleBuffer);
                Graphics.DrawProceduralIndirect(MeshTopology.Triangles, args);
                */
                 
                //Graphics.DrawProcedural(MeshTopology.Points, c.triangleCount);
                //print(c.triangleCount);
                
            }
            args.Release();
        }
       
    }
    
    void SplitIntoChunks()
    {
        for (int z = 0; z < m_numberOfChunks.z; z++)
            for (int y = 0; y < m_numberOfChunks.y; y++)
                for (int x = 0; x < m_numberOfChunks.x; x++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    pos *= m_chunkDim;
                    Vector3 cDim = new Vector3(m_chunkDim, m_chunkDim, m_chunkDim);
                    Chunk2 c = new Chunk2(pos, cDim, 32, 1);
                    if (CONTINOUS_CREATION)
                        m_chunkQueue.Enqueue(c);
                    else
                    {
                        CreateChunk(c);   
                    }
                        
                }

    }

    void CreateChunk(Chunk2 c)
    {

        c.chunkMatMesh = m_chunkMat;
        c.chunkMatProcedural = m_procMat;
        float startTime;
        startTime = Time.realtimeSinceStartup;
        BuildChunk(ref c);
        m_totalCreationTime += Time.realtimeSinceStartup - startTime;
        if (!c.isEmpty)
            m_chunkList.Add(c);
        else
            c.Release();
        

    }

    bool BuildChunk(ref Chunk2 chunk)
    {        
        float startTime = Time.realtimeSinceStartup;
		float t;
        int i = 0;
		Profiler.BeginSample ("MyChunkCreation");
        foreach (BasePass pass in m_passes)
        {
            float s = Time.realtimeSinceStartup;
            if (!(pass.DoPass(ref chunk, ref m_densityTexture)))
			{
				t = Time.realtimeSinceStartup - startTime;
				m_emptyTime += t;
				return false;
			}

            
            m_passTimes[i++] += Time.realtimeSinceStartup - s;
        }
		Profiler.EndSample ();
		t = Time.realtimeSinceStartup - startTime;

		m_nonemptyTime += t;
		
		
        if(!DRAW_PROCEDURAL)
        {
            if (m_chosenPass == BuildDensityMap.Passes.INDICES || m_chosenPass == BuildDensityMap.Passes.INDICES_MEDICAL)
            {
                // Copy vertex and index buffers from GPU to CPU so we can generate a Unity Mesh
                
                startTime = Time.realtimeSinceStartup;
				//Profiler.BeginSample ("CopyVBIB");

                // start timer
                Vector3[] vertBuffer = new Vector3[chunk.vertexCount * 2];
                chunk.vertexBuffer.GetData(vertBuffer);

                int[] indices = new int[chunk.indexCount];
                chunk.indexBuffer.GetData(indices);
			//	Profiler.EndSample ();
                m_totalCopyTime += Time.realtimeSinceStartup - startTime;

                // start timer and create mesh
                startTime = Time.realtimeSinceStartup;
				//Profiler.BeginSample("IndexMeshCreation");
                chunk.CreateChunkMeshIndexed(ref vertBuffer, ref indices);
				//Profiler.EndSample();
                m_totalMeshCreationTime += Time.realtimeSinceStartup - startTime;
            }

            else
            {
                // start timer and copy triangle buffer from GPU to CPU
                startTime = Time.realtimeSinceStartup;
				//Profiler.BeginSample("CopyTriBuffer");
                Vector3[] triBuffer = new Vector3[chunk.triangleCount * 6];
                chunk.triangleBuffer.GetData(triBuffer);
                m_totalCopyTime += Time.realtimeSinceStartup - startTime;
				//Profiler.EndSample();
                // start timer and create the mesh
                startTime = Time.realtimeSinceStartup;
				//Profiler.BeginSample("NonemptyMeshCreation");
                chunk.CreateChunkMesh(ref triBuffer);
				//Profiler.EndSample();
                m_totalMeshCreationTime += Time.realtimeSinceStartup - startTime;

            }

        }
        m_tCount += chunk.triangleCount;
        m_vCount += chunk.vertexCount;
        
        return true;

    }
    void PrintTimeInfo()
    {
        float numChunks = m_numberOfChunks.x * m_numberOfChunks.y * m_numberOfChunks.z;
        float emptyChunks = numChunks - m_chunkList.Count;
        m_totalChunkCreationTime = m_emptyTime + m_nonemptyTime;
        float totalNonemptyCreationTime = m_nonemptyTime + m_totalCopyTime + m_totalMeshCreationTime;
        float totTime = m_totalChunkCreationTime + m_totalCopyTime + m_totalMeshCreationTime;
        /*
        // Total time 
        print(emptyChunks + "\n"                                               // empty chunks
          + m_chunkList.Count + "\n"                                           // nonempty chunks
          + m_emptyTime * 1000 + "\n"                                          // time to process empty chunk
          + m_nonemptyTime * 1000 + "\n"                                       // time to go through all compute shaders for nonempty voxels
          + m_totalCopyTime * 1000 + "\n"                                      // Total time to copy vertex buffer back to CPU
          + m_totalMeshCreationTime * 1000 + "\n"                              // total time to create unity mesh
          + (totalNonemptyCreationTime) * 1000 + "\n"                          // total time to create chunk copy back and create mesh
          + m_totalChunkCreationTime * 1000 + "\n"                             // Time to go through all CS for all chunks
          + (totTime) * 1000 + "\n"                                            // total creation time
          );
        */
        // Average time per run
        print(emptyChunks + "\n"                                               // empty chunks
          + m_chunkList.Count + "\n"                                           // nonempty chunks
          + ((m_emptyTime * 1000) / runs) + "\n"                                          // time to process empty chunk
          + ((m_nonemptyTime * 1000) / runs) + "\n"                                       // time to go through all compute shaders for nonempty voxels
          + m_totalCopyTime * 1000 / runs + "\n"                                      // Total time to copy vertex buffer back to CPU
          + m_totalMeshCreationTime * 1000 / runs + "\n"                              // total time to create unity mesh
          + (totalNonemptyCreationTime) * 1000 / runs + "\n"                          // total time to create chunk copy back and create mesh
          + m_totalChunkCreationTime * 1000 / runs + "\n"                             // Time to go through all CS for all chunks
          + (totTime) * 1000 / runs + "\n"                                            // total creation time

          );
        foreach (float f in m_passTimes)
        {
            print(f);
        }
    }
    void OnApplicationQuit()
    {
        Helper.Finalize();
        if (m_passes != null)
        {
            foreach (BasePass pass in m_passes)
                pass.Release();
        }
        if (m_chunkList != null)
        {
            foreach (Chunk2 c in m_chunkList)
            {
                c.Release();
            }
        }
        if (m_densityTexture != null)
            m_densityTexture.Release();
    }
    void OnDestroy()
    {
        if (m_passes != null)
        {
            foreach (BasePass pass in m_passes)
                pass.Release();
        }
        if (m_chunkList != null)
        {
            foreach (Chunk2 c in m_chunkList)
            {
                c.Release();
            }
        }
        if (m_densityTexture != null)
            m_densityTexture.Release();
    }

    int CountVertices()
    {
        int v = 0;

        foreach(Chunk2 c in m_chunkList)
        {
            v += c.GetVertexCount();
        }
        return v;
    }

    int CountTriangles()
    {
        int t = 0;

        foreach (Chunk2 c in m_chunkList)
        {
            t += c.triangleCount;
        }
        return t;
    }
    int GetVRAM()
    {
        int m = 0;
        foreach(Chunk2 c in m_chunkList)
        {
            m += c.GetVRAMUsage();
        }
        return m;
    }
}
