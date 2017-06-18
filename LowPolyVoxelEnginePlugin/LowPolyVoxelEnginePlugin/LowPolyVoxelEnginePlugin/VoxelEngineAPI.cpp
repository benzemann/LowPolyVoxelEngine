#include "VoxelEngineAPI.h"
#include "VoxelEngine.h"

extern "C" {

	void Init(int cw, int ch, int cd, int scw, int sch, int scd, float vSpacing, int xzlr, int ylr, int xzulr, int yulr, bool debugMode)
	{
		VoxelEngine::Init(cw, ch, cd, scw, sch, scd, vSpacing, xzlr, ylr, xzulr, yulr, debugMode);
	}

	void Update(float x, float y, float z)
	{
		VoxelEngine::Update(voxelengine::Vec3(x, y, z));
	}

	char* GetDebugString()
	{
		return (char*)VoxelEngine::GetDebugText().c_str();
	}

}