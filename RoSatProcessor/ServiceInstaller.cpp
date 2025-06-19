#pragma region Includes
#include "pch.h"
#include "ServiceInstaller.h"
#include <stdio.h>
#include <strsafe.h>
#pragma endregion

#pragma region Static Members
void InstallService(PCWSTR pszServiceName,
    PCWSTR pszDisplayName,
    DWORD dwStartType,
    PCWSTR pszDependencies,
    PCWSTR pszAccount,
    PCWSTR pszPassword)
{
    SC_HANDLE schSCManager = NULL;
    SC_HANDLE schService = NULL;
    TCHAR szUnquotedPath[MAX_PATH];

    // Retrieves the fully qualified path for the file that contains the specified module
    // If hModule parameteris Null, this function retrieves the path of the current process.
    if (GetModuleFileName(
        NULL,                               // hModule
        szUnquotedPath,                     // lpFilename
        ARRAYSIZE(szUnquotedPath)) == 0)    // nSize
    {
        wprintf(L"GetModuleFileName failed w/err 0x%08lx\n", GetLastError());
        goto Cleanup;
    }
    
    // In case the path contains a space, it must be quoted so that
    // it is correctly interpreted. For example,
    // "d:\my share\myservice.exe" should be specified as
    // ""d:\my share\myservice.exe"".
    TCHAR szPath[MAX_PATH];
    StringCbPrintf(szPath, MAX_PATH, TEXT("\"%s\""), szUnquotedPath);

    // Open the local default service control manager database
    // * dwDesiredAccess
    // following values:
    //  SC_MANAGER_ALL_ACCESS           : All access rights and STANDARD_RIGHTS_REQUIRED
    //  SC_MANAGER_CREATE_SERVICE       : Call CreateService function and create service
    //  SC_MANAGER_CONNECT              : Connect the service manager
    //  SC_MANAGER_ENUMERATE_SERVICE    : Call EnumServiceStatus or EnumServicesStatusEX function
    //  SC_MANAGER_LOCK                 : Call LockServiceDatabase and acquire lock
    //  SC_MANAGER_MODIFY_BOOT_CONFIG   : Call NotifyBootConfigStatus function
    //  SC_MANAGER_QUERY_LOCK_STATUS    : Call QueryServiceLockStatus function and check the lock status
    //  ...
    //  See https://learn.microsoft.com/en-us/windows/win32/services/service-security-and-access-rights
    //
    schSCManager = OpenSCManager(
        NULL,                                               // lpMachineName
        NULL,                                               // lpDatabaseName
        SC_MANAGER_CONNECT | SC_MANAGER_CREATE_SERVICE);    // dwDesiredAccess
    if (schSCManager == NULL)
    {
        wprintf(L"OpenSCManager failed w/err 0x%08lx\n", GetLastError());
        goto Cleanup;
    }

    // Install the service into SCM by calling CreateService
    // * dwDesiredAccess - See Above
    // 
    // * dwServiceType
    // following values:
    //  SERVICE_FILE_SYSTEM_DRIVER      : The service is a file system driver
    //  SERVICE_KERNEL_DRIVER           : The service is a device driver
    //  SERVICE_WIN32_OWN_PROCESS       : The service runs in its own process
    //  SERVICE_WIN32_SHARE_PROCESS     : The service shares a process with other services
    //  SERVICE_USER_OWN_PROCESS        : The service runs in its own process under the logged-on user account
    //  SERVICE_USER_SHARE_PROCESS      : The service shares a process with one or more other services that run under the logged-on user account
    // 
    // * dwStartType
    // following values:
    //  SERVICE_AUTO_START              : A service started automatically by the service control manager during system startup
    //  SERVICE_BOOT_START              : A device driver started by the system loader. This value is valid only for driver services
    //  SERVICE_DEMAND_START            : A service started by the service control manager when a process calls the StartService function
    //  SERVICE_DISABLED                : A service that cannot be started
    //  SERVICE_SYSTEM_START            : A device driver started by the IoInitSystem function
    // 
    // * dwErrorControl
    // following values:
    //  SERVICE_ERROR_CRITICAL          : If the last-known-good configuration is being started, the startup operation fails
    //  SERVICE_ERROR_IGNORE            : The startup program ignores the error and continues the startup operation
    //  SERVICE_ERROR_NORMAL            : The startup program logs the error in the event log but continues the startup operation
    //  SERVICE_ERROR_SEVERE            : If the last-known-good configuration is being started, the startup operation continues
    //
    schService = CreateService(
        schSCManager,                   // hSCManage            : SCManager database
        pszServiceName,                 // lpServiceName        : Name of service
        pszDisplayName,                 // lpDisplayName        : Name to display
        SERVICE_QUERY_STATUS,           // dwDesiredAccess      : Desired access
        SERVICE_WIN32_OWN_PROCESS,      // dwServiceType        : Service type
        dwStartType,                    // dwStartType          : Service start type
        SERVICE_ERROR_NORMAL,           // dwErrorControl       : Error control type
        szPath,                         // lpBinaryPathName     : Service's binary          [optional]
        NULL,                           // lpLoadOrderGroup     : No load ordering group    [optional]
        NULL,                           // lpdwTagId            : No tag identifier         [optional]
        pszDependencies,                // lpDependencies       : Dependencies              [optional]
        pszAccount,                     // lpServiceStartName   : Service running account   [optional]
        pszPassword                     // lpPassword           : Password of the account   [optional]
    );
    if (schService == NULL)
    {
        wprintf(L"CreateService failed w/err 0x%08lx\n", GetLastError());
        goto Cleanup;
    }

    wprintf(L"%s is installed.\n", pszServiceName);

Cleanup:
    // Centralized cleanup for all allocated resources.
    if (schSCManager)
    {
        CloseServiceHandle(schSCManager);
        schSCManager = NULL;
    }
    if (schService)
    {
        CloseServiceHandle(schService);
        schService = NULL;
    }
}

