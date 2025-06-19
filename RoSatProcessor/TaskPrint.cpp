#include "pch.h"
#include "TaskPrint.h"
#include "RoSatTaskManager.h"
#include "pmt/pmt.h"
#include "Helper.h"
#include "msgpack.hpp"


RoSatProcessor::TaskPrint::TaskPrint(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 1000)
{
	
}

RoSatProcessor::TaskPrint::TaskPrint(const TaskPrint& src)
	: TaskPrint(src.m_serviceName, src.m_taskName)
{

}

void RoSatProcessor::TaskPrint::stop()
{
	RoSatTask::stop();
}

void RoSatProcessor::TaskPrint::Enqueue(const DataFrame& value)
{
}

BOOL RoSatProcessor::TaskPrint::initialize()
{
	m_context = zmq::context_t(1);
	m_socket = zmq::socket_t(m_context, zmq::socket_type::rep);
	m_socket.bind("tcp://127.0.0.1:60000");
	//m_socket.set(zmq::sockopt::subscribe, "");

	return TRUE;
}

void RoSatProcessor::TaskPrint::task()
{
	zmq::message_t rep;
	auto rr = m_socket.recv(rep, zmq::recv_flags::dontwait);
	if (!rr.has_value()) {
		return;
	}

	DataFrame ret(reinterpret_cast<const char*>(rep.data()), rep.size());
	std::cout << ret << std::endl;

	DataFrame msg;
	zmq::message_t req(msg.data(), msg.size());
	auto rs = m_socket.send(rep, zmq::send_flags::dontwait);
	if (!rs.has_value()) { return; }
	
	
	/*
	pmt::pmt_t pm = pmt::deserialize(*ret.get());
	
	pmt::pmt_t payload = pmt::cdr(pm);

	if (pmt::is_u8vector(payload)) {
		size_t len = pmt::length(payload);
		const uint8_t* data = pmt::u8vector_elements(payload, len);
		printf("payload = %lu\n", (unsigned long)len);

		std::string str = byteArrayToHexString((char*)data, len);
		str += "\r\n";
		print(str);
	}


	/*
	zmq::message_t message;
	if (m_socket.recv(message, zmq::recv_flags::dontwait)) {
		DataFrame buffer(static_cast<char*>(message.data()), message.size());
		std::cout << buffer << std::endl;
	}*/
}

void RoSatProcessor::TaskPrint::starting()
{
	
}

void RoSatProcessor::TaskPrint::stopped()
{
	m_socket.close();
	m_context.close();
}
