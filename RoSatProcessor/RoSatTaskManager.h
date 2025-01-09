#pragma once
#include <memory>
#include <vector>
#include <any>
#include <Windows.h>

#include "TaskPrint.h"

namespace RoSatProcessor {
	class RoSatTask;

	class RoSatTaskManager {
	public:
		RoSatTaskManager(const RoSatTaskManager&) = delete;
		RoSatTaskManager(RoSatTaskManager&&) = delete;
		RoSatTaskManager& operator=(const RoSatTaskManager&) = delete;
		RoSatTaskManager& operator=(RoSatTaskManager&&) = delete;

		static RoSatTaskManager& instance();


		static void run(HANDLE hStoppedEvent);
		static void stop();
	
		template<typename T>
		static std::enable_if_t<std::is_base_of_v<RoSatTask, T>>
			addTask(const T& task) {
			auto& e = instance();
			e.m_tasks.push_back(std::make_shared<T>(task));
		}

		static void message(PCWSTR pszTaskName, const DataFrame& msg) {
			auto& e = instance();
			auto ptr = e.FindTask(pszTaskName);
			if (ptr != nullptr) {
				ptr->Enqueue(msg);
			}
		}

	private:
		RoSatTaskManager() = default;
		~RoSatTaskManager() = default;

		RoSatTask* FindTask(PCWSTR pszTaskName);

		BOOL isRunning = FALSE;
		HANDLE m_hStoppedEvent = NULL;
		std::vector<std::shared_ptr<RoSatTask>> m_tasks;
	};
}