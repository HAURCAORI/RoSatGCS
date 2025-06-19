#pragma once
#include "AsyncSerial.h"
#include "RoSatTask.h"

namespace RoSatProcessor {
	class TaskSerial : public RoSatTask {
	public:
		TaskSerial(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskSerial(const TaskSerial&);
		virtual ~TaskSerial() = default;

		virtual void stop() override;
		virtual void Enqueue(const DataFrame& value) override;

	protected:
		virtual BOOL initialize() override;
		virtual void task() override;
		virtual void starting() override;
		virtual void stopped() override;
	private:
		void callback(const char* ptr, size_t len);
		CallbackAsyncSerial serial;
	};
}