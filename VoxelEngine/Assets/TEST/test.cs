using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class test : MonoBehaviour {
    public float frequency = 1f;

    [Range(1, 8)]
    public int octaves = 1;

    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Range(0f, 1f)]
    public float persistence = 0.5f;

    [Range(1, 3)]
    public int dimensions = 3;
    public enum FaceDir { y_positiv, y_negativ, x_positiv, x_negativ, z_positiv, z_negativ };
    Vector3 gradient;
    struct Node
    {
        public Node(Vector3 p, float v)
        {
            position = p;
            isoValue = v;
        }
        public Vector3 position;
        public float isoValue;
    }

    Node[,,] Grid;
    int gridWidth = 50;
    int gridHeigh = 50;
    int gridDepth = 50;

	// Use this for initialization
	void Start () {
        Grid = new Node[gridWidth, gridHeigh, gridDepth];

        for(int i = 0; i < gridWidth; i++)
        {
            for(int j = 0; j < gridHeigh; j++)
            {
                for(int w = 0; w < gridDepth; w++)
                {

                    float sample = Noise.Sum(Noise.methods[(int)NoiseMethodType.Perlin][2], new Vector3(i, j, w), frequency, octaves, lacunarity, persistence);
                    var value = Noise.Perlin2D(new Vector3(i, j, w), 0.08f);
                    
                    Grid[i, j, w] = new Node(new Vector3(i, j, w), 5f - Vector3.Distance(new Vector3(i, j, w), new Vector3(15f,15f,15f)));
                }
            }
        }

        GenerateMesh();
	}
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKeyDown("m"))
        {
            Thread t = new Thread(GenerateMesh);
            t.Start();
        }
        Mesh m = GetComponent<MeshFilter>().mesh;
        var verts = m.vertices;
        for(int i = 0; i < verts.Length;i++)
        {
            if(i > 10 && i < 200)
            {
                verts[i] -= new Vector3(0f,0.01f,0f);
            }
        }
        m.vertices = verts;
        m.RecalculateBounds();
        m.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = m;
    }

    void GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 5; i < gridWidth - 5; i++)
        {
            for (int j =5; j < gridHeigh - 5; j++)
            {
                for (int w = 5; w < gridDepth - 5; w++)
                {

                    if (Grid[i, j, w].isoValue >= 0f)
                    {

                        if (i + 1 < gridWidth && Grid[i + 1, j, w].isoValue < 0f)
                        {
                            CreateFace(FaceDir.x_positiv, Grid[i, j, w].position, vertices, uvs, triangles);
                        }
                        if (i - 1 >= 0 && Grid[i - 1, j, w].isoValue < 0f)
                        {
                            CreateFace(FaceDir.x_negativ, Grid[i, j, w].position, vertices, uvs, triangles);
                        }
                        if (j + 1 < gridHeigh && Grid[i, j + 1, w].isoValue < 0f)
                        {
                            CreateFace(FaceDir.y_positiv, Grid[i, j, w].position, vertices, uvs, triangles);
                        }
                        if (j - 1 >= 0 && Grid[i, j - 1, w].isoValue < 0f)
                        {
                            CreateFace(FaceDir.y_negativ, Grid[i, j, w].position, vertices, uvs, triangles);
                        }
                        if (w + 1 < gridDepth && Grid[i, j, w + 1].isoValue < 0f)
                        {
                            CreateFace(FaceDir.z_positiv, Grid[i, j, w].position, vertices, uvs, triangles);
                        }
                        if (w - 1 >= 0 && Grid[i, j, w - 1].isoValue < 0f)
                        {
                            CreateFace(FaceDir.z_negativ, Grid[i, j, w].position, vertices, uvs, triangles);
                        }
                    }

                }
            }
        }

        PushToSurface(vertices);

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
    
    void PushToSurface(List<Vector3> vertices)
    {
         for(int it = 0; it < 10; it++)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                gradient = GetGradient(vertices[i]) * 5f;
                if (gradient.magnitude > 0f)
                    vertices[i] += -gradient * (GetIsoValue(vertices[i])) / (gradient.magnitude * gradient.magnitude);

            }
        }
    }

    public Vector3 GetGradient(Vector3 pos)
    {
        return new Vector3(
                    (GetIsoValue((pos + new Vector3(0.5f, 0.0f, 0.0f))) - GetIsoValue((pos - new Vector3(0.5f, 0.0f, 0.0f)))) / 2f,
                    (GetIsoValue((pos + new Vector3(0.0f, 0.5f, 0.0f))) - GetIsoValue((pos - new Vector3(0.0f, 0.5f, 0.0f)))) / 2f,
                    (GetIsoValue((pos + new Vector3(0.0f, 0.0f, 0.5f))) - GetIsoValue((pos - new Vector3(0.0f, 0.0f, 0.5f)))) / 2f
                    );
    }

    public float GetIsoValue(Vector3 pos)
    {
        float x = pos.x;
        float y = pos.y;
        float z = pos.z;

        int xRoot = Mathf.FloorToInt(x);
        int yRoot = Mathf.FloorToInt(y);
        int zRoot = Mathf.FloorToInt(z);

        if (xRoot < 0 && xRoot + 1 >= gridWidth
            && yRoot < 0 && yRoot + 1 >= gridHeigh
            && zRoot < 0 && zRoot + 1 >= gridDepth)
        {
            return 0f;
        }

        var w = 1f;
        var tx = (x - xRoot) / w;
        var val_000_100 = ((1 - tx) * Grid[xRoot, yRoot, zRoot].isoValue) + (tx * Grid[xRoot + 1, yRoot, zRoot].isoValue);

        var val_010_110 = ((1 - tx) * Grid[xRoot, yRoot + 1, zRoot].isoValue) + (tx * Grid[xRoot + 1, yRoot + 1, zRoot].isoValue);

        var ty = (y - yRoot) / w;
        var val_mid_1 = ((1 - ty) * val_000_100) + (ty * val_010_110);

        var val_001_101 = ((1 - tx) * Grid[xRoot, yRoot, zRoot + 1].isoValue) + (tx * Grid[xRoot + 1, yRoot, zRoot + 1].isoValue);

        var val_011_111 = ((1 - tx) * Grid[xRoot, yRoot + 1, zRoot + 1].isoValue) + (tx * Grid[xRoot + 1, yRoot + 1, zRoot + 1].isoValue);

        var val_mid_2 = ((1 - ty) * val_001_101) + (ty * val_011_111);

        var tz = (z - zRoot) / w;

        return ((1 - tz) * val_mid_1) + (tz * val_mid_2);
    }

    void CreateFace(FaceDir dir, Vector3 center, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
    {
        int triOffset = triangles.Count;
        switch (dir)
        {
            case FaceDir.y_positiv:
                vertices.Add( center + new Vector3(-0.5f, 0.5f, 0.5f) );
                uvs.Add (new Vector2(0f, 1f));
                vertices.Add (center + new Vector3(0.5f, 0.5f, -0.5f) );
                uvs.Add (new Vector2(1f, 0f));
                vertices.Add( center + new Vector3(-0.5f, 0.5f, -0.5f) );
                uvs.Add(new Vector2(0f, 0f));

                vertices.Add( center + new Vector3(-0.5f, 0.5f, 0.5f) );
                uvs.Add(new Vector2(0f, 1f));
                vertices.Add( center + new Vector3(0.5f, 0.5f, 0.5f) );
                uvs.Add(new Vector2(1f, 1f));
                vertices.Add( center + new Vector3(0.5f, 0.5f, -0.5f) );
                uvs.Add(new Vector2(1f, 0f));
                
                triangles.AddRange( new int[] { triOffset + 0, triOffset + 1, triOffset + 2, triOffset + 3, triOffset + 4, triOffset + 5 } );

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

                triangles.AddRange(new int[] { triOffset + 2, triOffset + 1, triOffset + 0, triOffset + 5, triOffset + 4, triOffset + 3 });

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

                triangles.AddRange(new int[] { triOffset + 2, triOffset + 1, triOffset + 0, triOffset + 5, triOffset + 4, triOffset + 3 });
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

                triangles.AddRange(new int[] { triOffset + 0, triOffset + 1, triOffset + 2, triOffset + 3, triOffset + 4, triOffset + 5 });
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

                triangles.AddRange(new int[] { triOffset + 2, triOffset + 1, triOffset + 0, triOffset + 5, triOffset + 4, triOffset + 3 });
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

                triangles.AddRange(new int[] { triOffset + 0, triOffset + 1, triOffset + 2, triOffset + 3, triOffset + 4, triOffset + 5 });
                break;
            default:
                break;
        }
    }
}
