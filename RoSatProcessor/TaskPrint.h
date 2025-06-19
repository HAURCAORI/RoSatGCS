#pragma once

#include "RoSatTask.h"
#include "zmq.hpp"

namespace RoSatProcessor {
	class TaskPrint : public RoSatTask {
	public:
		TaskPrint(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskPrint(const TaskPrint&);
		virtual ~TaskPrint() = default;

		virtual void stop() override;
		virtual void Enqueue(const DataFrame& value) override;

	protected:
		virtual BOOL initialize() override;
		virtual void task() override;
		virtual void starting() override;
		virtual void stopped() override;
	private:
		zmq::context_t m_context;
		zmq::socket_t m_socket;
	};
}