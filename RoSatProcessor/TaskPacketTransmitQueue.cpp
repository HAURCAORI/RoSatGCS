#include "pch.h"
#include "TaskPacketTransmitQueue.h"
#include "RoSatTaskManager.h"
#include "pmt/pmt.h"
#include "Constants.h"

RoSatProcessor::TaskPacketTransmitQueue::TaskPacketTransmitQueue(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, TRANSMIT_INTERVAL), m_interrupt(false), m_lost(false)
{
}

RoSatProcessor::TaskPacketTransmitQueue::TaskPacketTransmitQueue(const TaskPacketTransmitQueue& src)
	: TaskPacketTransmitQueue(src.m_serviceName, src.m_taskName)
{

}

void RoSatProcessor::TaskPacketTransmitQueue::stop()
{
	m_interrupt = true;
	_cv.notify_one();
	if (!m_jobs.empty()) {
		Sleep(TRANSMIT_LINGER);
	}
	RoSatTask::stop();
}

void RoSatProcessor::TaskPacketTransmitQueue::Enqueue(const DataFrame& df)
{
	m_jobs.emplace(df);
	_cv.notify_one();
}

void RoSatProcessor::TaskPacketTransmitQueue::clearQueue() noexcept
{
	std::unique_lock<std::mutex> l(_m);
	std::queue<DataFrame> empty;
	std::swap(m_jobs, empty);
}

BOOL RoSatProcessor::TaskPacketTransmitQueue::initialize()
{
	m_context = zmq::context_t(1);
	m_socket = zmq::socket_t(m_context, zmq::socket_type::rep);
	m_socket.bind("tcp://127.0.0.1:60001");

	return TRUE;
}

void RoSatProcessor::TaskPacketTransmitQueue::task()
{
	std::unique_lock<std::mutex> l(_m);
	auto r = _cv.wait_for(l, TASK_TIME_0UT,
		[this]() { return !this->m_jobs.empty() || this->m_interrupt; });
	if (!r && m_jobs.empty()) { return; }
	if (m_interrupt && m_jobs.empty()) { return; }

	DataFrame df = m_jobs.front();

	zmq::message_t rep;
	auto rr = m_socket.recv(rep, zmq::recv_flags::dontwait);
	if (m_lost = !rr.has_value()) {
		checkQueue();
		return;
	}

	// Make pmt message
	pmt::pmt_t vec = pmt::make_u8vector(df.size(), 0);

	for (size_t i = 0; i < df.size(); ++i) {
		pmt::u8vector_set(vec, i, df[i]);
	}

	pmt::pmt_t pmt_pair = pmt::cons(pmt::PMT_NIL, vec);

	DataFrame msg;
	pmt::serialize(pmt_pair, msg);

	// Send Data
	zmq::message_t req(msg.data(), msg.size());
	auto rs = m_socket.send(req, zmq::send_flags::dontwait);
	if (!rs.has_value()) { return; }

	// TODO: Log the success or fail

	m_jobs.pop();
}

void RoSatProcessor::TaskPacketTransmitQueue::starting()
{

}

void RoSatProcessor::TaskPacketTransmitQueue::stopped()
{
	m_socket.close();
	m_context.close();
}

void RoSatProcessor::TaskPacketTransmitQueue::checkQueue()
{
	if (m_jobs.empty()) { return; }
	while (m_jobs.size() < TRANSMIT_MAX_QUEUE) {
		m_jobs.pop();
	}
}
