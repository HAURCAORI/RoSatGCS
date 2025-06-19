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
	{
		std::unique_lock<std::mutex> l(e._m);
		e.m_hStoppedEvent = NULL;
		if (hStoppedEvent) {
			e.m_hStoppedEvent = hStoppedEvent;
		}

		e.isRunning = TRUE;
		for (auto& iter : e.m_tasks) {
			CThreadPool::QueueUserWorkItem(&RoSatProcessor::RoSatTask::run, (iter.get()));
		}
	}
}

void RoSatProcessor::RoSatTaskManager::stop()
{
	auto& e = instance();

	if (!e.isRunning) {
		return;
	}

	{
		std::unique_lock<std::mutex> l(e._m);

		for (auto& iter : e.m_tasks) {
			iter.get()->stop();
		}

		if (e.m_hStoppedEvent) {
			SetEvent(e.m_hStoppedEvent);
		}

		e.isRunning = FALSE;
	}
	RoSatProcessor::Config::Export();
}

void RoSatProcessor::RoSatTaskManager::restart(PCWSTR pszTaskName)
{
	auto& e = instance();
	auto ptr = e.FindTask(pszTaskName);
	if (ptr != nullptr) {
		ptr->stop();
	}
	CThreadPool::QueueUserWorkItem(&RoSatProcessor::RoSatTask::run, ptr);
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


