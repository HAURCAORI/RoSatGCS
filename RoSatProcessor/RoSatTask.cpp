#include "pch.h"
#include <algorithm>
#include "RoSatTask.h"

#define MAX_INTERVAL (DWORD) 60000
#define MIN_INTERVAL (DWORD) 50


RoSatProcessor::RoSatTask::RoSatTask(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	m_serviceName(pszServiceName), m_taskName(pszTaskName),
	m_loop(FALSE), m_interval(MIN_INTERVAL)
{
}

RoSatProcessor::RoSatTask::RoSatTask(PCWSTR pszServiceName, PCWSTR pszTaskName, BOOL fLoop, DWORD dwInterval) :
	m_serviceName(pszServiceName), m_taskName(pszTaskName),
	m_loop(fLoop), m_interval(std::clamp(dwInterval, MIN_INTERVAL, MAX_INTERVAL))
{
}

RoSatProcessor::RoSatTask::RoSatTask(const RoSatTask& src)
	: RoSatTask(src.m_serviceName, src.m_taskName, src.m_loop, src.m_interval)
{

}

RoSatProcessor::RoSatTask& RoSatProcessor::RoSatTask::operator=(const RoSatTask& rhs) 
{
	if (this == &rhs) {
		return *this;
	}

	this->m_serviceName = rhs.m_serviceName;
	this->m_taskName = rhs.m_taskName;
	this->m_loop = rhs.m_loop;
	this->m_interval = rhs.m_interval;

	return *this;

}

RoSatProcessor::RoSatTask::~RoSatTask()
{
	m_stop = TRUE;
	std::lock_guard<std::mutex> l(_m);
}

void RoSatProcessor::RoSatTask::run()
{
#ifdef _DEBUG
	wprintf(TEXT("===Task Started[%s]\r\n"), getTaskName());
#endif
	try {
		m_initialized = initialize();
		if (!m_initialized) {
			print("Initialize Fail\r\n");
			return;
		}

		if (m_running) {
			print("Already Running\r\n");
			return;
		}
	}
	catch(std::exception& e) {
		print(e.what());
	}

	m_stop = FALSE;
	m_running = TRUE;
	starting();
	do {
		try {
			task();
		}
		catch (std::exception& e) {
			print(e.what());
		}

		if (m_stop) {
			break;
		}

		if (m_loop) {
			this->wait_for(std::chrono::milliseconds(m_interval));
		}
	} while (m_loop && !m_stop);
	m_running = FALSE;
	stopped();
}

void RoSatProcessor::RoSatTask::stop()
{
	m_stop = true;
	_cv.notify_one();
	while (m_running) {
		Sleep(50);
	}
#ifdef _DEBUG
	wprintf(TEXT("\n===Task Stopped[%s]\n"), getTaskName());
#endif
}

PCWSTR RoSatProcessor::RoSatTask::getServiceName() const
{
	return m_serviceName;
}

PCWSTR RoSatProcessor::RoSatTask::getTaskName() const
{
	return m_taskName;
}

DWORD RoSatProcessor::RoSatTask::getInterval() const
{
	return m_interval;
}

DWORD RoSatProcessor::RoSatTask::getIsRunning() const
{
	return m_running;
}

void RoSatProcessor::RoSatTask::Enqueue(const DataFrame& value)
{
	
}


BOOL RoSatProcessor::RoSatTask::initialize()
{
	return 0;
}

void RoSatProcessor::RoSatTask::task()
{
}

void RoSatProcessor::RoSatTask::starting()
{
}

void RoSatProcessor::RoSatTask::stopped()
{
}


void RoSatProcessor::RoSatTask::print(const std::string& sMessage, WORD wType)
{
	auto temp = std::wstring(m_taskName);
	std::string task_name;
	task_name.assign(temp.begin(), temp.end());
	std::string str = "[" + task_name + "]" + sMessage;
	print(str.c_str(), wType);
}

void RoSatProcessor::RoSatTask::print(const std::wstring& sMessage, WORD wType)
{
	std::wstring str = L"[" + std::wstring(m_taskName) + L"]" + sMessage;
	print(str.c_str(), wType);
}

void RoSatProcessor::RoSatTask::print(PCSTR pszMessage, WORD wType)
{
	size_t cSize = strlen(pszMessage) + 1;
	size_t converted = 0;
	wchar_t* wc = new wchar_t[cSize];
	mbstowcs_s(&converted, wc, cSize, pszMessage, cSize-1);
	wprintf(wc);

	if (!m_serviceName) {
		delete[] wc;
		return;
	}

	HANDLE hEventSource = NULL;
	LPCWSTR lpszStrings[2] = { NULL, NULL };

	hEventSource = RegisterEventSource(NULL, m_serviceName);
	if (hEventSource)
	{
		lpszStrings[0] = m_serviceName;
		lpszStrings[1] = wc;

		ReportEvent(hEventSource,  // Event log handle
			wType,                 // Event type
			0,                     // Event category
			0,                     // Event identifier
			NULL,                  // No security identifier
			2,                     // Size of lpszStrings array
			0,                     // No binary data
			lpszStrings,           // Array of strings
			NULL                   // No binary data
		);

		DeregisterEventSource(hEventSource);
	}

	delete[] wc;
}

void RoSatProcessor::RoSatTask::print(PCWSTR pszMessage, WORD wType)
{
	wprintf(pszMessage);

	HANDLE hEventSource = NULL;
	LPCWSTR lpszStrings[2] = { NULL, NULL };

	if (m_serviceName == NULL) {
		return;
	}

	hEventSource = RegisterEventSource(NULL, m_serviceName);
	if (hEventSource)
	{
		lpszStrings[0] = m_serviceName;
		lpszStrings[1] = pszMessage;

		ReportEvent(hEventSource,  // Event log handle
			wType,                 // Event type
			0,                     // Event category
			0,                     // Event identifier
			NULL,                  // No security identifier
			2,                     // Size of lpszStrings array
			0,                     // No binary data
			lpszStrings,           // Array of strings
			NULL                   // No binary data
		);

		DeregisterEventSource(hEventSource);
	}
}

bool RoSatProcessor::RoSatTask::wait_for(const std::chrono::milliseconds& duration)
{
	std::unique_lock<std::mutex> l(_m);
	return !_cv.wait_for(l, duration, [this]() { return m_stop; });
}