using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
public class CallPluginTest : MonoBehaviour {

    // The imported function
    [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "Init")]
    public static extern void init(int cw, int ch, int cd, float vSpacing);
    //   [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "LoadChunk")]
    //   unsafe public static extern void LoadChunk(int x, int y, int z, float** vertices, int* triangles);
    [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "FillArray")]
    public static extern void FillArray(ref int len, ref float data);
    // Use this for initialization
    void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown("f"))
            init(10,10,10,1f);
        //    if (Input.GetKeyDown("g"))
        //      LoadChunk(0, 0, 0);
        if (Input.GetKeyDown("g"))
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            float[] d = new float[5000];
            for (int i = 0; i < 5000; i++)
            {
                d[i] = i*i - (299f * 0.01f);
            }
            sw.Stop();
            UnityEngine.Debug.Log("c# : " + sw.Elapsed);
            sw.Reset();
            sw.Start();
            int size = 0;
            float[] data = new float[5000];
            FillArray(ref size, ref data[0]);
            //Array.Resize<float>(ref data, size);
            sw.Stop();
            UnityEngine.Debug.Log("c++ : " + sw.Elapsed);
            sw.Reset();

        }
        
    }
}
