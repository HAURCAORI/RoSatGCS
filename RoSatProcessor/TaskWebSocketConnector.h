#pragma once
#include "RoSatTask.h"
#include "AsyncWebSocket.h"
#include "WebSocketPacket.h"
#include <queue>
#include <boost/asio/steady_timer.hpp>

using work_guard_type = boost::asio::executor_work_guard<boost::asio::io_context::executor_type>;

namespace RoSatProcessor {
	class TaskWebSocketConnector : public RoSatTask {
	public:
		TaskWebSocketConnector(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskWebSocketConnector(const TaskWebSocketConnector&);
		virtual ~TaskWebSocketConnector() = default;

		virtual void stop() override;
		virtual void Enqueue(const DataFrame& value) override;

	protected:
		virtual BOOL initialize() override;
		virtual void task() override;
		virtual void starting() override;
		virtual void stopped() override;
	private:
		void onTimeout(uint64_t id);
		void onReceived(const std::string& str);
		std::shared_ptr<AsyncWebSocket> _socket;

		boost::asio::io_context ioContext_;
		std::thread backgroundThread_;
		std::unique_ptr<work_guard_type> work_guard_;

		BOOL m_interrupt;
		std::mutex _m;
		std::mutex _m_e;
		std::condition_variable _cv;
		std::priority_queue<WebSocketPacket> m_jobs;
		std::unordered_map<WebSocketPacket, std::shared_ptr<boost::asio::steady_timer>> m_executed;
	};
}