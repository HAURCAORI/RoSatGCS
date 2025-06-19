#include "pch.h"
#include "TaskTemplate.h"


RoSatProcessor::TaskTemplate::TaskTemplate(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 50)
{

}

RoSatProcessor::TaskTemplate::TaskTemplate(const TaskTemplate& src)
	: TaskTemplate(src.m_serviceName, src.m_taskName)
{

}

void RoSatProcessor::TaskTemplate::stop()
{
	RoSatTask::stop();
}

void RoSatProcessor::TaskTemplate::Enqueue(const DataFrame& value)
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
