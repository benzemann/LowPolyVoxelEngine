#pragma once
#pragma once
#define VOXELENGINE_API __declspec(dllexport) 

extern "C" {

	VOXELENGINE_API void Init(int cw, int ch, int cd, int scw, int sch, int scd,float vSpacing);

}