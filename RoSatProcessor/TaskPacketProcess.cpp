#include "pch.h"
#include "TaskPacketProcess.h"
#include "pmt/pmt.h"
#include "Constants.h"
#include "Helper.h"


RoSatProcessor::TaskPacketProcess::TaskPacketProcess(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 100), m_interrupt(FALSE)
{

}

RoSatProcessor::TaskPacketProcess::TaskPacketProcess(const TaskPacketProcess& src)
	: TaskPacketProcess(src.m_serviceName, src.m_taskName)
{

}

void RoSatProcessor::TaskPacketProcess::stop()
{
	m_interrupt = true;
	_cv.notify_one();
	RoSatTask::stop();
}

void RoSatProcessor::TaskPacketProcess::Enqueue(const DataFrame& df)
{
	m_jobs.emplace(df);
	_cv.notify_one();
}


BOOL RoSatProcessor::TaskPacketProcess::initialize()
{
	return TRUE;
}

void RoSatProcessor::TaskPacketProcess::task()
{
	std::unique_lock<std::mutex> l(_m);
	auto r = _cv.wait_for(l, TASK_TIME_0UT, [this]() { return !this->m_jobs.empty() || this->m_interrupt; });
	if (!r && m_jobs.empty()) { return; }
	if (m_interrupt && m_jobs.empty()) { return; }

	DataFrame df = std::move(m_jobs.front());
	m_jobs.pop();

	pmt::pmt_t msg = pmt::deserialize(*df.get());

	pmt::pmt_t payload = pmt::cdr(msg);
	
	if (pmt::is_u8vector(payload)) {
		size_t len = pmt::length(payload);
		const uint8_t* data = pmt::u8vector_elements(payload, len);
		printf("payload = %lu\n", (unsigned long)len);

		std::string str = byteArrayToHexString((char*)data, len);
		str += "\r\n";
		print(str);
	}
}

void RoSatProcessor::TaskPacketProcess::starting()
{

}

void RoSatProcessor::TaskPacketProcess::stopped()
{

}
