using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using diagnostic = System.Diagnostics;
using UnityEngine.UI;
using System.Runtime.InteropServices;


public class VoxelEngine : Singleton<VoxelEngine> {

    #region Variables
    [SerializeField]
    Text infoText;
    [Header("Voxel engine parameters")]
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
    int xzLoadRadius;
    [SerializeField]
    int yLoadRadius;
    [SerializeField]
    int xzUnloadRadius;
    [SerializeField]
    int yUnloadRadius;
    #endregion
    
    // Use this for initialization
    void Awake () {
        // Initialize voxel engine 
        PluginApi.InitVoxelEngine(chunkWidth, chunkHeight, chunkDepth, subChunkWidth, 
            subChunkHeight, subChunkDepth, voxelSpacing, xzLoadRadius, yLoadRadius, 
            xzUnloadRadius, yUnloadRadius, true);
	}
	
	// Update is called once per frame
	void Update () {
       // if (Input.GetKeyDown("j"))
        //{
            // Update the voxel engine based on position of the main camera
            PluginApi.UpdateVoxelEngine(0f, 0f, 0f);
            // Update debug text
            infoText.text = Marshal.PtrToStringAnsi(PluginApi.GetDebugText());
        //}
	}
}
