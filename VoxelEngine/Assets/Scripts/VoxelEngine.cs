﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System;
using diagnostic = System.Diagnostics;
using System.Linq;

public class VoxelEngine : Singleton<VoxelEngine>
{

    [SerializeField]
    Text infoText;
    [SerializeField]
    int chunkWidth;
    [SerializeField]
    int chunkHeight;
    [SerializeField]
    int chunkDepth;
    [SerializeField]
    int subChunkWidth;
    [SerializeField]
    int subChunkHeight;
    [SerializeField]
    int subChunkDepth;
    [SerializeField]
    float voxelSpacing;
    [SerializeField]
    int loadRadiusXZ;
    [SerializeField]
    int loadRadiusY;
    [SerializeField]
    int unloadRadiusXZ;
    [SerializeField]
    int unloadRadiusY;
    [SerializeField]
    float updateRate;
    [SerializeField]
    GameObject meshObjectPrefab;

    float timeAtLastUpdate;

    List<Coords> initializingChunksQueue;
    List<Coords> meshUpdateQueue;
    List<Coords> loadedChunks;
    Dictionary<Coords, Chunk> chunks;
    List<Coords> loadingChunksQueue;
    List<Coords> unloadingChunksQueue;

    private static object initLock = new object();
    private static object meshUpdateLock = new object();


    public Vector3 playerPos;

