#include "pch.h"
#include "TaskQueryRequestHandler.h"
#include "WebSocketPacket.h"
#include "RoSatTaskManager.h"


RoSatProcessor::TaskQueryRequestHandler::TaskQueryRequestHandler(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 50)
{

}

RoSatProcessor::TaskQueryRequestHandler::TaskQueryRequestHandler(const TaskQueryRequestHandler& src)
	: TaskQueryRequestHandler(src.m_serviceName, src.m_taskName)
{

}

void RoSatProcessor::TaskQueryRequestHandler::stop()
{
	RoSatTask::stop();
}

void RoSatProcessor::TaskQueryRequestHandler::Enqueue(const DataFrame& value)
{

}

BOOL RoSatProcessor::TaskQueryRequestHandler::initialize()
{
	m_context = zmq::context_t(1);
	m_socket = zmq::socket_t(m_context, zmq::socket_type::rep);
	m_socket.bind("tcp://127.0.0.1:50000");
	return TRUE;
}



void RoSatProcessor::TaskQueryRequestHandler::task()
{
	// Receive From Client
	zmq::message_t rep;
	auto rr = m_socket.recv(rep, zmq::recv_flags::dontwait);
	if (!rr.has_value()) { return; }

	DataFrame buffer(static_cast<char*>(rep.data()), rep.size());
	DataFrame msg;

	try {
		auto ret = QueryPacket::DeserializePacket(buffer);

		if (ret.Dispatcher == DispatcherType::NoResponse || ret.Dispatcher == DispatcherType::Postpone) {
			ExecuteQueryAsync(ret);
			msg << QueryPacket::SerializePacket(QueryPacket{ ret.Id, ret.Name, QueryType::ACK, ret.Dispatcher });
		}
		else if (ret.Dispatcher == DispatcherType::ImmediateResponse) {
			msg << QueryPacket::SerializePacket(ExecuteQuery(ret));
		}
	}
	catch (const msgpack::type_error&) {
		// Invalid Data
	}
	catch (const msgpack::parse_error&) {

	}
	catch (const std::exception&) {

	}
	
	zmq::message_t req(msg.data(), msg.size());
	auto rs = m_socket.send(req, zmq::send_flags::dontwait);
	if (!rs.has_value()) { return; }
}

void RoSatProcessor::TaskQueryRequestHandler::starting()
{

}

void RoSatProcessor::TaskQueryRequestHandler::stopped()
{
	m_socket.close();
}

RoSatProcessor::QueryPacket RoSatProcessor::TaskQueryRequestHandler::ExecuteQuery(const QueryPacket& packet)
{
	if (packet.Type == QueryType::Command) {
		auto ret = ErrorPacket(0, "Do not support Dispatcher::ImmediateResponse for command queries.");
		return QueryPacket{ packet.Id, packet.Name, QueryType::Error, packet.Dispatcher, ErrorPacket::SerializePacket(ret).toVector() };
	}
	else if (packet.Type == QueryType::Data) {

	}
	else if (packet.Type == QueryType::Schedule) {
	}
	else if (packet.Type == QueryType::Service) {
	}
}

void RoSatProcessor::TaskQueryRequestHandler::ExecuteQueryAsync(const QueryPacket& packet) {
	if (packet.Type == QueryType::Command) {
		auto command = CommandCpPacket::DeserializePacket(packet.Payload);
		command.QueryId = packet.Id;

		auto request = WebSocketPacket::CreateCPCommand(command);
		RoSatTaskManager::message(TEXT("WebSocketConnector"), std::move(request.Serialize()));
	}
	else if (packet.Type == QueryType::Data) {
	}
	else if (packet.Type == QueryType::Schedule) {
	}
	else if (packet.Type == QueryType::Service) {
	}
}