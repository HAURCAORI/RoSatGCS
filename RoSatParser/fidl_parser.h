#pragma once

#ifndef FIDL_PARSER_H_
#define FIDL_PARSER_H_

#include <map>
#include <stack>
#include "generic_parser.h"
#include "re2/re2.h"
#include "fidl_parser_types.h"

namespace RoSatParser::FIDL {
	class FIDLParser : public GenericParser<true,false> {
	public:
		FIDLParser();
		virtual ~FIDLParser() = default;
		FIDLParser(FIDLParser const&) = delete;
		FIDLParser& operator=(FIDLParser const&) = delete;
		FIDLParser(FIDLParser&&) = delete;
		FIDLParser& operator=(FIDLParser&&) = delete;

		enum class Scope {
			Root, TypeCollection, Interface, Version,
			Enumeration, Struct, Method, MethodIn,
			MethodOut, MethodError, Union, Map,
			Broadcast, BroadcastOut, PSM, PSMState
		};

		virtual std::string GetResult() override;
		virtual bool GetResultFile(const unsigned char* path, int length);


	protected:
		virtual ParsingContext Parsing(const std::string& target, int length, int line) override;
		virtual void Parsed() override;
		virtual std::optional<std::string> Preprocess(const std::string& target);
		virtual void LocalParsing(const std::string& str);
	private:
		void ScopeRoot(const std::string& str);
		void ScopeTypeCollection(const std::string& str);
		void ScopeInterface(const std::string& str);
		void ScopeVersion(const std::string& str);
		void ScopeEnumeration(const std::string& str);
		void ScopeStruct(const std::string& str);
		void ScopeMethod(const std::string& str);
		void ScopeMethodIn(const std::string& str);
		void ScopeMethodOut(const std::string& str);
		void ScopeMethodError(const std::string& str);
		void ScopeUnion(const std::string& str);
		void ScopeMap(const std::string& str);
		void ScopeBroadcast(const std::string& str);
		void ScopeBroadcastOut(const std::string& str);
		void ScopePSM(const std::string& str);
		void ScopePSMState(const std::string& str);

		void CommentCheck(std::string& str);
		void ParseComment();

		void ScopeCheck(int level);
		void AddElement(const std::string& str); // 최상위 노드 생성
		void Add(FIDLType type, const std::string& name); // 하위 노드 생성
		void AddExtend(const std::string& str); // Extend 항목 추가
		void Append(const FIDLType& type, const std::string& detail, const std::string& name, FIDLMethodType method = FIDLMethodType::None, int value = -1);
		void TypeCheck(const std::string& str, FIDLMethodType ismethod = FIDLMethodType::None);
		FIDLContainer* GetCurrentContainer();

		static int getID(const std::string& str);
		static int getSize(const std::string& str);

		FIDL::FIDLContext mContext;
		FIDL::FIDLContext mContextResult;
		FIDL::FIDLTag mTag;
		std::stack<Scope> mScopeStack;
		Scope mNextScope = Scope::Root;
		std::string tempStr;
		std::string mComment;
		bool isMultiLineComment = false;
		bool isInterface = false;
		int mScopeLevel = 0;
		int enumCounter = 0;

		static const std::map<Scope, void(FIDLParser::*)(const std::string&)> mScopeMapping;
		static const std::map<std::string, FIDLType> kwType;

		
	};
}

#endif