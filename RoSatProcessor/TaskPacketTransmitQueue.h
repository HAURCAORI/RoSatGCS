#pragma once

#include <queue>
#include "RoSatTask.h"
#include "DataFrame.h"
#include "zmq.hpp"

namespace RoSatProcessor {
	class TaskPacketTransmitQueue : public RoSatTask {
	public:
		TaskPacketTransmitQueue(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskPacketTransmitQueue(const TaskPacketTransmitQueue&);
		virtual ~TaskPacketTransmitQueue() = default;

		virtual void stop() override;
		virtual void Enqueue(const DataFrame& value) override;

		void clearQueue() noexcept;

		inline BOOL isLost() const { return m_lost; }

	protected:
		virtual BOOL initialize() override;
		virtual void task() override;
		virtual void starting() override;
		virtual void stopped() override;
	private:
		void checkQueue();

		BOOL m_interrupt;
		BOOL m_lost;
		std::mutex _m;
		std::condition_variable _cv;
		std::queue<DataFrame> m_jobs;
		zmq::context_t m_context;
		zmq::socket_t m_socket;
	};
}