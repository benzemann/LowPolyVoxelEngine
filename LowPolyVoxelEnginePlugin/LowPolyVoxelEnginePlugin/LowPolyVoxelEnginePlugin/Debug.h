#pragma once
#include "Windows.h"
#include "VoxelEngine.h"
#include <iostream>
#include <string>

using namespace std;

class Debug
{
public:
	// Creates a console window where the user can write debug text to using Debug::Log("message");
	static void CreateConsoleWindow();
	// Writes a message to the open console window
	static void Log(string message);
private:
	Debug() {};
	~Debug() {};
};

