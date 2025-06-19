#pragma region Includes
#include "pch.h"
#include "ServiceImpl.h"
#include "ThreadPool.h"
#include "RoSatTaskManager.h"
#pragma endregion

#define THREAD_POOL_SIZE 4

CServiceImpl::CServiceImpl(PWSTR pszServiceName,
    BOOL fCanStop,
    BOOL fCanShutdown,
    BOOL fCanPauseContinue) : CServiceBase(pszServiceName, fCanStop, fCanShutdown, fCanPauseContinue)
{
    // Create a manual-reset event that is not signaled at first to indicate 
    // the stopped signal of the service.
    m_hStoppedEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
    if (m_hStoppedEvent == NULL)
    {
        throw GetLastError();
    }
}


CServiceImpl::~CServiceImpl(void)
{
    if (m_hStoppedEvent)
    {
        CloseHandle(m_hStoppedEvent);
        m_hStoppedEvent = NULL;
    }
}

void CServiceImpl::OnStart(DWORD dwArgc, PWSTR* pszArgv)
{
    // Log a service start message to the Application log.
    WriteEventLogEntry(const_cast<PWSTR>(TEXT("WindowsService in OnStart")),
        EVENTLOG_INFORMATION_TYPE);


    RoSatProcessor::RoSatTaskManager::run(m_hStoppedEvent);
}

void CServiceImpl::OnStop()
{
    // Log a service stop message to the Application log.
    WriteEventLogEntry(const_cast<PWSTR>(TEXT("WindowsService in OnStop")),
        EVENTLOG_INFORMATION_TYPE);

    // Indicate that the service is stopping and wait for the finish of the 
    // main service function (ServiceWorkerThread).
    RoSatProcessor::RoSatTaskManager::stop();

    if (WaitForSingleObject(m_hStoppedEvent, INFINITE) != WAIT_OBJECT_0)
    {
        throw GetLastError();
    }
}