#include "ChunkManager.h"

#pragma region Decleration
set<Coord> ChunkManager::loadedChunks;
set<Coord> ChunkManager::loadingChunks;
set<Coord> ChunkManager::unloadingChunks;
#pragma endregion

list<Coord> ChunkManager::UpdateQueues(Vec3 camPos)
{
	// Get current chunk coordinates
	Coord currentCoord = VoxelEngine::WordPos2Coord(camPos);
	list<Coord> loadQueue;
	// Update loading queue
	for (int i = -VoxelEngine::xzLoadRadius; i <= VoxelEngine::xzLoadRadius; i++)
	{
		for (int j = -VoxelEngine::yLoadRadius; j <= VoxelEngine::yLoadRadius; j++)
		{
			for (int w = -VoxelEngine::xzLoadRadius; w <= VoxelEngine::xzLoadRadius; w++)
			{
				Coord c = Coord(currentCoord.x + i, currentCoord.y + j, currentCoord.z + w);
				// Chech if coords is loaded or loading
				if(!(loadedChunks.find(c) != loadedChunks.end()) &&
					!(loadingChunks.find(c) != loadingChunks.end()))
				{
					loadQueue.push_back(c);
				}
			}
		}
	}
	// Update unloading queue
	for (set<Coord>::iterator it = loadedChunks.begin(); it != loadedChunks.end(); ++it) {
		// Check if loaded chunks should be unloaded
		if (it->x >= currentCoord.x + VoxelEngine::xzUnloadRadius ||
			it->x <= currentCoord.x - VoxelEngine::xzUnloadRadius ||
			it->y >= currentCoord.y + VoxelEngine::xzUnloadRadius ||
			it->y <= currentCoord.y - VoxelEngine::xzUnloadRadius ||
			it->z >= currentCoord.z + VoxelEngine::xzUnloadRadius ||
			it->z <= currentCoord.z - VoxelEngine::xzUnloadRadius &&
			unloadingChunks.find(*it) == unloadingChunks.end()) {
			
			unloadingChunks.insert(Coord(it->x,it->y,it->z));
		}
	}

	return loadQueue;
}
