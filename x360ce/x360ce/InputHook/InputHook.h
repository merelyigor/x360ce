/*  x360ce - XBOX360 Controler Emulator
*  Copyright (C) 2002-2010 Racer_S
*  Copyright (C) 2010-2011 Robert Krawczyk
*
*  x360ce is free software: you can redistribute it and/or modify it under the terms
*  of the GNU Lesser General Public License as published by the Free Software Found-
*  ation, either version 3 of the License, or (at your option) any later version.
*
*  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
*  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
*  PURPOSE.  See the GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License along with x360ce.
*  If not, see <http://www.gnu.org/licenses/>.
*/

#ifndef _InputHook_H_
#define _InputHook_H_

#include <CGuid.h>
#include <vector>
#include <detours.h>

#ifdef WIN64
#pragma comment( lib, "detours64.lib" )
#else
#pragma comment( lib, "detours32.lib" )
#endif

void HookWMI_UNI();
void HookWMI_ANSI();
void HookDI();
void HookWinTrust();

void HookWMI_UNI_Clean();
void HookWMI_ANSI_Clean();
void HookDIClean();
void HookWinTrustClean();

class iHookPadConfig
{
public:
	iHookPadConfig()
		:bEnabled(0)
		,gProductGUID(GUID_NULL)
		,gInstanceGUID(GUID_NULL)
		,dwVIDPID(0)
	{}
	virtual ~iHookPadConfig(){};

	inline BOOL Enable()
	{
		return bEnabled = TRUE;
	}

	inline BOOL Disable()
	{
		return bEnabled = FALSE;
	}

	inline GUID SetProductGUID(const GUID &guid)
	{
		dwVIDPID = guid.Data1;
		return gProductGUID = guid;
	}

	inline GUID SetInstanceGUID(GUID &guid)
	{
		return gInstanceGUID = guid;
	}

	inline DWORD GetHookState()
	{
		return bEnabled;
	}

	inline GUID GetProductGUID()
	{
		return gProductGUID;
	}

	inline GUID GetInstanceGUID()
	{
		return gInstanceGUID;
	}

	inline DWORD GetProductVIDPID()
	{
		return dwVIDPID;
	}

private:
	BOOL  bEnabled;
	GUID  gProductGUID;
	GUID  gInstanceGUID;
	DWORD dwVIDPID;
};

class iHook
{
public:
	iHook();
	virtual ~iHook()
	{
		HookWMI_UNI_Clean();
		HookWMI_ANSI_Clean();
		HookDIClean();
		HookWinTrustClean();
	};

	static const DWORD HOOK_NONE        = 0x00000000;
	static const DWORD HOOK_WMI         = 0x00000001;
	static const DWORD HOOK_DI          = 0x00000002;
	static const DWORD HOOK_VIDPID      = 0x00000004;
	static const DWORD HOOK_NAME        = 0x00000008;
	static const DWORD HOOK_STOP        = 0x00000010;
	static const DWORD HOOK_WMIA        = 0x00000020;
	static const DWORD HOOK_TRUST       = 0x00000040;
	static const DWORD HOOK_ENABLE      = 0x80000000;

	inline VOID Enable()
	{
		dwHookMode |= HOOK_ENABLE;
	}

	inline VOID Disable()
	{
		dwHookMode &= ~HOOK_ENABLE;
	}

	inline VOID EnableHook(const DWORD flag)
	{
		dwHookMode |= flag;
	}

	inline VOID DisableHook(const DWORD flag)
	{
		dwHookMode &= ~flag;
	}

	inline BOOL CheckHook(const DWORD flag)
	{
		return (dwHookMode & (flag | HOOK_ENABLE)) == (flag | HOOK_ENABLE);
	}

	inline BOOL GetState()
	{
		return (dwHookMode & HOOK_ENABLE) == HOOK_ENABLE;
	}

	inline DWORD SetMode(DWORD mode)
	{
		return dwHookMode = mode;
	}

	inline DWORD SetFakeVIDPID(DWORD vidpid)
	{
		return dwHookVIDPID = vidpid;
	}

	inline DWORD GetFakeVIDPID()
	{
		return dwHookVIDPID;
	}

	inline size_t GetHookCount()
	{
		return vPadConf.size();
	}

	inline iHookPadConfig& GetPadConfig(size_t dwUserIndex)
	{
		return vPadConf[dwUserIndex];
	}

	inline HMODULE GetDinput8()
	{
		return hDinput8;
	}

	inline HMODULE SetDinput8(HMODULE hMod)
	{
		return hDinput8 = hMod;
	}

	inline VOID AddHook(iHookPadConfig &config)
	{
		vPadConf.push_back(config);
	}

	inline VOID ExecuteHooks()
	{
		if(!GetState()) return;

		if(CheckHook(HOOK_WMI | HOOK_WMIA))
			HookWMI_ANSI();
		else if(CheckHook(HOOK_WMI))
			HookWMI_UNI();

		if(CheckHook(HOOK_DI))
			HookDI();

		if(CheckHook(HOOK_TRUST))
			HookWinTrust();

		return;
	}

private:
	DWORD dwHookMode;
	DWORD dwHookVIDPID;
	HMODULE hDinput8;
protected:
	std::vector<iHookPadConfig> vPadConf;
};

#ifdef _IN_HOOK
extern iHook *iHookThis;
#endif

#endif