void UninstallService(PCWSTR pszServiceName)
{
    SC_HANDLE schSCManager = NULL;
    SC_HANDLE schService = NULL;
    SERVICE_STATUS ssSvcStatus = {};

    // Open the local default service control manager database
    // * dwDesiredAccess
    // following values:
    //  SC_MANAGER_ALL_ACCESS           : All access rights and STANDARD_RIGHTS_REQUIRED
    //  SC_MANAGER_CREATE_SERVICE       : Call CreateService function and create service
    //  SC_MANAGER_CONNECT              : Connect the service manager
    //  SC_MANAGER_ENUMERATE_SERVICE    : Call EnumServiceStatus or EnumServicesStatusEX function
    //  SC_MANAGER_LOCK                 : Call LockServiceDatabase and acquire lock
    //  SC_MANAGER_MODIFY_BOOT_CONFIG   : Call NotifyBootConfigStatus function
    //  SC_MANAGER_QUERY_LOCK_STATUS    : Call QueryServiceLockStatus function and check the lock status
    //  ...
    //  See https://learn.microsoft.com/en-us/windows/win32/services/service-security-and-access-rights
    //
    schSCManager = OpenSCManager(
        NULL,                   // lpMachineName
        NULL,                   // lpDatabaseName
        SC_MANAGER_CONNECT);    // dwDesiredAccess
    if (schSCManager == NULL)
    {
        wprintf(L"OpenSCManager failed w/err 0x%08lx\n", GetLastError());
        goto Cleanup;
    }

    // Open the service with delete, stop, and query status permissions
    // * dwDesiredAccess
    // following values:
    //  SC_ALL_ACCESS                   : All access rights and STANDARD_RIGHTS_REQUIRED
    //  SERVICE_CHANGE_CONFIG           : Call the ChangeServiceConfig or ChangeServiceConfig2 function
    //  SERVICE_ENUMERATE_DEPENDENTS    : Call the EnumDependentServices function
    //  SERVICE_INTERROGATE             : Call the ControlService function to ask the service to report its status immediately
    //  SERVICE_PAUSE_CONTINUE          : Call the ControlService function to pause or continue the service
    //  SERVICE_QUERY_CONFIG            : Call the QueryServiceConfig and QueryServiceConfig2 functions to query the service configuration
    //  SERVICE_QUERY_STATUS            : Call the QueryServiceStatus or QueryServiceStatusEx function to ask the status of the service.
    //  SERVICE_STOP                    : Call the ControlService function to stop the service
    //  DELETE                          : Call the DeleteService function to delete the service
    //  ...
    //  See https://learn.microsoft.com/en-us/windows/win32/services/service-security-and-access-rights
    //
    schService = OpenService(
        schSCManager,                                   // hSCManager
        pszServiceName,                                 // lpServiceName
        SERVICE_STOP | SERVICE_QUERY_STATUS | DELETE);  // dwDesiredAccess
    if (schService == NULL)
    {
        wprintf(L"OpenService failed w/err 0x%08lx\n", GetLastError());
        goto Cleanup;
    }

    // Try to stop the service
    if (ControlService(schService, SERVICE_CONTROL_STOP, &ssSvcStatus))
    {
        wprintf(L"Stopping %s.", pszServiceName);
        Sleep(1000);

        while (QueryServiceStatus(schService, &ssSvcStatus))
        {
            if (ssSvcStatus.dwCurrentState == SERVICE_STOP_PENDING)
            {
                wprintf(L".");
                Sleep(1000);
            }
            else break;
        }

        if (ssSvcStatus.dwCurrentState == SERVICE_STOPPED)
        {
            wprintf(L"\n%s is stopped.\n", pszServiceName);
        }
        else
        {
            wprintf(L"\n%s failed to stop.\n", pszServiceName);
        }
    }

    // Now remove the service by calling DeleteService.
    if (!DeleteService(schService))
    {
        wprintf(L"DeleteService failed w/err 0x%08lx\n", GetLastError());
        goto Cleanup;
    }

    wprintf(L"%s is removed.\n", pszServiceName);

Cleanup:
    // Centralized cleanup for all allocated resources.
    if (schSCManager)
    {
        CloseServiceHandle(schSCManager);
        schSCManager = NULL;
    }
    if (schService)
    {
        CloseServiceHandle(schService);
        schService = NULL;
    }
}

#pragma endregion