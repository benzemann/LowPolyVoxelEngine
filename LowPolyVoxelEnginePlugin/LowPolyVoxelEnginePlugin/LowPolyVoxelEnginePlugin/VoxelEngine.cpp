#include "VoxelEngine.h"
#include "Windows.h"
#include <iostream>
#include <string>

using namespace std;

unordered_map<VoxelEngine::Coord, VoxelEngine::Chunk*, VoxelEngine::CoordHashFunc, VoxelEngine::CoordEqualFunc> VoxelEngine::chunks;

void VoxelEngine::Init(int cw, int ch, int cd, int scw, int sch, int scd, float vSpacing) 
{
	// Create a console window 
	Debug::CreateConsoleWindow();
	Debug::Log("Initializing voxel engine...");
	
	chunks.insert(pair<Coord, Chunk*>(Coord(0, 0, 0), new Chunk()));

	Debug::Log("Initialization ended...");
}
