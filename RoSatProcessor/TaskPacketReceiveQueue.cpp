#include "pch.h"
#include "TaskPacketReceiveQueue.h"
#include "RoSatTaskManager.h"

RoSatProcessor::TaskPacketReceiveQueue::TaskPacketReceiveQueue(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 50)
{

}

RoSatProcessor::TaskPacketReceiveQueue::TaskPacketReceiveQueue(const TaskPacketReceiveQueue& src)
	: TaskPacketReceiveQueue(src.m_serviceName, src.m_taskName)
{

}

void RoSatProcessor::TaskPacketReceiveQueue::stop()
{
	RoSatTask::stop();
}

void RoSatProcessor::TaskPacketReceiveQueue::Enqueue(const DataFrame& value)
{

}

BOOL RoSatProcessor::TaskPacketReceiveQueue::initialize()
{
	m_context = zmq::context_t(1);
	m_socket = zmq::socket_t(m_context, zmq::socket_type::sub);
	m_socket.bind("tcp://127.0.0.1:60000");
	m_socket.set(zmq::sockopt::subscribe, "");

	return TRUE;
}

void RoSatProcessor::TaskPacketReceiveQueue::task()
{
	zmq::message_t message;
	if (m_socket.recv(message, zmq::recv_flags::dontwait)) {
		DataFrame buffer(static_cast<char*>(message.data()), message.size());
		RoSatTaskManager::message(TEXT("PacketProcess"), buffer);
	}
}

void RoSatProcessor::TaskPacketReceiveQueue::starting()
{

}

void RoSatProcessor::TaskPacketReceiveQueue::stopped()
{
	m_socket.close();
	m_context.close();
}
