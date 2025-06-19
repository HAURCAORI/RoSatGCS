#pragma once

#include "RoSatTask.h"
#include "zmq.hpp"

namespace RoSatProcessor {
	class TaskPacketReceiveQueue : public RoSatTask {
	public:
		TaskPacketReceiveQueue(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskPacketReceiveQueue(const TaskPacketReceiveQueue&);
		virtual ~TaskPacketReceiveQueue() = default;

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