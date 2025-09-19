#pragma once
#include "RoSatTask.h"
#include "zmq.hpp"
#include <queue>

namespace RoSatProcessor {
	class TaskQueryRequestHandler : public RoSatTask {
	public:
		TaskQueryRequestHandler(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskQueryRequestHandler(const TaskQueryRequestHandler&);
		virtual ~TaskQueryRequestHandler() = default;

		virtual void stop() override;
		virtual void Enqueue(const DataFrame& value) override;

		static void Dequeue(uint64_t id);

	protected:
		virtual BOOL initialize() override;
		virtual void task() override;
		virtual void starting() override;
		virtual void stopped() override;
	private:
		static std::queue<uint64_t> m_qid;
		QueryPacket ExecuteQuery(const QueryPacket& packet);
		void ExecuteQueryAsync(const QueryPacket& packet);
		zmq::context_t m_context;
		zmq::socket_t m_socket;
	};
}