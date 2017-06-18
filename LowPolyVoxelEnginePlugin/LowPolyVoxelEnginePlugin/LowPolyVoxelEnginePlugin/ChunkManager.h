#pragma once
#include "VoxelEngine.h"
#include <list>
#include <set>
using namespace voxelengine;

class ChunkManager
{
public:
	// Updates the unloading queue and returns a loading list based on camera position
	static list<Coord> UpdateQueues(Vec3 camPos);
	static set<Coord> loadedChunks;
	static set<Coord> loadingChunks;
	static set<Coord> unloadingChunks;
private:

};

