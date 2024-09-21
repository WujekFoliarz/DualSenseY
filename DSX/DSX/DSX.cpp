#include <iostream>
#include <cstdio>
#include <windows.h>
#include <tlhelp32.h>
#include <string>

bool isDSY() {
    PROCESSENTRY32 entry;
    entry.dwSize = sizeof(PROCESSENTRY32);

    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);

    if (Process32First(snapshot, &entry) == TRUE)
    {
        while (Process32Next(snapshot, &entry) == TRUE)
        {
            if ((std::wstring)entry.szExeFile == L"DualSenseY.exe")
            {
                return true;
            }
        }

    }

    CloseHandle(snapshot);
    return false;
}

int main()
{
	while (true)
	{        
		Sleep(5000);

        if (isDSY() == false) {
            break;
        }
	}

    return 0;
}