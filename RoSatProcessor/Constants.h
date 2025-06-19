#pragma once
#include "pch.h"
#include <chrono>

namespace RoSatProcessor {
	constexpr std::chrono::seconds	TASK_TIME_0UT	= std::chrono::seconds(5);
	constexpr unsigned long			TRANSMIT_INTERVAL = 100; // unit: ms
	constexpr unsigned long			TRANSMIT_LINGER	= 3000; // unit: ms
	constexpr size_t				TRANSMIT_MAX_QUEUE = 1;
}