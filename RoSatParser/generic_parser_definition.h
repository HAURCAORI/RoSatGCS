#pragma once

#ifndef GENERIC_PARSER_DEFINITION_H_
#define GENERIC_PARSER_DEFINITION_H_

#include <vector>
#include "generic_parser.h"

namespace RoSatParser {
	GPENTRY ParseResult GenericParser<GPOPTION>::Parse(const unsigned char* target, int length) {
		Clear();
		if (isFile()) {
			return ParseFile(reinterpret_cast<const char*>(target));
		}

		return ParseData(target, length);
	}

	GPENTRY void GenericParser<GPOPTION>::Clear() {
		mFile.close();
	}

	GPENTRY bool GenericParser<GPOPTION>::isFile() const {
		return IsFile;
	}

	GPENTRY bool GenericParser<GPOPTION>::isBinary() const {
		return IsBinary;
	}

	GPENTRY bool GenericParser<GPOPTION>::OpenFile(const std::string& path) {
		CloseFile();

		if (isBinary())
			mFile.open(path, std::ios::binary);
		else
			mFile.open(path);

		return mFile.is_open();
	}

	GPENTRY void GenericParser<GPOPTION>::CloseFile() {
		if (mFile.is_open()) { mFile.close(); }
	}

	GPENTRY ParseResult GenericParser<GPOPTION>::ParseCheck(const ParsingContext& context) {
		if (context) {
			mResults.push_back(context);
		}
		if (context.severity == ParseSeverity::Warn) {
			return ParseResult::Warn;
		}
		else if (context.severity == ParseSeverity::Error) {
			return ParseResult::Fail;
		}
		return ParseResult::None;
	}

	GPENTRY ParseResult GenericParser<GPOPTION>::ParseFile(const std::string& path) {
		if (!OpenFile(path)) { return ParseResult::FileOpenFail; }
		
		int line = 0;
		std::string str;
		ParseResult ret = ParseResult::Success;

		try {
			// Binary parsing using a buffer
			if constexpr (IsBinary) {
				// Get lenght of the file
				mFile.seekg(0, mFile.end);
				int length = (int)mFile.tellg();
				mFile.seekg(0, mFile.beg);

				// Alloc memory and copy the data
				char* buffer = new char[length];
				mFile.read(buffer, length);

				// Execute parsing
				ParseResult valid= ParseCheck(this->Parsing(reinterpret_cast<const unsigned char*>(buffer), length, line));
				// If the parsing succeeds, 'valid' might be 'ParseResult::None'
				ret = (valid != ParseResult::None) ? valid : ParseResult::Success;
			}
			else {
				// String parsing using 'std::getline()'
				while (std::getline(mFile, str)) {
					// Execute parsing
					ParseResult valid = ParseCheck(this->Parsing(str, (int) str.size(), ++line));
					// The 'ret' value is initialized by 'ParseReult::Success'
					// If the 'valid' value is not a 'ParseResult::None',
					// overwrite the 'ret' value with the 'valid' value
					ret = (valid != ParseResult::None) ? valid : ret;
					if (ret == ParseResult::Fail) { break; }
				}
			}
		}
		catch (const std::bad_alloc& ex) {
			ret = ParseResult::MemoryAllocFail;
		}
		catch (...) {
			ret = ParseResult::ExceptionFail;
		}
		CloseFile();
		Parsed();
		return ret;
	}

	GPENTRY ParseResult GenericParser<GPOPTION>::ParseData(const unsigned char* target, int length)
	{
		int line = 0;
		std::string str;
		ParseResult ret = ParseResult::Success;
		try {
			// Binary parsing
			if constexpr (IsBinary) {
				// Execute parsing
				ParseResult valid = ParseCheck(this->Parsing(target, length, line));
				// If the parsing succeeds, 'valid' might be 'ParseResult::None'
				ret = (valid != ParseResult::None) ? valid : ParseResult::Success;
			}
			else {
				std::istringstream ss(reinterpret_cast<const char*>(target));
				// String parsing using 'std::getline()'
				while (std::getline(ss, str)) {
					// Execute parsing
					ParseResult valid = ParseCheck(this->Parsing(str,(int) str.size(), ++line));
					// The 'ret' value is initialized by 'ParseReult::Success'
					// If the 'valid' value is not a 'ParseResult::None',
					// overwrite the 'ret' value with the 'valid' value
					ret = (valid != ParseResult::None) ? valid : ret;
					if (ret == ParseResult::Fail) { break; }
				}
			}
		}
		catch (...) {
			ret = ParseResult::ExceptionFail;
		}

		Parsed();
		return ret;
	}

}
#endif