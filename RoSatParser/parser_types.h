#pragma once
#include "pch.h"
#ifndef PARSER_TYPES_H_
#define PARSER_TYPES_H_
#include <string>

#define GPENTRY template<bool IsFile, bool IsBinary>
#define GPOPTION IsFile, IsBinary

namespace RoSatParser {
	using StringResult = char*;

	enum class ParseResult : uint8_t {
		None = 0,
		Success = 1,
		Warn = 2,
		Fail = 3,
		FileOpenFail = 4,
		MemoryAllocFail = 5,
		ExceptionFail = 6,
		ArgumentFail = 7
	};

	enum class ParseSeverity : uint8_t {
		None	= 0,
		Info	= 1,
		Warn	= 2,
		Error	= 3,
	};

	struct ParsingContext {
		ParseSeverity severity;
		int line;
		int offset = 0;
		std::string what = "";
		operator bool() const { return severity != ParseSeverity::None; }
	};

}

#endif