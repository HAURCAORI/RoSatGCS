#pragma once
#include <Windows.h>
#include <condition_variable>

namespace RoSatProcessor {
	class RoSatTask {
	public:
		RoSatTask(PCWSTR pszServiceName, PCWSTR pszTaskName);
		RoSatTask(PCWSTR pszServiceName, PCWSTR pszTaskName, BOOL fLoop, DWORD dwInterval);
		RoSatTask(const RoSatTask&);
		RoSatTask(RoSatTask&&) = delete;
		RoSatTask& operator=(const RoSatTask&);
		RoSatTask& operator=(RoSatTask&&) = delete;
		virtual ~RoSatTask();

		void run();
		void stop();
		PCWSTR getServiceName() const;
		PCWSTR getTaskName() const;
		DWORD getInterval() const;
		DWORD getIsRunning() const;

	protected:
		virtual BOOL initialize();
		virtual void task();
		virtual void starting();
		virtual void stopped();

		void print(const std::string& sMessage, WORD wType = EVENTLOG_INFORMATION_TYPE);
		void print(const std::wstring& sMessage, WORD wType = EVENTLOG_INFORMATION_TYPE);
		void print(PCSTR pszMessage, WORD wType = EVENTLOG_INFORMATION_TYPE);
		void print(PCWSTR pszMessage, WORD wType = EVENTLOG_INFORMATION_TYPE);
		PCWSTR	m_serviceName;
		PCWSTR	m_taskName;
	private:
		BOOL	m_loop;
		BOOL	m_stop = FALSE;
		BOOL	m_running = FALSE;
		BOOL	m_initialized = FALSE;
		DWORD	m_interval;

		std::condition_variable _cv;
		std::mutex _m;

		bool wait_for(const std::chrono::milliseconds& duration);
	};
}