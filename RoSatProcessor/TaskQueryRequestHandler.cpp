#include "pch.h"
#include "TaskQueryRequestHandler.h"
#include "WebSocketPacket.h"
#include "RoSatTaskManager.h"
#include "Helper.h"

static std::unordered_map<int, std::vector<uint8_t>> commandLookup;
std::queue<uint64_t> RoSatProcessor::TaskQueryRequestHandler::m_qid;

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

void RoSatProcessor::TaskQueryRequestHandler::Dequeue(uint64_t id)
{
	while(!m_qid.empty()) {
		if (m_qid.front() == id) {
			m_qid.pop();
			return;
		}
		m_qid.pop();
	}
}

BOOL RoSatProcessor::TaskQueryRequestHandler::initialize()
{
	commandLookup = readCsvLookup("C:\\Project\\RoSatGCS\\x64\\Debug\\Sample\\table.csv");
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
	//return;

	DataFrame buffer(static_cast<char*>(rep.data()), rep.size());
	DataFrame msg;

	try {
		auto ret = QueryPacket::DeserializePacket(buffer);

		if (ret.Dispatcher == DispatcherType::NoResponse || ret.Dispatcher == DispatcherType::Postpone || ret.Dispatcher == DispatcherType::FileTransfer) {
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

		if (Config::GetDebugMode()) {
			uint32_t cmd = command.Gateway;
			if (command.Gateway == 1212 || command.Gateway == 1300) {
				cmd =
					static_cast<uint32_t>(command.Payload[3]) |
					(static_cast<uint32_t>(command.Payload[4]) << 8) |
					(static_cast<uint32_t>(command.Payload[5]) << 16) |
					(static_cast<uint32_t>(command.Payload[6]) << 24);
			}

			QueryPacket queryPacket = { packet.Id, packet.Name, QueryType::Command, DispatcherType::NoResponse };
			queryPacket.Type = QueryType::Command;
			CommandCpResultPacket cpResultPacket = { };

			auto it = commandLookup.find(cmd);
			if (it != commandLookup.end()) {
				DataFrame df(it->second);
				std::cout << df << std::endl;
				cpResultPacket.Payload = it->second;
			}

			queryPacket.Payload = CommandCpResultPacket::SerializePacket(cpResultPacket).toVector();
			RoSatTaskManager::message(TEXT("QueryResponse"), std::move(QueryPacket::SerializePacket(queryPacket)));
			return;
		}
		auto request = WebSocketPacket::CreateCPCommand(command);
		m_qid.push(request.Id());
		RoSatTaskManager::message(TEXT("WebSocketConnector"), std::move(request.Serialize()));
	}
	else if (packet.Type == QueryType::Radio) {
		auto command = CommandRadioPacket::DeserializePacket(packet.Payload);
		command.QueryId = packet.Id;

		if (command.RemoteRadioMac != 0) {
			auto request = WebSocketPacket::CreateRadioConn(command);
			m_qid.push(request.Id());
			RoSatTaskManager::message(TEXT("WebSocketConnector"), std::move(request.Serialize()));
		}

		if (command.RFConfig != 0 && command.DownlinkFrequency != 0 && command.UplinkFrequency != 0) {
			auto request = WebSocketPacket::CreateUpdateRadio(command);
			m_qid.push(request.Id());
			RoSatTaskManager::message(TEXT("WebSocketConnector"), std::move(request.Serialize()));
		}

		if (command.AES.IV != std::array<uint8_t, 16>{0}&& command.AES.Key != std::array<uint8_t, 32>{0}) {
			auto request = WebSocketPacket::CreateUpdateAESKey(command);
			m_qid.push(request.Id());
			RoSatTaskManager::message(TEXT("WebSocketConnector"), std::move(request.Serialize()));
		}
	}
	else if (packet.Type == QueryType::Config) {
		auto command = ProcessorConfigPacket::DeserializePacket(packet.Payload);
		command.QueryId = packet.Id;

		Config::SetWebSocketHost(command.IP);
		Config::SetWebSocketPort(std::to_string(command.Port));
		Config::SetWebSocketTLS(command.TLS);

		RoSatTaskManager::restart(TEXT("WebSocketConnector"));
	}
	else if (packet.Type == QueryType::Data) {
	}
	else if (packet.Type == QueryType::Schedule) {
	}
	else if (packet.Type == QueryType::Service) {

	}
	else if (packet.Type == QueryType::Debug) {
		auto command = ProcessorDebugPacket::DeserializePacket(packet.Payload);
		Config::SetDebugMode(command.Debug);
	}
	else if (packet.Type == QueryType::FwUpdate) {
		auto command = FirmwareUpdatePacket::DeserializePacket(packet.Payload);
		command.QueryId = packet.Id;
		if (command.IsFile) {
			auto request = WebSocketPacket::CreateFilePacket(command);
			m_qid.push(request.Id());
			RoSatTaskManager::message(TEXT("WebSocketConnector"), std::move(request.Serialize()));
		}
		else if (command.IsBundle) {
			auto request = WebSocketPacket::CreateFWUpdBundle(command);
			m_qid.push(request.Id());
			RoSatTaskManager::message(TEXT("WebSocketConnector"), std::move(request.Serialize()));
		}
		else {
			auto request = WebSocketPacket::CreateFWUpd(command);
			m_qid.push(request.Id());
			RoSatTaskManager::message(TEXT("WebSocketConnector"), std::move(request.Serialize()));
		}
	}
	else if (packet.Type == QueryType::Cancel) {
		auto command = CancelPacket::DeserializePacket(packet.Payload);
		command.QueryId = packet.Id;
		while(!m_qid.empty()) {
			command.CommandId = m_qid.front();
			auto request = WebSocketPacket::CreateCancel(command);
			RoSatTaskManager::message(TEXT("WebSocketConnector"), std::move(request.Serialize()));
			m_qid.pop();
		}
	}
	
}