#include "Debug.h"

#pragma region Declerations
#pragma endregion

void Debug::CreateConsoleWindow()
{
	if (VoxelEngine::debugMode) {
		FILE * pConsole;
		AllocConsole();
		freopen_s(&pConsole, "CONOUT$", "wb", stdout);

		cout.clear();
		cout << "--------------- ";
		cout << constants::NAME << " " << constants::VERSION;
		cout << " -----------------" << endl;
		cout << "PARAMETERS:" << endl;
		cout << "    Chunk width: " << VoxelEngine::chunkWidth << endl;
		cout << "    Chunk height: " << VoxelEngine::chunkHeight << endl;
		cout << "    Chunk depth: " << VoxelEngine::chunkDepth << endl;
		cout << "    SubChunk width: " << VoxelEngine::subChunkWidth << endl;
		cout << "    SubChunk height: " << VoxelEngine::subChunkHeight << endl;
		cout << "    SubChunk depth: " << VoxelEngine::subChunkDepth << endl;
		cout << "    Voxel spacing: " << VoxelEngine::voxelSpacing << endl;
		cout << "    XZ load radius: " << VoxelEngine::xzLoadRadius << endl;
		cout << "    Y load radius: " << VoxelEngine::yLoadRadius << endl;
		cout << "    XZ unload radius: " << VoxelEngine::xzUnloadRadius << endl;
		cout << "    Y unload radius: " << VoxelEngine::yUnloadRadius << endl;
		cout << endl;
	}
}
void Debug::Log(string message)
{
	if (VoxelEngine::debugMode) {
		cout << message << endl;
	}
}
