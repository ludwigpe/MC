using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class BuildDensityMap : MonoBehaviour
{
    public enum Passes { NAIVE, APPENDING, NONEMPTY, INDICES, INDICES_MEDICAL, NONEMPTY_MEDICAL};
    public bool DEBUG = true;
    public bool CREATE_GAMEOBJECT = true;
    public bool CONTINOUS_CREATION = true;
    public Passes m_chosenPass;
    const int SIZE = 32 * 32 * 32 * 5 * 3;
	public Material m_chunkMat;
    public Material m_chunkMeshMat;
    public Vector3 m_numberOfChunks = new Vector3(1, 1, 1);
    public int m_chunkDim;
    public int m_voxelDim = 32;
    public int m_numChunkPerFrame = 4;
    private Texture3D m_medicalVol;
    private RenderTexture m_densityTexture;

    private List<Chunk> m_chunkList;
    private Queue<Chunk> m_chunkQueue;
    private Queue<Vector3> m_posQueue;
    private BasePass[] m_passes;

    private GameObject m_worldGameObject;
    private float m_totalChunkCreationTime;
    private float m_totalMeshCreationTime;
    private int maxResolution = 32;
	// Use this for initialization
	void Start () 
    {
      
        int numChunksTotal = (int)(m_numberOfChunks.x * m_numberOfChunks.y * m_numberOfChunks.z);
        m_chunkList = new List<Chunk>(numChunksTotal);
        m_posQueue = new Queue<Vector3>(numChunksTotal);
        m_chunkQueue = new Queue<Chunk>(numChunksTotal);
        switch(m_chosenPass)
        {
            case Passes.NAIVE:
                m_passes = new BasePass[2];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new NaiveGenVerticesPass();
                break;
            case Passes.NONEMPTY:
                m_passes = new BasePass[3];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new NonemptyGenVerticesPass();
                break;
            case Passes.NONEMPTY_MEDICAL:
                m_passes = new BasePass[3];
                m_passes[0] = new BuildDensityMedicalPass();
                m_passes[1] = new ListNonemptyCellsPass();
				m_passes[2] = new NonemptyGenVerticesPass();
                break;
            case Passes.INDICES:
                m_passes = new BasePass[4];
                m_passes[0] = new BuildDensityPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new SplatGenVerticesPass();
                m_passes[3] = new GenIndicesPass();
                break;
            case Passes.INDICES_MEDICAL:
                m_passes = new BasePass[4];
                m_passes[0] = new BuildDensityMedicalPass();
                m_passes[1] = new ListNonemptyCellsPass();
                m_passes[2] = new SplatGenVerticesPass();
                m_passes[3] = new GenIndicesPass();
                break;
        }

        m_densityTexture = Helper.CreateDensityTexture(32);

        float startTime = Time.realtimeSinceStartup;
        m_worldGameObject = new GameObject("World");
        CreateChunks();
        //GenerateChunkQueue();
        CreateLODChunks(2, new Vector3(0, 0, 0), new Vector3(24, 24, 24));
        //if ((m_chosenPass == Passes.APPENDING || m_chosenPass == Passes.NONEMPTY) && m_chunkList.Count > 1)
        //{
        //    int prev = m_chunkList[0].triangleCount;
        //    for (int i = 1; i < m_chunkList.Count; i++)
        //    {
        //        int temp = m_chunkList[i].triangleCount;
        //        m_chunkList[i].triangleCount = prev;
        //        prev = temp;
        //    }
        //    m_chunkList[0].triangleCount = prev;
        //}
        float endTime = Time.realtimeSinceStartup;
         
        if (DEBUG)
        {
            print((float)(endTime - startTime) + "\n"
           + m_totalChunkCreationTime + "\n"
           + m_totalMeshCreationTime + "\n"
           + m_chunkList.Count + "\n"
           + (float)(m_chunkList.Count / m_totalChunkCreationTime) + "\n"
           );
            //print("Total Creation time:" + (float)(endTime - startTime));
            //print("Chunk Creation time: " + m_totalChunkCreationTime);
            //print("Mesh Creation time:" + m_totalMeshCreationTime);
            //print("Number of chunks generated: " + m_chunkList.Count);

            //print("Chunks per second: " + (float)(m_chunkList.Count / m_totalChunkCreationTime));
        }
        
	
	}
    void Update()
    {
        if(CONTINOUS_CREATION)
        {
            for (int i = 0; i < m_numChunkPerFrame; i++)
            {
                if (m_chunkQueue.Count > 0)
                {
                    Chunk c = m_chunkQueue.Dequeue();
                    CreateChunk(c);
                }
                else
                    break;
                
            }
        }
       
        if(!CREATE_GAMEOBJECT)
        {
            foreach(Chunk c in m_chunkList)
            {
                c.Draw();
            }
        }
            
    }

    void OnDrawGizmos()
    {
        if(DEBUG)
        {
            if(m_chunkList != null)
            {
                foreach (Chunk c in m_chunkList)
                {
                    if(c.wsChunkDim.x > 2)
                        Gizmos.color = new Color(1, 0, 0, 0.2F);
                    if (c.wsChunkDim.x == 2)
                        Gizmos.color = new Color(0, 1, 0, 0.2F);
                    if (c.wsChunkDim.x == 1)
                        Gizmos.color = new Color(0, 0, 1, 0.2F);

                    Vector3 pos = c.wsPosLL;
                    pos += (c.wsChunkDim / 2.0f);

                    Gizmos.DrawWireCube(pos, c.wsChunkDim);
                }
            }
            
        }
    }
    void GenerateChunkQueue()
    {
        for (int z = 0; z < m_numberOfChunks.z; z++)
            for (int y = 0; y < m_numberOfChunks.y; y++)
                for (int x = 0; x < m_numberOfChunks.x; x++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    pos *= m_chunkDim;
                    m_chunkQueue.Enqueue(new Chunk(pos, new Vector3(m_chunkDim, m_chunkDim, m_chunkDim), 32));
                }
    }
    void CreateChunks ()
    {
        for(int z = 0; z < m_numberOfChunks.z; z++)
            for(int y = 0; y < m_numberOfChunks.y; y++)
                for(int x = 0; x < m_numberOfChunks.x; x++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    pos *= m_chunkDim;
                    CreateChunk(pos, m_chunkDim);           
                }
    }
    void CreateChunk(Vector3 pos, int chunkDim)
    {
        Vector3 cDim = new Vector3(chunkDim, chunkDim, chunkDim);
        Chunk c = new Chunk(pos, cDim, 32);
        c.chunkMatProcedural = m_chunkMat;
        c.chunkMatMesh = m_chunkMeshMat;
        if (BuildChunk(ref c))
            m_chunkList.Add(c);
        else
            c.Release();
    }
    void CreateChunk(Chunk c)
    {
        c.chunkMatProcedural = m_chunkMat;
        c.chunkMatMesh = m_chunkMeshMat;
        if (BuildChunk(ref c))
            m_chunkList.Add(c);
        else
            c.Release();
    }
    void CreateLODChunks(int lodlvl, Vector3 min, Vector3 max)
    {
        int chunkDim = Mathf.RoundToInt( Mathf.Pow(2,lodlvl) );
        int innerChunkDim = chunkDim/2;
        print("chunkdim: " + chunkDim);
        
        Vector3 innerMin = new Vector3();
        innerMin.x = min.x + chunkDim;
        innerMin.y = min.y + chunkDim;
        innerMin.z = min.z + chunkDim;
        
        Vector3 innerMax = new Vector3();
        innerMax.x = max.x - chunkDim;
        innerMax.y = max.y - chunkDim;
        innerMax.z = max.z - chunkDim;
        if(lodlvl == 0)
        {
            innerMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            innerMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }
        print("min: " + min);
        print("max: " + max);
        
        for (int z = (int)min.z; z < max.z; z+= chunkDim)
            for (int y = (int)min.y; y < max.y; y += chunkDim)
                for (int x = (int)min.x; x < max.x; x += chunkDim)
                {
                    if (x >= innerMin.x && x < innerMax.x && y >= innerMin.y && y < innerMax.y && z >= innerMin.z && z < innerMax.z)
                        continue;
                    else
                    {
                        Vector3 pos = new Vector3(x, y, z);
                        if(CONTINOUS_CREATION)
                            m_chunkQueue.Enqueue(new Chunk(pos, new Vector3(chunkDim, chunkDim, chunkDim), 32));
                        else
                            CreateChunk(pos, chunkDim);
                    }

                    
                }
        if (lodlvl == 0)
            return;
        else
            CreateLODChunks(lodlvl - 1, innerMin, innerMax);
    }
    
	/// <summary>
	/// Builds the chunk given a chunk object and a vertex buffer to put the result in.
	/// </summary>
	/// <param name="vertexBuffer">Vertex buffer.</param>
	/// <param name="chunk">Chunk.</param>
	bool BuildChunk(ref Chunk chunk)
	{
        float startTime = Time.realtimeSinceStartup;
        foreach (BasePass pass in m_passes)
        {
            if (!(pass.DoPass(ref chunk, ref m_densityTexture)))
                return false;
        }
        m_totalChunkCreationTime += Time.realtimeSinceStartup - startTime;
        startTime = Time.realtimeSinceStartup;
        if (m_chosenPass == Passes.INDICES || m_chosenPass == Passes.INDICES_MEDICAL)
        {
            if (CREATE_GAMEOBJECT)
                chunk.GenerateChunkIndexed().transform.parent = m_worldGameObject.transform;
            else
                chunk.CreateChunkMeshIndexed();
        }
            
        else
        {
            if (CREATE_GAMEOBJECT)
                chunk.GenerateChunkObject().transform.parent = m_worldGameObject.transform;
            else
                chunk.CreateChunkMesh();
        }
            
        m_totalMeshCreationTime += Time.realtimeSinceStartup - startTime;
        return true ;

	}

    void OnApplicationQuit()
    {
        Helper.Finalize();
        if(m_passes != null)
        {
            foreach (BasePass pass in m_passes)
                pass.Release();
        }
        if(m_chunkList != null)
        {
            foreach (Chunk c in m_chunkList)
            {
                c.Release();
            }
        }
    
        if(m_densityTexture != null)
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
            foreach (Chunk c in m_chunkList)
            {
                c.Release();
            }
        }
        if (m_densityTexture != null)
            m_densityTexture.Release();
    }
	


}
