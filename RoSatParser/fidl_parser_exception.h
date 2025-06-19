#pragma once

#ifndef FIDL_PARSER_EXCEPTION_H_
#define FIDL_PARSER_EXCEPTION_H_

#include <string>
#include <exception>
#include "parser_types.h"

namespace RoSatParser::FIDL {
	class FIDLException : std::exception {
	public:
		explicit FIDLException(const char* message, ParseSeverity severity, int offset = 0)
			: message(message), offset(offset), severity(severity) {}
		explicit FIDLException(const std::string& message, ParseSeverity severity, int offset = 0)
			: message(message), offset(offset), severity(severity) {}
		virtual ~FIDLException() noexcept {}
		virtual const char* what() const noexcept {
			return message.c_str();
		}
		std::string getMessage() const { return message; }
		int getOffset() const { return offset; }
		ParseSeverity getSeverity() const { return severity; }
	protected:
		std::string message;
		int offset;
		ParseSeverity severity;

	};
}

#endif