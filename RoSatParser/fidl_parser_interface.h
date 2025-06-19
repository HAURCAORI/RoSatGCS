#pragma once

#ifndef FIDL_PARSER_INTERFACE_H_
#define FIDL_PARSER_INTERFACE_H_

#ifdef _USRDLL
#define RoSatParserAPI __declspec(dllexport)
#else
#define RoSatParserAPI __declspec(dllimport)
#endif

#include "fidl_parser.h"

namespace RoSatParser {
	extern "C" RoSatParserAPI void DisposeString(StringResult instance);
	namespace FIDL {
		extern "C" RoSatParserAPI FIDLParser * CreateParser();
		extern "C" RoSatParserAPI void Destroy(FIDLParser * instance);
		extern "C" RoSatParserAPI ParseResult Parse(FIDLParser * instance, const unsigned char* target, int length);
		extern "C" RoSatParserAPI ParseResult ParseString(FIDLParser * instance, const std::string & target);
		extern "C" RoSatParserAPI ParseResult ParseToFile(FIDL::FIDLParser * instance, const unsigned char* target, int length, const unsigned char* path, int pathLength);
		extern "C" RoSatParserAPI ParseResult ParseStringToFile(FIDL::FIDLParser * instance, const std::string & target, const std::string & file);
		extern "C" RoSatParserAPI StringResult GetResult(FIDLParser * instance);
	}
}

#endif
