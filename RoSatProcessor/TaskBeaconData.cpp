#include "pch.h"
#include "TaskBeaconData.h"
#include "RosatTaskManager.h"
#include <random>


RoSatProcessor::TaskBeaconData::TaskBeaconData(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 1000)
{

}

RoSatProcessor::TaskBeaconData::TaskBeaconData(const TaskBeaconData& src)
	: TaskBeaconData(src.m_serviceName, src.m_taskName)
{

}

void RoSatProcessor::TaskBeaconData::stop()
{
	RoSatTask::stop();
}

void RoSatProcessor::TaskBeaconData::Enqueue(const DataFrame& value)
{
}

BOOL RoSatProcessor::TaskBeaconData::initialize()
{
	return TRUE;
}

void RoSatProcessor::TaskBeaconData::task()
{
	static const std::array<uint8_t, 16> emptyQueryId = {};
	QueryPacket queryPacket = { emptyQueryId , "data", QueryType::Data, DispatcherType::NoResponse};
	uint16_t DataID = 0;
	DataType Type = DataType::UInt8;
	uint8_t TypeSize = 1;
	uint16_t Count = 1;
	std::vector<uint8_t> Payload;

	std::random_device rd;
	std::mt19937 gen(rd());
	std::uniform_int_distribution<> dis(0, 255);
	for (int i = 0; i < Count * TypeSize; i++) {
		uint8_t value = static_cast<uint8_t>(dis(gen));
		Payload.push_back(value);
	}

	std::vector<BeaconTimestampPacket> Timestamps;
	for (int i = 0; i < Count; i++) {
		BeaconTimestampPacket timestamp;
		timestamp.Unix = static_cast<uint32_t>(time(nullptr));
		timestamp.SubTicks = 0;
		Timestamps.push_back(timestamp);
	}

	queryPacket.Payload = BeaconDataPacket::SerializePacket(BeaconDataPacket(DataID, Type, Count, Timestamps, Payload)).toVector();

	RoSatTaskManager::message(TEXT("QueryResponse"), std::move(QueryPacket::SerializePacket(queryPacket)));
}

void RoSatProcessor::TaskBeaconData::starting()
{

}

void RoSatProcessor::TaskBeaconData::stopped()
{

}
