#include "VoxelEngineAPI.h"
#include "VoxelEngine.h"

extern "C" {

	void Init(int cw, int ch, int cd, int scw, int sch, int scd, float vSpacing) 
	{
		VoxelEngine::Init(cw, ch, cd, scw, sch, scd, vSpacing);
	}

}