#include "Debug.h"

#define DEBUG

void Debug::CreateConsoleWindow()
{
#ifdef DEBUG
	FILE * pConsole;
	AllocConsole();
	freopen_s(&pConsole, "CONOUT$", "wb", stdout);

	cout.clear();
	cout << "--------------- ";
	cout << constants::NAME << " " << constants::VERSION;
	cout << " -----------------" << endl;
#endif // DEBUG
}
void Debug::Log(string message)
{
#ifdef DEBUG
	cout << message << endl;
#endif // DEBUG
}
