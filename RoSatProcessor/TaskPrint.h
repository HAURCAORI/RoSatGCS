#pragma once
#include "RoSatTask.h"
#include "zmq.hpp"

namespace RoSatProcessor {
	class TaskPrint : public RoSatTask {
	public:
		TaskPrint(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskPrint(const TaskPrint&);
		virtual ~TaskPrint() = default;

	protected:
		virtual BOOL initialize() override;
		virtual void task() override;
		virtual void starting() override;
		virtual void stopped() override;
	private:
		
	};
}