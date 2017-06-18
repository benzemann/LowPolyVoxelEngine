#include "stdafx.h"
#include "CppUnitTest.h"
#include "..\LowPolyVoxelEnginePlugin\VoxelEngine.h"
#include <VoxelEngine.h>
using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace VoxelEngineTest
{		
	TEST_CLASS(UnitTest1)
	{
	public:
		
		TEST_METHOD(Init)
		{
			// TODO: Your test code here
			VoxelEngine v;
			v.test();
			//v.Init(0, 0, 0, 0, 0, 0, 0);
		}

	};
}