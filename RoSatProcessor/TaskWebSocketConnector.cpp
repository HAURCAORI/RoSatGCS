#include "pch.h"
#include "RosatTaskManager.h"
#include "TaskWebSocketConnector.h"
#include "Constants.h"
#include "WebSocketPacket.h"
#include <condition_variable>
#include <boost/bind.hpp>


void printHex(const std::array<uint8_t, 16>& queryId) {
	std::ostringstream oss;
	oss << std::hex << std::setfill('0');

	for (const auto& byte : queryId) {
		oss << std::setw(2) << static_cast<int>(byte) << " ";
	}

	std::cout << "Query ID in Hex: " << oss.str() << std::endl;
}


RoSatProcessor::TaskWebSocketConnector::TaskWebSocketConnector(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 100), _socket(std::make_shared<AsyncWebSocket>(Config::GetWebSocketHost(), Config::GetWebSocketPort(), Config::GetWebSocketTLS())),
	m_interrupt(false)
{
	
}

RoSatProcessor::TaskWebSocketConnector::TaskWebSocketConnector(const TaskWebSocketConnector& src)
	: TaskWebSocketConnector(src.m_serviceName, src.m_taskName)
{

}

void RoSatProcessor::TaskWebSocketConnector::stop()
{
	m_interrupt = true;
	_cv.notify_one();
	RoSatTask::stop();
}

void RoSatProcessor::TaskWebSocketConnector::Enqueue(const DataFrame& value)
{
	m_jobs.emplace(WebSocketPacket::Deserialize(value));
	_cv.notify_one();
}

BOOL RoSatProcessor::TaskWebSocketConnector::initialize()
{
	m_interrupt = false;
	_socket->connect(Config::GetWebSocketHost(),Config::GetWebSocketPort(), Config::GetWebSocketTLS());
	_socket->setCallback(std::bind(&TaskWebSocketConnector::onReceived, this, std::placeholders::_1));

	ioContext_.restart();
	work_guard_ = std::make_unique<work_guard_type>(ioContext_.get_executor());
	if (backgroundThread_.joinable()) backgroundThread_.join();
	backgroundThread_ = std::thread([this]() { ioContext_.run(); });
	return TRUE;
}


void RoSatProcessor::TaskWebSocketConnector::task()
{
	if (m_interrupt) return;

	if (!_socket->isConnected()) {
		_socket->connect(Config::GetWebSocketHost(), Config::GetWebSocketPort(), Config::GetWebSocketTLS());
		return;
	}
	
	std::unique_lock<std::mutex> l(_m);
	auto r = _cv.wait_for(l, TASK_TIME_0UT, [this]() { return !this->m_jobs.empty() || this->m_interrupt; });
	if (!r && m_jobs.empty()) { return; }
	if (m_interrupt && m_jobs.empty()) { return; }

	WebSocketPacket packet = m_jobs.top();
	
	_socket->write(packet.Get());


#ifdef _DEBUG
	std::cout << "Send: " << packet.Get() << std::endl;
#endif

	auto timer = std::make_shared<boost::asio::steady_timer>(boost::asio::make_strand(ioContext_), std::chrono::milliseconds(200000));
	timer->async_wait(boost::bind(&TaskWebSocketConnector::onTimeout, this, packet.Id()));

	m_executed[packet] = timer;

	m_jobs.pop();
}

void RoSatProcessor::TaskWebSocketConnector::starting()
{

}

void RoSatProcessor::TaskWebSocketConnector::stopped()
{
	_socket->close();
	work_guard_.reset();
	ioContext_.stop();
	if (backgroundThread_.joinable()) backgroundThread_.join();
}

void RoSatProcessor::TaskWebSocketConnector::onTimeout(uint64_t id)
{
	std::lock_guard<std::mutex> l(_m_e);
	
	auto it = std::find_if(m_executed.begin(), m_executed.end(), [id](const auto& t) -> bool { return t.first.Id() == id; });
	if (it != m_executed.end()) {
		spdlog::info("Timeout: id={}", id);
		m_executed.erase(it);
	}
}

void RoSatProcessor::TaskWebSocketConnector::onReceived(const std::string& str)
{
	std::array<uint8_t, 16> queryId = {};
	uint64_t id;
	std::string type;
	{
#ifdef _DEBUG
		std::cout <<"Recv: " << str << std::endl;
#endif

		std::lock_guard<std::mutex> l(_m_e);

		try {
			type = WebSocketPacket::GetType(str);
			if (type == "Notify") {
				auto stateResult = WebSocketPacket::ParseStateResult(str);
				spdlog::info("Notify: {}", stateResult.Notifier);
				return;
			}
			id = WebSocketPacket::GetRequestId(str);
		}
		catch (std::exception e)
		{
			spdlog::error("Exception: {}", e.what());
			return;
		}
		
		auto it = std::find_if(m_executed.begin(), m_executed.end(), [id](const auto& t) { return t.first.Id() == id; });

		if (it != m_executed.end() && type != "CPCommandPartResult") {
			queryId = it->first.QueryId();
			m_executed.erase(it);
		}
	}

	if (queryId.empty()) {
		spdlog::warn("QueryId is empty");
		return;
	}

	try {
		QueryPacket queryPacket = { queryId, type, QueryType::Command, DispatcherType::NoResponse };

		if (type == "Error") {
			auto statePacket = WebSocketPacket::ParseStateResult(str);
			queryPacket.Type = QueryType::Error;
			queryPacket.Payload = CommandStatePacket::SerializePacket(statePacket).toVector();
		}
		else if (type == "CPCommandPartResult") {
			// Not implemented
			return;
		}
		else if (type == "CPCommandResult")
		{
			auto cpResultPacket = WebSocketPacket::ParseCpResult(str);
			queryPacket.Type = QueryType::Command;
			queryPacket.Payload = CommandCpResultPacket::SerializePacket(cpResultPacket).toVector();
		}
		else if (type == "RadioConnResult" || type == "RadioResult") {
			auto radioConnResult = WebSocketPacket::ParseRadioResult(str);
			queryPacket.Type = QueryType::Radio;
			queryPacket.Payload = CommandRadioResultPacket::SerializePacket(radioConnResult).toVector();
		}
		else if (type == "UploadPartResult") {
			return;
		}
		else if (type == "UploadResult") {
			auto fwUpdateResult = WebSocketPacket::ParseFwUpdateResult(str);
			queryPacket.Type = QueryType::FwUpdate;
			queryPacket.Payload = FirmwareUpdateResultPacket::SerializePacket(fwUpdateResult).toVector();
		}
		else {
			queryPacket.Type = QueryType::Error;
			queryPacket.Payload = ErrorPacket::SerializePacket(ErrorPacket{ 0, "Unknown type" }).toVector();
			spdlog::warn("Unknown type: {}", type);
		}

		RoSatTaskManager::message(TEXT("QueryResponse"), std::move(QueryPacket::SerializePacket(queryPacket)));
	}
	catch (std::exception e)
	{
		QueryPacket errorPacket = { queryId, "Error", QueryType::Error, DispatcherType::NoResponse };
		errorPacket.Payload = ErrorPacket::SerializePacket(ErrorPacket{ 0, e.what() }).toVector();
		RoSatTaskManager::message(TEXT("QueryResponse"), std::move(QueryPacket::SerializePacket(errorPacket)));
		spdlog::error("Parsing error: {}", e.what());
	}
}
