#include "TaskPrint.h"
#include "RoSatTaskManager.h"
#include "pmt/pmt.h"
#include "DataFrame.h"
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

BOOL RoSatProcessor::TaskPrint::initialize()
{

	return TRUE;
}

void RoSatProcessor::TaskPrint::task()
{
	static int i;
	DataFrame df;
	df << i++ << 'a';
	RoSatTaskManager::message(TEXT("PacketTransmitQueue"), df);
}

void RoSatProcessor::TaskPrint::starting()
{
	
}

void RoSatProcessor::TaskPrint::stopped()
{

}
