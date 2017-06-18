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

namespace voxelengine
{

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
			return tie(x, y, z) < tie(other.x, other.y, other.z);
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
		SubChunk(Coord _coord) {
			coords = _coord;
		}
		SubChunk(int x, int y, int z) {
			coords = Coord(x, y, z);
		}
		SubChunk() {}
		Coord coords;
		Voxel* voxels;
	};
	struct Chunk
	{
		Chunk(Coord _coord) {
			coords = _coord;
		}
		Chunk() {

		}
		Coord coords;
		SubChunk* subChunks;
	};
#pragma endregion

}

using namespace voxelengine;

class VoxelEngine
{
public:

#pragma region Enums
	enum FaceDir { yPositive, yNegative, xPositive, xNegative, zPositive, zNegative };
#pragma endregion

#pragma region Methods
	// Initializes the Voxel engine
	static void Init(int cw, int ch, int cd, int scw, int sch, int scd, float vSpacing, int xzlr, int ylr, int xzulr, int yulr, bool debugMode);
	// Update the voxel engine from the input camera position
	static void Update(Vec3 camPos);
	// Returns the chunk coords the world position is in
	static Coord WordPos2Coord(Vec3 worldPos);
	// Returns a string of debug text like number of loading chunks etc.
	static string GetDebugText();
#pragma endregion

	static bool debugMode;
	static int chunkWidth;
	static int chunkHeight;
	static int chunkDepth;
	static int subChunkWidth;
	static int subChunkHeight;
	static int subChunkDepth;
	static int voxelSpacing;
	static int xzLoadRadius;
	static int yLoadRadius;
	static int xzUnloadRadius;
	static int yUnloadRadius;
private:
	// Initalize and start calculating data for loading chunks
	static void UpdateLoadingChunks(list<Coord> loadQueue);
	// Initialize a chunk at the given chunk coords
	static void InitializeChunk(Coord chunkCoord);
	// Load a chunk
	static void LoadChunk(Coord chunkCoord);
	static unordered_map<Coord, Chunk*, CoordHashFunc, CoordEqualFunc> chunks;
};