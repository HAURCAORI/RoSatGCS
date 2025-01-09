#pragma once
#include "AsyncSerial.h"

#include "RoSatTask.h"

namespace RoSatProcessor {
	class TaskSerial : public RoSatTask {
	public:
		TaskSerial(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskSerial(const TaskSerial&);
		virtual ~TaskSerial() = default;

	protected:
		virtual BOOL initialize() override;
		virtual void task() override;
		virtual void starting() override;
		virtual void stopped() override;
	private:
		CallbackAsyncSerial serial;
	};
}