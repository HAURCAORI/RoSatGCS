#include "pch.h"
#include "fidl_parser_interface.h"

using namespace RoSatParser;

StringResult AllocString(const std::string& str) {
	StringResult ret = new(std::nothrow) char[str.size() + 1];
	if (ret != nullptr) {
		strcpy_s(ret, str.size() + 1, str.c_str());
	}
	return ret;
}

extern "C" RoSatParserAPI void RoSatParser::DisposeString(StringResult instance) {
	if (instance != nullptr)
		delete instance;
}

extern "C" RoSatParserAPI FIDL::FIDLParser * RoSatParser::FIDL::CreateParser() {
	return new FIDL::FIDLParser();
}

extern "C" RoSatParserAPI void RoSatParser::FIDL::Destroy(FIDL::FIDLParser * instance) {
	if (instance != nullptr)
		delete instance;
}
extern "C" RoSatParserAPI ParseResult RoSatParser::FIDL::Parse(FIDL::FIDLParser * instance, const unsigned char* target, int length) {
	if (instance == nullptr) { return ParseResult::ArgumentFail; }
	if (target == nullptr) { return ParseResult::ArgumentFail; }
	if (length == 0) { return ParseResult::ArgumentFail; }
	return instance->Parse(target, length);
}

extern "C" RoSatParserAPI ParseResult RoSatParser::FIDL::ParseString(FIDL::FIDLParser * instance, const std::string& target) {
	return Parse(instance, reinterpret_cast<const unsigned char*>(target.c_str()), (int) target.size());
}

extern "C" RoSatParserAPI ParseResult RoSatParser::FIDL::ParseToFile(FIDL::FIDLParser * instance, const unsigned char* target, int length, const unsigned char* path, int pathLength) {
	if (instance == nullptr) { return ParseResult::ArgumentFail; }
	if (target == nullptr) { return ParseResult::ArgumentFail; }
	if (length == 0) { return ParseResult::ArgumentFail; }
	if (path == nullptr) { return ParseResult::ArgumentFail; }
	if (pathLength == 0) { return ParseResult::ArgumentFail; }
	ParseResult ret = instance->Parse(target, length);
	if (ret != ParseResult::Success) {
		return ret;
	}
	if (!(instance->GetResultFile(path, pathLength))) {
		return ParseResult::FileOpenFail;
	}
	return ret;
}

extern "C" RoSatParserAPI ParseResult RoSatParser::FIDL::ParseStringToFile(FIDL::FIDLParser * instance, const std::string & target, const std::string & file) {
	return ParseToFile(instance, reinterpret_cast<const unsigned char*>(target.c_str()), (int)target.size(), reinterpret_cast<const unsigned char*>(file.c_str()), (int)file.size());
}

extern "C" RoSatParserAPI StringResult RoSatParser::FIDL::GetResult(FIDL::FIDLParser * instance) {
	return AllocString(instance->GetResult());
}
