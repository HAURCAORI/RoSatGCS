#pragma once
#include "AsyncSerial.h"
#include "RoSatTask.h"

namespace RoSatProcessor {
	class TaskBeaconData : public RoSatTask {
	public:
		TaskBeaconData(PCWSTR pszServiceName, PCWSTR pszTaskName);
		TaskBeaconData(const TaskBeaconData&);
		virtual ~TaskBeaconData() = default;

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