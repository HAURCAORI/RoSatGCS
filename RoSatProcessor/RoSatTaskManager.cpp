#pragma region Includes
#include "pch.h"
#include "RoSatTask.h"
#include "RoSatTaskManager.h"
#include "ThreadPool.h"

#pragma endregion


RoSatProcessor::RoSatTaskManager& RoSatProcessor::RoSatTaskManager::instance()
{
	static RoSatProcessor::RoSatTaskManager instance;
	return instance;
}

void RoSatProcessor::RoSatTaskManager::run(HANDLE hStoppedEvent)
{
	auto& e = instance();
	
	if (e.isRunning) {
		return;
	}

	e.m_hStoppedEvent = NULL;
	if (hStoppedEvent) {
		e.m_hStoppedEvent = hStoppedEvent;
	}

	e.isRunning = TRUE;
	for (auto& iter : e.m_tasks) {
#ifdef _DEBUG
		wprintf(TEXT("===Task Started[%s]\r\n"), iter->getTaskName());
#endif
		CThreadPool::QueueUserWorkItem(&RoSatProcessor::RoSatTask::run, (iter.get()));
	}
}

void RoSatProcessor::RoSatTaskManager::stop()
{
	auto& e = instance();

	if (!e.isRunning) {
		return;
	}

	for (auto& iter : e.m_tasks) {
		iter.get()->stop();
#ifdef _DEBUG
		wprintf(TEXT("\n===Task Stopped[%s]\n"),iter->getTaskName());
#endif
	}

	if (e.m_hStoppedEvent) {
		SetEvent(e.m_hStoppedEvent);
	}

	e.isRunning = FALSE;
	
}

RoSatProcessor::RoSatTask* RoSatProcessor::RoSatTaskManager::FindTask(PCWSTR pszTaskName)
{
	auto& e = instance();
	auto it = std::find_if(e.m_tasks.begin(), e.m_tasks.end(),
		[&pszTaskName](std::shared_ptr<RoSatTask> s)
		{ return lstrcmpW(s.get()->getTaskName(), pszTaskName) == 0; });

	if (it != e.m_tasks.end()) {
		return it->get();
	}
	return nullptr;
}


