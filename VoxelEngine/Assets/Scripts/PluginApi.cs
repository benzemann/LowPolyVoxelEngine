using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
/// <summary>
/// Static class containing all methods to communicate with the native plugin
/// </summary>
public static class PluginApi {

    /// <summary>
    /// Initializes the voxelengine.
    /// </summary>
    /// <param name="cw">The chunk width (how many subChunks on the x axis in one chunk)</param>
    /// <param name="ch">The chunk height (how many subChunks on the y axis in one chunk)</param>
    /// <param name="cd">The chunk depth (how many subChunks on the z axis in one chunk)</param>
    /// <param name="scw">The subChunk width (how many voxels on the x axis in one chunk)</param>
    /// <param name="sch">The subChunk height (how many voxels on the y axis in one chunk)</param>
    /// <param name="scd">The subChunk depth (how many voxels on the z axis in one chunk)</param>
    /// <param name="vSpacing">How much space between each voxel in worldspace</param>
    [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "Init")]
    public static extern void InitVoxelEngine(int cw, int ch, int cd, int scw, int sch, int scd, float vSpacing, int xzlr, int ylr, int xzulr, int yulr, bool debugMode);

    [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "Update")]
    public static extern void UpdateVoxelEngine(float x, float y, float z);

    [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "GetDebugString")]
    public static extern System.IntPtr GetDebugText();
}
