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
		virtual BOOL initialize();
		virtual void task();
		virtual void starting();
		virtual void stopped();
	private:
		zmq::context_t m_context;
		zmq::socket_t m_socket;
	};
}