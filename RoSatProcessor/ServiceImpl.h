#pragma once
#include <memory>
#include "ServiceBase.h"

class CServiceImpl : public CServiceBase
{
public:
    CServiceImpl(PWSTR pszServiceName,
        BOOL fCanStop = TRUE,
        BOOL fCanShutdown = TRUE,
        BOOL fCanPauseContinue = FALSE);
    ~CServiceImpl(void);

protected:
    virtual void OnStart(DWORD dwArgc, PWSTR* pszArgv);
    virtual void OnStop();

private:
    HANDLE m_hStoppedEvent;
};