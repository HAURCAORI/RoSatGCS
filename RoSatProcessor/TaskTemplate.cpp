#include "TaskTemplate.h"


RoSatProcessor::TaskTemplate::TaskTemplate(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 50)
{

}

RoSatProcessor::TaskTemplate::TaskTemplate(const TaskTemplate& src)
	: TaskTemplate(src.m_serviceName, src.m_taskName)
{

}

BOOL RoSatProcessor::TaskTemplate::initialize()
{
	return TRUE;
}

void RoSatProcessor::TaskTemplate::task()
{

}

void RoSatProcessor::TaskTemplate::starting()
{

}

void RoSatProcessor::TaskTemplate::stopped()
{

}
