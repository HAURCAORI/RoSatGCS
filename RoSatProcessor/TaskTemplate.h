#pragma once

#include "RoSatTask.h"

namespace RoSatProcessor {
	class TaskTemplate : public RoSatTask {
	public:
		TaskTemplate(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskTemplate(const TaskTemplate&);
		virtual ~TaskTemplate() = default;

		virtual void stop() override;
		virtual void Enqueue(const DataFrame& value) override;

	protected:
		virtual BOOL initialize() override;
		virtual void task() override;
		virtual void starting() override;
		virtual void stopped() override;
	private:

	};
}