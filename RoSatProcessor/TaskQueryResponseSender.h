#pragma once
#include "RoSatTask.h"
#include "zmq.hpp"
#include <queue>

namespace RoSatProcessor {
	class TaskQueryResponseSender : public RoSatTask {
	public:
		TaskQueryResponseSender(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskQueryResponseSender(const TaskQueryResponseSender&);
		virtual ~TaskQueryResponseSender() = default;

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

		BOOL m_interrupt;
		std::mutex _m;
		std::condition_variable _cv;
		std::queue<DataFrame> m_jobs;
	};
}