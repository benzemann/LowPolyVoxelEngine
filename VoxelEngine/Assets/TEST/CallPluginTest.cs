using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using diagnostic = System.Diagnostics;

public class CallPluginTest : MonoBehaviour {

    // The imported function
    [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "Init")]
    public static extern void init(int cw, int ch, int cd, float vSpacing);
    //   [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "LoadChunk")]
    //   unsafe public static extern void LoadChunk(int x, int y, int z, float** vertices, int* triangles);
    [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "FillArray")]
    public static extern void FillArray(out int len, out IntPtr data);

    [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "GetArray")]
    public static extern float[] GetArray(out int len);

    [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "Expensive")]
    public static extern void Expensive();

    // Use this for initialization
    void Start () {

	}
	
	// Update is called once per frame
	void Update () {
    
        if (Input.GetKeyDown("p"))
        {
            var sw = new diagnostic.Stopwatch();
            sw.Start();
            //for (int j = 0; j < 10; j++)
            //{
            var arr = PluginWrapper.PluginWrapper.GetArrayFromPluging();
            //}
            // var arr = PluginWrapper.PluginWrapper.ArrayFromIntPtr(data, len);
            sw.Stop();
            Debug.Log("Plugin execution time: " + sw.Elapsed);
            sw.Reset();
            sw.Start();
            //for (int j = 0; j < 10; j++)
            //{
            var array = new float[50000];
            var dict = new Dictionary<int, float>();
            for (int i = 0; i < 5000000; i++)
            {
                if (!dict.ContainsKey(i))
                {
                    dict.Add(i, i * i - (i * 10.0f));
                }
            }
            for (int i = 0; i < 50000; i++)
            {
                array[i] = i * i - (i * 10f);
            }
            //}
            
            sw.Stop();
            Debug.Log("C# execution time: " + sw.Elapsed);
            //Debug.Log(arr[5] + " " + array[5]);
        }

    }
}
