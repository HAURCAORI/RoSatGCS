#include "pch.h"
#include "Constants.h"
#include "TaskQueryResponseSender.h"
#include "WebSocketPacket.h"
#include "RoSatTaskManager.h"


RoSatProcessor::TaskQueryResponseSender::TaskQueryResponseSender(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 50), m_interrupt(FALSE)
{

}

RoSatProcessor::TaskQueryResponseSender::TaskQueryResponseSender(const TaskQueryResponseSender& src)
	: TaskQueryResponseSender(src.m_serviceName, src.m_taskName)
{

}

void RoSatProcessor::TaskQueryResponseSender::stop()
{
	RoSatTask::stop();
}

void RoSatProcessor::TaskQueryResponseSender::Enqueue(const DataFrame& value)
{
	m_jobs.emplace(value);
	_cv.notify_one();
}

BOOL RoSatProcessor::TaskQueryResponseSender::initialize()
{
	m_context = zmq::context_t(1);
	m_socket = zmq::socket_t(m_context, zmq::socket_type::req);
	m_socket.bind("tcp://127.0.0.1:50001");
	return TRUE;
}

void RoSatProcessor::TaskQueryResponseSender::task()
{
	std::unique_lock<std::mutex> l(_m);
	auto r = _cv.wait_for(l, TASK_TIME_0UT, [this]() { return !this->m_jobs.empty() || this->m_interrupt; });
	if (!r && m_jobs.empty()) { return; }
	if (m_interrupt && m_jobs.empty()) { return; }

	DataFrame df = std::move(m_jobs.front());
	m_jobs.pop();
	l.unlock();


	zmq::message_t req(df.data(), df.size());
	m_socket.send(req, zmq::send_flags::none);

	//Receive
	zmq::pollitem_t items[] = { {static_cast<void*>(m_socket), 0, ZMQ_POLLIN, 0} };
	int rc = zmq::poll(items, 1, std::chrono::milliseconds(3000));

	if (rc > 0 && (items[0].revents & ZMQ_POLLIN)) {
		zmq::message_t rep;
		auto ret = m_socket.recv(rep);
		std::string response(static_cast<char*>(rep.data()), rep.size());
		//spdlog::info("Received response: {}", response);
	}
	else {
		spdlog::warn("Response timeout for request");
	}
}

void RoSatProcessor::TaskQueryResponseSender::starting()
{

}

void RoSatProcessor::TaskQueryResponseSender::stopped()
{
	m_socket.close();
}
