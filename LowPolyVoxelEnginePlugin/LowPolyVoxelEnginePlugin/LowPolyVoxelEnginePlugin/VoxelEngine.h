#pragma once
#include <unordered_map>
#include <list>
#include <string>
#include "Debug.h"

using namespace std;

namespace constants 
{
	const string NAME = "LOW POLY VOXEL ENGINE";
	const string VERSION = "0.1v";
	const string AUTHOR = "VertexLab";
}

class VoxelEngine
{
public:
#pragma region Methods
	//Initializes the Voxel engine
	static void Init(int cw, int ch, int cd, int scw, int sch, int scd, float vSpacing);
#pragma endregion

#pragma region Enums
	enum FaceDir { yPositive, yNegative, xPositive, xNegative, zPositive, zNegative };
#pragma endregion

#pragma region Structs
	struct Vec3
	{
		Vec3(float _x, float _y, float _z) {
			x = _x;
			y = _y;
			z = _z;
		}
		float x;
		float y;
		float z;

		Vec3 operator +(const Vec3 other) const
		{
			return Vec3(x + other.x, y + other.y, z + other.z);
		}
		Vec3 operator -(const Vec3 other) const
		{
			return Vec3(x - other.x, y - other.y, z - other.z);
		}
	};
	struct Voxel
	{
		Voxel(int _x, int _y, int _z) {
			x = _x;
			y = _y;
			z = _z;
			isoValue = 0;
		}
		Voxel() {}
		int x;
		int y;
		int z;
		float isoValue;
	};
	struct Coord
	{
		Coord(int _x, int _y, int _z) {
			x = _x;
			y = _y;
			z = _z;
		}

		Coord() {

		}

		int x;
		int y;
		int z;

		bool operator <(const Coord& other) const
		{
			return (x < other.x || y < other.y || z < other.z);
		}
		bool operator !=(const Coord& other) const
		{
			return (x != other.x || y != other.y || z != other.z);
		}
		bool operator ==(const Coord& other) const
		{
			return (x == other.x && y == other.y && z == other.z);
		}
		
	};
	struct CoordEqualFunc {
		bool operator()(const Coord& first, const Coord& second) const
		{
			return first.x == second.x && first.y == second.y && first.z == second.z;
		}
	};
	struct CoordHashFunc {
		size_t operator()(const Coord& c) const
		{
			return hash<int>()(c.x) ^
				(hash<int>()(c.y) << 1) ^
				hash<int>()(c.z);
		}
	};
	struct SubChunk
	{
		SubChunk(Coord _Coord) {
			Coords = _Coord;
		}
		SubChunk(int x, int y, int z) {
			Coords = Coord(x, y, z);
		}
		SubChunk() {}
		Coord Coords;
		Voxel*** Voxels;
	};
	struct Chunk
	{
		Chunk(Coord _Coord) {
			Coords = _Coord;
		}
		Chunk() {

		}
		Coord Coords;
		SubChunk*** SubChunks;
	};
#pragma endregion

private:
	static unordered_map<Coord, Chunk*, CoordHashFunc, CoordEqualFunc> chunks;
};