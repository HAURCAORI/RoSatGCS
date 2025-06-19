#pragma once
#include <queue>
#include "RoSatTask.h"
#include "DataFrame.h"

namespace RoSatProcessor {
	class TaskPacketProcess : public RoSatTask {
	public:
		TaskPacketProcess(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskPacketProcess(const TaskPacketProcess&);
		virtual ~TaskPacketProcess() = default;

		virtual void stop() override;
		virtual void Enqueue(const DataFrame& value) override;

	protected:
		virtual BOOL initialize() override;
		virtual void task() override;
		virtual void starting() override;
		virtual void stopped() override;

	private:
		BOOL m_interrupt;
		std::mutex _m;
		std::condition_variable _cv;
		std::queue<DataFrame> m_jobs;
	};
}