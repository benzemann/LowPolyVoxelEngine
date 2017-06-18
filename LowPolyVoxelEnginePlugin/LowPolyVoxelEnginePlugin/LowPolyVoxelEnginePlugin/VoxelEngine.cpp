#include "VoxelEngine.h"
#include "Windows.h"
#include <iostream>
#include <string>
#include "ChunkManager.h"

using namespace std;

#pragma region Declerations
class ChunkManager;
unordered_map<Coord, Chunk*, CoordHashFunc, CoordEqualFunc> VoxelEngine::chunks;
int VoxelEngine::chunkWidth;
int VoxelEngine::chunkHeight;
int VoxelEngine::chunkDepth;
int VoxelEngine::subChunkWidth;
int VoxelEngine::subChunkHeight;
int VoxelEngine::subChunkDepth;
int VoxelEngine::voxelSpacing;
bool VoxelEngine::debugMode;
int VoxelEngine::xzLoadRadius;
int VoxelEngine::yLoadRadius;
int VoxelEngine::xzUnloadRadius;
int VoxelEngine::yUnloadRadius;
#pragma endregion

void VoxelEngine::Init(int cw, int ch, int cd, int scw, int sch, int scd, float vSpacing, int xzlr, int ylr, int xzulr, int yulr, bool _debugMode)
{
	// Set parameters
	chunkWidth = cw;
	chunkHeight = ch;
	chunkDepth = cd;
	subChunkWidth = scw;
	subChunkHeight = sch;
	subChunkDepth = scd;
	voxelSpacing = vSpacing;
	debugMode = _debugMode;
	xzLoadRadius = xzlr;
	yLoadRadius = ylr;
	xzUnloadRadius = xzulr;
	yUnloadRadius = yulr;
	// Create a console window 
	Debug::CreateConsoleWindow();
	Debug::Log("Initializing voxel engine...");


	Debug::Log("Initialization ended...");
}

void VoxelEngine::Update(Vec3 camPos)
{
	// Update queues
	list<Coord> loadQueue = ChunkManager::UpdateQueues(camPos);
	//ChunkManager::loadedChunks.insert(loadQueue.front());

	UpdateLoadingChunks(loadQueue);
}

Coord VoxelEngine::WordPos2Coord(Vec3 worldPos)
{
	int x = (int)(worldPos.x / (chunkWidth * subChunkWidth * voxelSpacing));
	int y = (int)(worldPos.y / (chunkHeight * subChunkHeight * voxelSpacing));
	int z = (int)(worldPos.z / (chunkDepth * subChunkDepth * voxelSpacing));

	return Coord(x, y, z);
}

string VoxelEngine::GetDebugText()
{
	return constants::NAME + " " + constants::VERSION + " \n" +
		"Initialized chunks: " + to_string(chunks.size()) + " \n" +
		"Loaded chunks: " + to_string(ChunkManager::loadedChunks.size()) + " \n" +
		"Loading chunks: " + to_string(ChunkManager::loadingChunks.size()) + " \n";
}

void VoxelEngine::UpdateLoadingChunks(list<Coord> loadQueue)
{
	if (loadQueue.size() > 0) {
		// Chech if chunk needs to be initialized first
		if (!(chunks.find(loadQueue.front()) != chunks.end())) {
			InitializeChunk(loadQueue.front());
		}
		LoadChunk(loadQueue.front());
	}
}

void VoxelEngine::InitializeChunk(Coord chunkCoord)
{
	Chunk* chunk = new Chunk(chunkCoord);
	// Create subchunk array
	chunk->subChunks = new SubChunk[chunkWidth * chunkHeight * chunkDepth]();
	// Create voxel arrays for each subchunk
	for (int i = 0; i < chunkWidth; i++)
	{
		for (int j = 0; j < chunkHeight; j++)
		{
			for (int w = 0; w < chunkDepth; w++)
			{
				chunk->subChunks[i * chunkHeight * chunkDepth + j * chunkDepth + w].voxels = 
					new Voxel[subChunkWidth * subChunkHeight * subChunkDepth];
			}
		}
	}
	// Insert into map of chunks
	chunks.insert(pair<Coord, Chunk*>(chunkCoord, chunk));
}

void VoxelEngine::LoadChunk(Coord chunkCoord)
{
	Chunk* chunk = chunks[chunkCoord];

	ChunkManager::loadedChunks.insert(chunkCoord);
}