    public struct Coords : IEquatable<Coords>
    {
        public Coords(int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }
        public int x;
        public int y;
        public int z;

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Coords))
                return false;

            return Equals((Coords)obj);
        }

        public bool Equals(Coords other)
        {
            return (x == other.x && y == other.y && z == other.z);
        }

        public static bool operator ==(Coords c1, Coords c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(Coords c1, Coords c2)
        {
            return !(c1 == c2);
        }
    }

    public struct ChunkCoords
    {
        public ChunkCoords(int _x, int _y, int _z, int _subX, int _subY, int _subZ)
        {
            x = _x;
            y = _y;
            z = _z;
            subX = _subX;
            subY = _subY;
            subZ = _subZ;
        }
        public int x;
        public int y;
        public int z;
        public int subX;
        public int subY;
        public int subZ;
    }

    public struct Voxel
    {
        public Voxel(int _x, int _y, int _z, float value, Vector3 c)
        {
            x = _x;
            y = _y;
            z = _z;
            isoValue = value;
            center = c;
        }
        public int x;
        public int y;
        public int z;
        public float isoValue;
        public Vector3 center;
    }

    public struct SubChunk
    {
        public SubChunk(int _x, int _y, int _z, Voxel[,,] v)
        {
            x = _x;
            y = _y;
            z = _z;
            voxels = v;
            meshObject = null;
            vertices = null;
            uvs = null;
            triangles = null;
        }
        public int x;
        public int y;
        public int z;
        public Voxel[,,] voxels;
        public GameObject meshObject;
        public Vector3[] vertices;
        public Vector2[] uvs;
        public int[] triangles;
    }

    public struct Chunk
    {
        public Chunk(int _x, int _y, int _z, SubChunk[,,] sc)
        {
            x = _x;
            y = _y;
            z = _z;
            subChunks = sc;
        }
        public int x;
        public int y;
        public int z;
        public SubChunk[,,] subChunks;

        public override bool Equals(object obj)
        {
            return (((Chunk)obj).x == this.x &&
                    ((Chunk)obj).y == this.y &&
                    ((Chunk)obj).z == this.z);
        }

        public override int GetHashCode()
        {
            return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
        }
    }

    public enum FaceDir { y_positiv, y_negativ, x_positiv, x_negativ, z_positiv, z_negativ };

    // Use this for initialization
    void Start()
    {
        // Initialize queues, lists, and dictionaries
        initializingChunksQueue = new List<Coords>();
        meshUpdateQueue = new List<Coords>();
        loadedChunks = new List<Coords>();
        chunks = new Dictionary<Coords, Chunk>();
        loadingChunksQueue = new List<Coords>();
        unloadingChunksQueue = new List<Coords>();

        /*var closeChunks = GetCloseChunks(playerPos, 10, 2);
        foreach(var chunk in closeChunks)
        {
            initializingChunksQueue.Add(chunk);
            InitializeChunk(chunk);
        }
        */
        var sw = new diagnostic.Stopwatch();
        sw.Start();
        for(int i = 0; i < 10000; i++)
            GetIsoValue(playerPos);
        sw.Stop();
        Debug.Log("Calculate IsoValue time: " + sw.Elapsed);
        sw.Reset();
        sw.Start();
        GetChunkCoords(playerPos);
        sw.Stop();
        Debug.Log("Calculate chunkCoords: " + sw.Elapsed);
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(CalculateIsoValue(playerPos));
        if (Time.time - timeAtLastUpdate > updateRate)
        {
            UpdateQueues();
            UpdateChunkMeshes();
            UpdateUnloadChunks();

            timeAtLastUpdate = Time.time;
        }

        if (Input.GetKeyDown("r"))
        {
            foreach (var chunk in loadedChunks)
            {
                var coords = new Coords(chunk.x, chunk.y, chunk.z);
                if (!unloadingChunksQueue.Contains(coords))
                    unloadingChunksQueue.Add(coords);
            }
            loadedChunks.Clear();
        }

        if (Input.GetKeyDown("i"))
        {
            var tmp = GetIsoValue(playerPos);
            Debug.Log(tmp);
        }
        //Debug.Log("grad: " + GetGradient(playerPos));
        if (Input.GetKeyDown("g"))
        {
            Ray ray = new Ray(playerPos, Camera.main.transform.forward);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {

                var chunkCoords = GetAllChunkCoordsInCircle(hit.point, 3.0f);
                Debug.Log("Found " + chunkCoords.Length + " chunkCoords");
                foreach (var chunkCoord in chunkCoords)
                {
                    //Debug.Log(chunkCoord.x + " " + chunkCoord.y + " " + chunkCoord.z + " sub: " + chunkCoord.subX + " " + chunkCoord.subY + " " + chunkCoord.subZ);
                    var chunk = chunks[new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z)];
                    var subChunk = chunk.subChunks[chunkCoord.subX, chunkCoord.subY, chunkCoord.subZ];
                    for (int i = 0; i < subChunkWidth; i++)
                    {
                        for (int j = 0; j < subChunkHeight; j++)
                        {
                            for (int w = 0; w < subChunkDepth; w++)
                            {
                                if (Vector3.Distance(subChunk.voxels[i, j, w].center, hit.point) < 3f)
                                {
                                    subChunk.voxels[i, j, w].isoValue += 0.5f;
                                    if (subChunk.voxels[i, j, w].isoValue > 5f)
                                        subChunk.voxels[i, j, w].isoValue = 5f;
                                }
                            }
                        }
                    }
                    if (loadedChunks.Contains(new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z)))
                        loadedChunks.Remove(new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z));
                    //if(!loadingChunksQueue.Contains(new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z)))
                    //  loadingChunksQueue.Add(new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z));
                }
            }

        }
        if (Input.GetKeyDown("f"))
        {
            Ray ray = new Ray(playerPos, Camera.main.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {

                var chunkCoords = GetAllChunkCoordsInCircle(hit.point, 1.0f);
                Debug.Log("Found " + chunkCoords.Length + " chunkCoords");
                foreach (var chunkCoord in chunkCoords)
                {
                    //Debug.Log(chunkCoord.x + " " + chunkCoord.y + " " + chunkCoord.z + " sub: " + chunkCoord.subX + " " + chunkCoord.subY + " " + chunkCoord.subZ);
                    var chunk = chunks[new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z)];
                    var subChunk = chunk.subChunks[chunkCoord.subX, chunkCoord.subY, chunkCoord.subZ];
                    for (int i = 0; i < subChunkWidth; i++)
                    {
                        for (int j = 0; j < subChunkHeight; j++)
                        {
                            for (int w = 0; w < subChunkDepth; w++)
                            {
                                if (Vector3.Distance(subChunk.voxels[i, j, w].center, hit.point) <= 1f)
                                {
                                    subChunk.voxels[i, j, w].isoValue -= 0.5f;
                                    if (subChunk.voxels[i, j, w].isoValue < -5f)
                                        subChunk.voxels[i, j, w].isoValue = -5f;
                                }
                            }
                        }
                    }
                    if (loadedChunks.Contains(new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z)))
                        loadedChunks.Remove(new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z));
                    //if(!loadingChunksQueue.Contains(new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z)))
                    //  loadingChunksQueue.Add(new Coords(chunkCoord.x, chunkCoord.y, chunkCoord.z));
                }
            }

        }
    }

    private void LateUpdate()
    {
        UpdateInfoText();
    }

    void UpdateQueues()
    {
        if (loadingChunksQueue.Count > 5 || meshUpdateQueue.Count > 5 || unloadingChunksQueue.Count > 5 || initializingChunksQueue.Count > 5)
            return;
        // Update loading and initalizing queue
        var closeChunks = GetCloseChunks(playerPos);
        lock (initLock)
        {
            for (int i = 0; i < closeChunks.Length; i++)
            {
                if (!loadedChunks.Contains(closeChunks[i]) &&
                    !loadingChunksQueue.Contains(closeChunks[i]) &&
                    !initializingChunksQueue.Contains(closeChunks[i]))
                {
                    if (chunks.ContainsKey(closeChunks[i]))
                    {
                        // Add it to the loading queue
                        var chunk = chunks[closeChunks[i]];
                        loadingChunksQueue.Add(closeChunks[i]);
                        var thread = new Thread(w => {
                            CalculateVertexDataChunk(chunk);
                        });
                        thread.IsBackground = false;
                        thread.Start();
                        
                        return;
                    }
                    else
                    {
                        // Not initialized chunk, add it to the initialization queue
                        initializingChunksQueue.Add(closeChunks[i]);
                        var coords = new Coords(closeChunks[i].x, closeChunks[i].y, closeChunks[i].z);
                        //ThreadPool.QueueUserWorkItem(work =>
                        //{
                            InitializeChunk(coords);
                        //});
                        return;
                    }
                }
            }
        }
        // Update unloading queue
        UpdateChunksToBeUnloaded();
    }

    void UpdateUnloadChunks()
    {
        if (unloadingChunksQueue.Count > 0)
        {
            var coords = unloadingChunksQueue[0];
            var chunk = chunks[coords];
            UnLoadChunk(chunk);
            unloadingChunksQueue.RemoveAt(0);
        }
    }

    void UpdateChunkMeshes()
    {
        // Update/ Create meshes for all sub meshes
        if (meshUpdateQueue.Count > 0)
        {
            lock (meshUpdateLock)
            {
                var coords = meshUpdateQueue[0];
                if (chunks.ContainsKey(coords))
                {
                    var chunk = chunks[coords];
                    for (int i = 0; i < subChunkWidth; i++)
                    {
                        for (int j = 0; j < subChunkHeight; j++)
                        {
                            for (int w = 0; w < subChunkDepth; w++)
                            {
                                UpdateSubChunk(ref chunk.subChunks[i, j, w], chunk.x, chunk.y, chunk.z);
                            }
                        }
                    }
                    loadedChunks.Add(coords);
                }
                loadingChunksQueue.Remove(coords);
                meshUpdateQueue.RemoveAt(0);
            }
        }
    }

    void InitializeChunk(Coords coords)
    {
        SubChunk[,,] subChunks = new SubChunk[subChunkWidth, subChunkHeight, subChunkDepth];

        for (int i = 0; i < subChunkWidth; i++)
        {
            for (int j = 0; j < subChunkHeight; j++)
            {
                for (int w = 0; w < subChunkDepth; w++)
                {
                    Voxel[,,] voxels = new Voxel[subChunkWidth, subChunkHeight, subChunkDepth];
                    for (int i2 = 0; i2 < subChunkWidth; i2++)
                    {
                        for (int j2 = 0; j2 < subChunkHeight; j2++)
                        {
                            for (int w2 = 0; w2 < subChunkDepth; w2++)
                            {
                                var worldPos = VoxelCoords2WorldPos(new ChunkCoords(coords.x, coords.y, coords.z, i, j, w), i2, j2, w2);
                                voxels[i2, j2, w2] = new Voxel(i2, j2, w2, CalculateIsoValue(worldPos), new Vector3((coords.x * chunkWidth) + (i * subChunkWidth) + (i2 * voxelSpacing), (coords.y * chunkHeight) + (j * subChunkHeight) + (j2 * voxelSpacing), (coords.z * chunkDepth) + (w * subChunkDepth) + (w2 * voxelSpacing)));
                            }
                        }
                    }
                    subChunks[i, j, w] = new SubChunk(i, j, w, voxels);
                }
            }
        }

        var newChunk = new Chunk(coords.x, coords.y, coords.z, subChunks);
        lock (initializingChunksQueue)
        {
            chunks.Add(coords, newChunk);
            initializingChunksQueue.Remove(coords);
        }
    }

    void UpdateChunksToBeUnloaded()
    {
        var currentChunkCoords = GetChunkCoords(playerPos);

        for (int i = 0; i < loadedChunks.Count; i++)
        {
            // Check if loaded chunk is too far away
            if ((Mathf.Abs(currentChunkCoords.x - loadedChunks[i].x) >= unloadRadiusXZ ||
               Mathf.Abs(currentChunkCoords.y - loadedChunks[i].y) >= unloadRadiusY ||
               Mathf.Abs(currentChunkCoords.z - loadedChunks[i].z) >= unloadRadiusXZ) &&
               !unloadingChunksQueue.Contains(new Coords(loadedChunks[i].x, loadedChunks[i].y, loadedChunks[i].z)))
            {
                unloadingChunksQueue.Add(new Coords(loadedChunks[i].x, loadedChunks[i].y, loadedChunks[i].z));
            }
        }
    }

    ChunkCoords[] GetAllChunkCoordsInCircle(Vector3 pos, float radius)
    {
        var chunkCoords = GetChunkCoords(pos);

        var r = Mathf.Max((int)((radius / voxelSpacing) / subChunkWidth),1);
        var outCoords = new List<ChunkCoords>();
        outCoords.Add(chunkCoords);
        for (int i = -r; i <= r; i++)
        {
            for (int j = -r; j <= r; j++)
            {
                for (int w = -r; w <= r; w++)
                {
                    var cCoords = new Coords(chunkCoords.x, chunkCoords.y, chunkCoords.z);
                    var x = chunkCoords.subX + i;
                    while (x < 0)
                    {
                        x += subChunkWidth;
                        cCoords.x -= 1;
                    }
                    while (x >= subChunkWidth)
                    {
                        x -= subChunkWidth;
                        cCoords.x += 1;
                    }
                    var y = chunkCoords.subY + j;
                    while (y < 0)
                    {
                        y += subChunkHeight;
                        cCoords.y -= 1;
                    }
                    while (y >= subChunkHeight)
                    {
                        y -= subChunkHeight;
                        cCoords.y += 1;
                    }
                    var z = chunkCoords.subZ + w;
                    while (z < 0)
                    {
                        z += subChunkDepth;
                        cCoords.z -= 1;
                    }
                    while (z >= subChunkDepth)
                    {
                        z -= subChunkDepth;
                        cCoords.z += 1;
                    }

                    if (chunks.ContainsKey(cCoords))
                    {
                        var chunk = chunks[cCoords];
                        var outCoord = new ChunkCoords(cCoords.x, cCoords.y, cCoords.z, x, y, z);
                        if(!outCoords.Contains(outCoord))
                            outCoords.Add(outCoord);
                    }
                }
            }
        }
        return outCoords.ToArray();
    }

    Coords[] GetCloseChunks(Vector3 pos, int radXZ = 0, int radY = 0)
    {
        var currentChunkCoords = GetChunkCoords(pos);
        var result = new List<Coords>();
        if (radXZ == 0)
            radXZ = loadRadiusXZ;
        if (radY == 0)
            radY = loadRadiusY;
        for (int i = -radXZ; i <= radXZ; i++)
        {
            for (int j = -radY; j <= radY; j++)
            {
                for (int w = -radXZ; w <= radXZ; w++)
                {
                    var coords = new Coords(currentChunkCoords.x + i,
                                            currentChunkCoords.y + j,
                                            currentChunkCoords.z + w);
                    if(!result.Contains(coords))
                        result.Add(coords);
                }
            }
        }
        result = result.OrderBy(x => Vector3.Distance(new Vector3(x.x * chunkWidth * voxelSpacing, x.y * chunkHeight * voxelSpacing, x.z * chunkDepth * voxelSpacing), playerPos)).ToList();
        return result.ToArray();
    }

    void UnLoadChunk(Chunk chunk)
    {
        for (int i = 0; i < subChunkWidth; i++)
        {
            for (int j = 0; j < subChunkHeight; j++)
            {
                for (int w = 0; w < subChunkDepth; w++)
                {
                    if (chunk.subChunks[i, j, w].meshObject != null)
                        Destroy(chunk.subChunks[i, j, w].meshObject);
                }
            }
        }
        loadedChunks.Remove(new Coords(chunk.x, chunk.y, chunk.z));
    }

    void CalculateVertexDataChunk(Chunk chunk)
    {
        
        for (int i = 0; i < subChunkWidth; i++)
        {
            for (int j = 0; j < subChunkHeight; j++)
            {
                for (int w = 0; w < subChunkDepth; w++)
                {
                    CalculateVertexDataSubChunk(ref chunk.subChunks[i, j, w], chunk.x, chunk.y, chunk.z);
                }
            }
        }
        

        lock (meshUpdateLock)
        {
            

            if (!meshUpdateQueue.Contains(new Coords(chunk.x, chunk.y, chunk.z)))
            {
                meshUpdateQueue.Add(new Coords(chunk.x, chunk.y, chunk.z));
            } else
            {
                Debug.LogError("Trying to add an already queued update");
            }
        }
    }

    void CalculateVertexDataSubChunk(ref SubChunk subChunk, int chunkX, int chunkY, int chunkZ)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < subChunkWidth; i++)
        {
            for (int j = 0; j < subChunkHeight; j++)
            {
                for (int w = 0; w < subChunkDepth; w++)
                {
                    if (subChunk.voxels[i, j, w].isoValue >= 0f)
                    {

                        var chunkCoords = new ChunkCoords(chunkX, chunkY, chunkZ, subChunk.x, subChunk.y, subChunk.z);
                        var vCoords = new Coords(i, j, w);

                        Voxel? positivX = null;
                        if (i + 1 >= subChunkWidth)
                        {
                            positivX = GetNeighborVoxel(chunkCoords, vCoords, 1, 0, 0);
                        }
                        else
                        {
                            positivX = subChunk.voxels[i + 1, j, w];
                        }
                        if (positivX != null && positivX.Value.isoValue < 0f)
                        {
                            CreateFace(FaceDir.x_positiv, new Vector3(i,j,w), vertices, uvs, triangles);
                        }

                        Voxel? negativX = null;
                        if (i - 1 < 0)
                        {
                            negativX = GetNeighborVoxel(chunkCoords, vCoords, -1, 0, 0);
                        }
                        else
                        {
                            negativX = subChunk.voxels[i - 1, j, w];
                        }
                        if (negativX != null && negativX.Value.isoValue < 0f)
                        {
                            CreateFace(FaceDir.x_negativ, new Vector3(i, j, w), vertices, uvs, triangles);
                        }
                        Voxel? positivY = null;
                        if (j + 1 >= subChunkHeight)
                        {
                            positivY = GetNeighborVoxel(chunkCoords, vCoords, 0, 1, 0);
                        }
                        else
                        {
                            positivY = subChunk.voxels[i, j + 1, w];
                        }
                        if (positivY != null && positivY.Value.isoValue < 0f)
                        {
                            CreateFace(FaceDir.y_positiv, new Vector3(i, j, w), vertices, uvs, triangles);
                        }
                        Voxel? negativY = null;
                        if (j - 1 < 0)
                        {
                            negativY = GetNeighborVoxel(chunkCoords, vCoords, 0, -1, 0);
                        }
                        else
                        {
                            negativY = subChunk.voxels[i, j - 1, w];
                        }
                        if (negativY != null && negativY.Value.isoValue < 0f)
                        {
                            CreateFace(FaceDir.y_negativ, new Vector3(i, j, w), vertices, uvs, triangles);
                        }
                        Voxel? positivZ = null;
                        if (w + 1 >= subChunkDepth)
                        {
                            positivZ = GetNeighborVoxel(chunkCoords, vCoords, 0, 0, 1);
                        }
                        else
                        {
                            positivZ = subChunk.voxels[i, j, w + 1];
                        }
                        if (positivZ != null && positivZ.Value.isoValue < 0f)
                        {
                            CreateFace(FaceDir.z_positiv, new Vector3(i, j, w), vertices, uvs, triangles);
                        }
                        Voxel? negativZ = null;
                        if (w - 1 < 0)
                        {
                            negativZ = GetNeighborVoxel(chunkCoords, vCoords, 0, 0, -1);
                        }
                        else
                        {
                            negativZ = subChunk.voxels[i, j, w - 1];
                        }
                        if (negativZ != null && negativZ.Value.isoValue < 0f)
                        {
                            CreateFace(FaceDir.z_negativ, new Vector3(i, j, w), vertices, uvs, triangles);
                        }
                    }
                }
            }
        }
        if (vertices.Count > 0)
        {
            var chunkCoords = new ChunkCoords(chunkX, chunkY, chunkZ, subChunk.x, subChunk.y, subChunk.z);

            PushToSurface(vertices, 
                new Vector3((chunkX * chunkWidth) + (subChunk.x * subChunkWidth), 
                (chunkY * chunkHeight) + (subChunk.y * subChunkHeight)
                ,(chunkZ * chunkDepth) + (subChunk.z * subChunkDepth)),
                chunkCoords);
            subChunk.vertices = vertices.ToArray();
            subChunk.uvs = uvs.ToArray();
            subChunk.triangles = triangles.ToArray();
        }
    }

    public Vector3 GetGradient(Vector3 pos, ChunkCoords chunkCoords)
    {
        return new Vector3(
                    (GetIsoValue((pos + new Vector3(0.5f, 0.0f, 0.0f))) - GetIsoValue((pos - new Vector3(0.5f, 0.0f, 0.0f)))) / 2f,
                    (GetIsoValue((pos + new Vector3(0.0f, 0.5f, 0.0f))) - GetIsoValue((pos - new Vector3(0.0f, 0.5f, 0.0f)))) / 2f,
                    (GetIsoValue((pos + new Vector3(0.0f, 0.0f, 0.5f))) - GetIsoValue((pos - new Vector3(0.0f, 0.0f, 0.5f)))) / 2f
                    );
    }

    Vector3 PushToSurface(Vector3 vertex, Vector3 pos, ChunkCoords chunkCoords)
    {
        var gradient = GetGradient(pos + vertex, chunkCoords) * 2f;
        // Debug.Log(gradient);
        if (gradient.magnitude > 0.01f)
            vertex += -gradient * (GetIsoValue(pos + vertex)) / (gradient.magnitude * gradient.magnitude);

        return vertex;
    }
    
    void PushToSurface(List<Vector3> vertices, Vector3 pos, ChunkCoords chunkCoords)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = PushToSurface(vertices[i], pos, chunkCoords);
        }
    }

    void UpdateSubChunk(ref SubChunk subChunk, int chunkX, int chunkY, int chunkZ)
    {
        
        if (subChunk.vertices != null && subChunk.vertices.Length > 0)
        {
            if (subChunk.meshObject == null)
            {
                subChunk.meshObject = Instantiate(meshObjectPrefab, ChunkCoords2WorldPos(new ChunkCoords(chunkX, chunkY, chunkZ, subChunk.x, subChunk.y, subChunk.z)), Quaternion.identity) as GameObject;
            }

            var mesh = new Mesh();

            mesh.vertices = subChunk.vertices;
            mesh.uv = subChunk.uvs;
            mesh.triangles = subChunk.triangles;

            subChunk.vertices = null;
            subChunk.uvs = null;
            subChunk.triangles = null;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            
            subChunk.meshObject.GetComponent<MeshFilter>().mesh = mesh;
            subChunk.meshObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        } else if(subChunk.meshObject != null)
        {
            Destroy(subChunk.meshObject);
        }
    }

    Vector3 VoxelCoords2WorldPos(ChunkCoords chunkCoords, int x, int y, int z)
    {
        var chunkPos = ChunkCoords2WorldPos(chunkCoords);
        return chunkPos + new Vector3(x * voxelSpacing, y * voxelSpacing, z * voxelSpacing);
    }

    Vector3 ChunkCoords2WorldPos(ChunkCoords chunkCoords)
    {

        var chunkOrigin = new Vector3(chunkWidth * voxelSpacing * chunkCoords.x,
                                        chunkHeight * voxelSpacing * chunkCoords.y,
                                        chunkDepth * voxelSpacing * chunkCoords.z);

        return new Vector3(chunkOrigin.x + (chunkCoords.subX * subChunkWidth * voxelSpacing),
                            chunkOrigin.y + (chunkCoords.subY * subChunkHeight * voxelSpacing),
                            chunkOrigin.z + (chunkCoords.subZ * subChunkDepth * voxelSpacing));
    }

    float CalculateIsoValue(Vector3 pos)
    {
        var value = (Noise.Perlin2D(new Vector2(pos.x,pos.z), 0.05f) * 5f);
        value = (pos.y - 25f)- value;
        if (value > 5f)
            value = 5f;
        if (value < -5f)
            value = -5f;
            
        return value;
    }

    Voxel? GetNeighborVoxel(ChunkCoords chunkCoords, Coords voxelCoords, int nX, int nY, int nZ)
    {

        var chunkX = chunkCoords.x;
        var subX = chunkCoords.subX;
        int vX = voxelCoords.x + nX;
        var chunkY = chunkCoords.y;
        var subY = chunkCoords.subY;
        int vY = voxelCoords.y + nY;
        var chunkZ = chunkCoords.z;
        var subZ = chunkCoords.subZ;
        int vZ = voxelCoords.z + nZ;

        #region positive
        if (vX >= subChunkWidth)
        {
            subX++;
            vX = 0;
        }
        if (subX >= subChunkWidth)
        {
            chunkX++;
            subX = 0;
        }
        if (vY >= subChunkHeight)
        {
            subY++;
            vY = 0;
        }
        if (subY >= subChunkHeight)
        {
            chunkY++;
            subY = 0;
        }
        if (vZ >= subChunkDepth)
        {
            subZ++;
            vZ = 0;
        }
        if (subZ >= subChunkDepth)
        {
            chunkZ++;
            subZ = 0;
        }
        #endregion

        #region negative
        if (vX < 0)
        {
            subX--;
            vX = subChunkWidth - 1;
        }
        if (subX < 0)
        {
            chunkX--;
            subX = subChunkWidth - 1;
        }
        if (vY < 0)
        {
            subY--;
            vY = subChunkHeight - 1;
        }
        if (subY < 0)
        {
            chunkY--;
            subY = subChunkHeight - 1;
        }
        if (vZ < 0)
        {
            subZ--;
            vZ = subChunkDepth - 1;
        }
        if (subZ < 0)
        {
            chunkZ--;
            subZ = subChunkDepth - 1;
        }
        #endregion

        var c = new Coords(chunkX, chunkY, chunkZ);
        if (!chunks.ContainsKey(c))
        {
            return null;
        }

        return chunks[c].subChunks[subX, subY, subZ].voxels[vX, vY, vZ];
    }

    Coords GetVoxelWorldCoords(ChunkCoords chunkCoords, Coords localVoxelCoords)
    {

        var worldX = Mathf.FloorToInt((chunkCoords.x * chunkWidth) +
            (chunkCoords.subX * subChunkWidth) +
            (localVoxelCoords.x));
        var worldY = Mathf.FloorToInt((chunkCoords.y * chunkHeight) +
            (chunkCoords.subY * subChunkHeight) +
            (localVoxelCoords.y));
        var worldZ = Mathf.FloorToInt((chunkCoords.z * chunkDepth) +
            (chunkCoords.subZ * subChunkDepth) +
            (localVoxelCoords.z));

        return new Coords(worldX, worldY, worldZ);
    }

    ChunkCoords VoxelWorldCoords2ChunkCoords(Coords voxelWorldCoords, out Coords voxelLocalCoords)
    {

        var chunkX = Mathf.FloorToInt(voxelWorldCoords.x / chunkWidth);
        var chunkY = Mathf.FloorToInt(voxelWorldCoords.y / chunkHeight);
        var chunkZ = Mathf.FloorToInt(voxelWorldCoords.z / chunkDepth);

        var subChunkX = Mathf.FloorToInt((voxelWorldCoords.x - (chunkX * chunkWidth)) / subChunkWidth);
        var subChunkY = Mathf.FloorToInt((voxelWorldCoords.y - (chunkY * chunkHeight)) / subChunkHeight);
        var subChunkZ = Mathf.FloorToInt((voxelWorldCoords.z - (chunkZ * chunkDepth)) / subChunkDepth);

        voxelLocalCoords = new Coords(voxelWorldCoords.x - (chunkX * chunkWidth) - (subChunkX * subChunkWidth),
                                        voxelWorldCoords.y - (chunkY * chunkHeight) - (subChunkY * subChunkHeight),
                                        voxelWorldCoords.z - (chunkZ * chunkDepth) - (subChunkZ * subChunkDepth));

        return new ChunkCoords(chunkX, chunkY, chunkZ, subChunkX, subChunkY, subChunkZ);

    }

    float GetIsoValueFromVoxelWorld(Coords voxelWorldCoords)
    {
        Coords voxelLocal;
        ChunkCoords chunkCoords = VoxelWorldCoords2ChunkCoords(voxelWorldCoords, out voxelLocal);
        Coords coords = new Coords(chunkCoords.x, chunkCoords.y, chunkCoords.z);
        if (!chunks.ContainsKey(coords))
        {
            return 0;
        }

        var tmp = chunks[coords].subChunks[chunkCoords.subX, chunkCoords.subY, chunkCoords.subZ];
        return 0;
    }

    Voxel GetVoxel(ChunkCoords chunkCoords, int x, int y, int z)
    {
        var coords = new Coords(chunkCoords.x, chunkCoords.y, chunkCoords.z);
        if (!chunks.ContainsKey(coords))
        {
            return new Voxel(0, 0, 0, 0, Vector3.zero);
        }

        return chunks[coords].subChunks[chunkCoords.subX, chunkCoords.subY, chunkCoords.subZ].voxels[x, y, z];

    }

    float GetIsoValue(ChunkCoords chunkCoords, int x, int y, int z)
    {
        //return 1f;
        var coords = new Coords(chunkCoords.x, chunkCoords.y, chunkCoords.z);
        lock (initLock)
        {
            if (!chunks.ContainsKey(coords) && !initializingChunksQueue.Contains(coords))
        {
            
                InitializeChunk(coords);
            
        }
        }
        var chunk = chunks[coords];
        var subChunk = chunk.subChunks[chunkCoords.subX, chunkCoords.subY, chunkCoords.subZ];

        return subChunk.voxels[x, y, z].isoValue;
    }

    float GetIsoValue(Vector3 pos)
    {
        int vX, vY, vZ;
        var chunkCoords = GetChunkCoords(pos, out vX, out vY, out vZ);

        int vX_100, vY_100, vZ_100;
        var chunkCoords_100 = GetChunkCoords(new Vector3(pos.x + voxelSpacing, pos.y, pos.z), out vX_100, out vY_100, out vZ_100);

        int vX_110, vY_110, vZ_110;
        var chunkCoords_110 = GetChunkCoords(new Vector3(pos.x + voxelSpacing, pos.y + voxelSpacing, pos.z), out vX_110, out vY_110, out vZ_110);

        int vX_010, vY_010, vZ_010;
        var chunkCoords_010 = GetChunkCoords(new Vector3(pos.x, pos.y + voxelSpacing, pos.z), out vX_010, out vY_010, out vZ_010);
        
        int vX_001, vY_001, vZ_001;
        var chunkCoords_001 = GetChunkCoords(new Vector3(pos.x, pos.y, pos.z + voxelSpacing), out vX_001, out vY_001, out vZ_001);

        int vX_101, vY_101, vZ_101;
        var chunkCoords_101 = GetChunkCoords(new Vector3(pos.x + voxelSpacing, pos.y, pos.z + voxelSpacing), out vX_101, out vY_101, out vZ_101);

        int vX_011, vY_011, vZ_011;
        var chunkCoords_011 = GetChunkCoords(new Vector3(pos.x, pos.y + voxelSpacing, pos.z + voxelSpacing), out vX_011, out vY_011, out vZ_011);

        int vX_111, vY_111, vZ_111;
        var chunkCoords_111 = GetChunkCoords(new Vector3(pos.x + voxelSpacing, pos.y + voxelSpacing, pos.z + voxelSpacing), out vX_111, out vY_111, out vZ_111);

        var coords = new Coords(chunkCoords.x, chunkCoords.y, chunkCoords.z);
        var coords_100 = new Coords(chunkCoords_100.x, chunkCoords_100.y, chunkCoords_100.z);
        var coords_110 = new Coords(chunkCoords_110.x, chunkCoords_110.y, chunkCoords_110.z);
        var coords_010 = new Coords(chunkCoords_010.x, chunkCoords_010.y, chunkCoords_010.z);
        var coords_001 = new Coords(chunkCoords_001.x, chunkCoords_001.y, chunkCoords_001.z);
        var coords_101 = new Coords(chunkCoords_001.x, chunkCoords_001.y, chunkCoords_001.z);
        var coords_011 = new Coords(chunkCoords_011.x, chunkCoords_011.y, chunkCoords_011.z);
        var coords_111 = new Coords(chunkCoords_111.x, chunkCoords_111.y, chunkCoords_111.z);
        
        var tx = (pos.x - ((vX * voxelSpacing) + (chunkCoords.x * chunkWidth) + (chunkCoords.subX * subChunkWidth))) / voxelSpacing;
        var val_000_100 = ((1 - tx) * GetIsoValue(chunkCoords, vX, vY, vZ)) + (tx * GetIsoValue(chunkCoords_100, vX_100, vY_100, vZ_100));

        var val_010_110 = ((1 - tx) * GetIsoValue(chunkCoords_010, vX_010, vY_010, vZ_010)) + (tx * GetIsoValue(chunkCoords_110, vX_110, vY_110, vZ_110));

        var ty = (pos.y - ((vY * voxelSpacing) + (chunkCoords.y * chunkHeight) + (chunkCoords.subY * subChunkHeight))) / voxelSpacing;
        var val_mid_1 = ((1 - ty) * val_000_100) + (ty * val_010_110);

        var val_001_101 = ((1 - tx) * GetIsoValue(chunkCoords_001, vX_001, vY_001, vZ_001)) + (tx * GetIsoValue(chunkCoords_101, vX_101, vY_101, vZ_101));

        var val_011_111 = ((1 - tx) * GetIsoValue(chunkCoords_011, vX_011, vY_011, vZ_011)) + (tx * GetIsoValue(chunkCoords_111, vX_111, vY_111, vZ_111));

        var val_mid_2 = ((1 - ty) * val_001_101) + (ty * val_011_111);

        var tz = (pos.z - ((vZ * voxelSpacing) + (chunkCoords.z * chunkDepth) + (chunkCoords.subZ * subChunkDepth))) / voxelSpacing;
        
        return ((1 - tz) * val_mid_1) + (tz * val_mid_2);
    }

    ChunkCoords GetChunkCoords(Vector3 pos)
    {
        var chunkCoords = new ChunkCoords();

        chunkCoords.x = Mathf.FloorToInt(pos.x / (chunkWidth * voxelSpacing));
        chunkCoords.y = Mathf.FloorToInt(pos.y / (chunkHeight * voxelSpacing));
        chunkCoords.z = Mathf.FloorToInt(pos.z / (chunkDepth * voxelSpacing));

        var chunkOrigin = new Vector3(chunkWidth * voxelSpacing * chunkCoords.x,
                                        chunkHeight * voxelSpacing * chunkCoords.y,
                                        chunkDepth * voxelSpacing * chunkCoords.z);

        chunkCoords.subX = Mathf.FloorToInt((pos.x - chunkOrigin.x) / (subChunkWidth * voxelSpacing));
        chunkCoords.subY = Mathf.FloorToInt((pos.y - chunkOrigin.y) / (subChunkHeight * voxelSpacing));
        chunkCoords.subZ = Mathf.FloorToInt((pos.z - chunkOrigin.z) / (subChunkDepth * voxelSpacing));

        return chunkCoords;
    }

    ChunkCoords GetChunkCoords(Vector3 pos, out int vX, out int vY, out int vZ)
    {
        var chunkCoords = new ChunkCoords();
        
        chunkCoords.x = Mathf.FloorToInt(pos.x / (chunkWidth * voxelSpacing));
        chunkCoords.y = Mathf.FloorToInt(pos.y / (chunkHeight * voxelSpacing));
        chunkCoords.z = Mathf.FloorToInt(pos.z / (chunkDepth * voxelSpacing));

        var chunkOrigin = new Vector3(chunkWidth * voxelSpacing * chunkCoords.x,
                                        chunkHeight * voxelSpacing * chunkCoords.y,
                                        chunkDepth * voxelSpacing * chunkCoords.z);

        chunkCoords.subX = Mathf.FloorToInt((pos.x - chunkOrigin.x) / (subChunkWidth * voxelSpacing));
        chunkCoords.subY = Mathf.FloorToInt((pos.y - chunkOrigin.y) / (subChunkHeight * voxelSpacing));
        chunkCoords.subZ = Mathf.FloorToInt((pos.z - chunkOrigin.z) / (subChunkDepth * voxelSpacing));

        var subChunkOrigin = new Vector3(subChunkWidth * voxelSpacing * chunkCoords.subX,
                                        subChunkHeight * voxelSpacing * chunkCoords.subY,
                                        subChunkDepth * voxelSpacing * chunkCoords.subZ);

        vX = Mathf.FloorToInt((pos.x - chunkOrigin.x - subChunkOrigin.x) / (voxelSpacing));
        vY = Mathf.FloorToInt((pos.y - chunkOrigin.y - subChunkOrigin.y) / (voxelSpacing));
        vZ = Mathf.FloorToInt((pos.z - chunkOrigin.z - subChunkOrigin.z) / (voxelSpacing));
        
        return chunkCoords;
    }

    void CreateFace(FaceDir dir, Vector3 center, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
    {

        int triOffset = triangles.Count;
        switch (dir)
        {
            case FaceDir.y_positiv:
                vertices.Add(center + new Vector3(-0.5f, 0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));
                vertices.Add(center + new Vector3(-0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(0f, 0f));

                vertices.Add(center + new Vector3(-0.5f, 0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, 0.5f, 0.5f));
                uvs.Add(new Vector2(1f, 1f));
                vertices.Add(center + new Vector3(0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));

                triangles.AddRange(new int[] { triOffset + 2, triOffset + 1, triOffset, triOffset + 5, triOffset + 4, triOffset + 3 });

                break;
            case FaceDir.y_negativ:
                vertices.Add(center + new Vector3(-0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, -0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));
                vertices.Add(center + new Vector3(-0.5f, -0.5f, -0.5f));
                uvs.Add(new Vector2(0f, 0f));

                vertices.Add(center + new Vector3(-0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(1f, 1f));
                vertices.Add(center + new Vector3(0.5f, -0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));

                triangles.AddRange(new int[] { triOffset, triOffset + 1, triOffset + 2, triOffset + 3, triOffset + 4, triOffset + 5 });

                break;
            case FaceDir.x_positiv:
                vertices.Add(center + new Vector3(0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));
                vertices.Add(center + new Vector3(0.5f, -0.5f, -0.5f));
                uvs.Add(new Vector2(0f, 0f));

                vertices.Add(center + new Vector3(0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, 0.5f, 0.5f));
                uvs.Add(new Vector2(1f, 1f));
                vertices.Add(center + new Vector3(0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));

                triangles.AddRange(new int[] { triOffset, triOffset + 1, triOffset + 2, triOffset + 3, triOffset + 4, triOffset + 5 });
                break;
            case FaceDir.x_negativ:
                vertices.Add(center + new Vector3(-0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(-0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));
                vertices.Add(center + new Vector3(-0.5f, -0.5f, -0.5f));
                uvs.Add(new Vector2(0f, 0f));

                vertices.Add(center + new Vector3(-0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(-0.5f, 0.5f, 0.5f));
                uvs.Add(new Vector2(1f, 1f));
                vertices.Add(center + new Vector3(-0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));

                triangles.AddRange(new int[] { triOffset + 2, triOffset + 1, triOffset, triOffset + 5, triOffset + 4, triOffset + 3 });
                break;
            case FaceDir.z_positiv:
                vertices.Add(center + new Vector3(-0.5f, 0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(1f, 0f));
                vertices.Add(center + new Vector3(-0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 0f));

                vertices.Add(center + new Vector3(-0.5f, 0.5f, 0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, 0.5f, 0.5f));
                uvs.Add(new Vector2(1f, 1f));
                vertices.Add(center + new Vector3(0.5f, -0.5f, 0.5f));
                uvs.Add(new Vector2(1f, 0f));

                triangles.AddRange(new int[] { triOffset, triOffset + 1, triOffset + 2, triOffset + 3, triOffset + 4, triOffset + 5 });
                break;
            case FaceDir.z_negativ:
                vertices.Add(center + new Vector3(-0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, -0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));
                vertices.Add(center + new Vector3(-0.5f, -0.5f, -0.5f));
                uvs.Add(new Vector2(0f, 0f));

                vertices.Add(center + new Vector3(-0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add(center + new Vector3(0.5f, 0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 1f));
                vertices.Add(center + new Vector3(0.5f, -0.5f, -0.5f));
                uvs.Add(new Vector2(1f, 0f));

                triangles.AddRange(new int[] { triOffset + 2, triOffset + 1, triOffset, triOffset + 5, triOffset + 4, triOffset + 3 });
                break;
            default:
                break;
        }
    }

    void UpdateInfoText()
    {
        if (infoText == null)
            return;

        var chunkCoords = GetChunkCoords(playerPos);

        var info = "Voxel Engine info: \n";
        info += "Current chunk: " + chunkCoords.x + " : " + chunkCoords.y + " : " + chunkCoords.z + "\n";
        info += "Sub chunk: " + chunkCoords.subX + " : " + chunkCoords.subY + " : " + chunkCoords.subZ + "\n";
        info += "Initialized chunks: " + chunks.Count + "\n";
        info += "Initializing chunks: " + initializingChunksQueue.Count + "\n";
        info += "Loaded chunks: " + loadedChunks.Count + "\n";
        info += "Loading chunks: " + loadingChunksQueue.Count + "\n";
        info += "Unloading chunks: " + unloadingChunksQueue.Count;

        infoText.text = info;
    }
}
