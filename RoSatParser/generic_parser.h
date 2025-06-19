#pragma once

#ifndef GENERIC_PARSER_H_
#define GENERIC_PARSER_H_

#include <vector>
#include <fstream>
#include <sstream>
#include "parser_types.h"

#ifdef _DEBUG
#include <iostream>
#endif

namespace RoSatParser {

	// Define specific parsing method
	// 'IsBinary' is true, Derived parser must override 'const unsigned char*' version
	// 'IsBinary' is false, Derived parser must override 'const std::string&' version
	template<bool IsBinary>
	class IParsing {};

	template<> class IParsing<false> {
	protected:
		virtual ParsingContext Parsing(const unsigned char* target, int length, int line) = delete;
		virtual ParsingContext Parsing(const std::string& target, int length, int line) = 0;
	};

	template<> class IParsing<true> {
	protected:
		virtual ParsingContext Parsing(const unsigned char* target, int length, int line) = 0;
		virtual ParsingContext Parsing(const std::string& target, int length, int line) = delete;
	};

	// GenericParser<IsFile, IsBinary>
	template<bool IsFile, bool IsBinary>
	class GenericParser : public IParsing<IsBinary> {
	public:
		GenericParser() = default;
		virtual ~GenericParser() = default;
		GenericParser(GenericParser const&) = delete;
		GenericParser& operator=(GenericParser const&) = delete;
		GenericParser(GenericParser&&) = delete;
		GenericParser& operator=(GenericParser&&) = delete;

		virtual ParseResult Parse(const unsigned char* target, int length);
		virtual void Parsed() {}
		virtual std::string GetResult() = 0;
		void Clear();

		bool isFile() const;
		bool isBinary() const;
		
	protected:
		bool OpenFile(const std::string& path);
		void CloseFile();
		std::ifstream mFile;
		std::vector<ParsingContext> mResults;

	private:
		ParseResult ParseCheck(const ParsingContext& context);
		ParseResult ParseFile(const std::string& path);
		ParseResult ParseData(const unsigned char* target, int length);
	};
}

#include "generic_parser_definition.h"

#endif