#include "TaskSerial.h"
#include "DataFrame.h"

void callback(const char* ptr, size_t len) {
	RoSatProcessor::DataFrame df(ptr, len);
	
	std::cout << df << std::endl;
}

RoSatProcessor::TaskSerial::TaskSerial(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 100)
{

}

RoSatProcessor::TaskSerial::TaskSerial(const TaskSerial& src)
	: TaskSerial(src.m_serviceName, src.m_taskName)
{
	
}

BOOL RoSatProcessor::TaskSerial::initialize()
{
	serial.open("COM5", 115200);
	serial.setCallback(&callback);
	return !serial.errorStatus();
}

void RoSatProcessor::TaskSerial::task()
{
	DataFrame df;

	serial.write(df);
}

void RoSatProcessor::TaskSerial::starting()
{

}

void RoSatProcessor::TaskSerial::stopped()
{
	serial.close();
}
