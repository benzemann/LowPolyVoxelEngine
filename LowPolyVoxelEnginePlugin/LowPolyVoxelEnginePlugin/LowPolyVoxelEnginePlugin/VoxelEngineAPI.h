#pragma once
#pragma once
#include <string>
#define VOXELENGINE_API __declspec(dllexport) 

extern "C" {

	VOXELENGINE_API void Init(int cw, int ch, int cd, int scw, int sch, int scd, float vSpacing, int xzlr, int ylr, int xzulr, int yulr, bool debugMode);

	VOXELENGINE_API void Update(float x, float y, float z);

	VOXELENGINE_API char* GetDebugString();

}