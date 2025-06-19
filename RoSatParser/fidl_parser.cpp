#include "pch.h"
#include <functional>
#include <memory>
#include <optional>
#include "fidl_parser.h"
#include "fidl_parser_exception.h"

#define RAPIDJSON_HAS_STDSTRING 1
#include "rapidjson/document.h"
#include "rapidjson/stringbuffer.h"
#include "rapidjson/writer.h"

 
#ifdef _DEBUG
#include <iostream>
#endif

#define SCOPE(VAL) {FIDLParser::Scope::##VAL, &FIDLParser::Scope##VAL}
#define MTYPE(VAL) {#VAL, FIDLType::##VAL}
#define MATCH(VAL)

using namespace RoSatParser;
using namespace RoSatParser::FIDL;

// Userful Functions
inline void ltrim(std::string& s) {
	s.erase(s.begin(), std::find_if(s.begin(), s.end(), [](unsigned char ch) {
		return !std::isspace(ch);
		}));
}

inline void rtrim(std::string& s) {
	s.erase(std::find_if(s.rbegin(), s.rend(), [](unsigned char ch) {
		return !std::isspace(ch);
		}).base(), s.end());
}

inline void trim(std::string& s) {
	rtrim(s);
	ltrim(s);
}

inline std::string matchRegex(const std::string& s) {
	return "(" + s + R"()\s*([a-zA-Z0-9_]*).*)";
}

const std::map<FIDLParser::Scope, void(FIDLParser::*)(const std::string&)> FIDLParser::mScopeMapping = {
	SCOPE(Root), SCOPE(TypeCollection), SCOPE(Interface), SCOPE(Version),
	SCOPE(Enumeration), SCOPE(Struct), SCOPE(Method), SCOPE(MethodIn),
	SCOPE(MethodOut), SCOPE(MethodError),SCOPE(Union), SCOPE(Map),
	SCOPE(Broadcast), SCOPE(BroadcastOut), SCOPE(PSM), SCOPE(PSMState)
};

const std::map<std::string, FIDLType> FIDLParser::kwType = {
	MTYPE(UInt8), MTYPE(Int8), MTYPE(UInt16), MTYPE(Int16),
	MTYPE(UInt32), MTYPE(Int32), MTYPE(UInt64), MTYPE(Int64),
	MTYPE(Integer), MTYPE(Boolean), MTYPE(Float), MTYPE(Double),
	MTYPE(String), MTYPE(ByteBuffer), MTYPE(Enumeration), MTYPE(Struct),
	MTYPE(Method), MTYPE(Union), MTYPE(Map), MTYPE(UserDefined),
	MTYPE(EnumElement)
};

RoSatParser::FIDL::FIDLParser::FIDLParser() {
	mScopeStack.push(Scope::Root);
}

std::string FIDLParser::GetResult() {
	rapidjson::Document d;
	d.SetObject();
	rapidjson::Document::AllocatorType& alloc = d.GetAllocator();
	rapidjson::Value errorNode(rapidjson::kArrayType);
	rapidjson::Value warningNode(rapidjson::kArrayType);
	
	for (auto it = mResults.begin(); it != mResults.end(); ++it) {
		rapidjson::Value node(rapidjson::kObjectType);
		node.AddMember("line", it->line, alloc);
		node.AddMember("what", it->what, alloc);
		if (it->severity == ParseSeverity::Warn) {
			warningNode.PushBack(node, alloc);
		}
		else if (it->severity == ParseSeverity::Error) {
			errorNode.PushBack(node, alloc);
		}
	}

	try {
		if(!errorNode.Empty()) {
			throw FIDLException("Parsing error occured", ParseSeverity::Error);
		}

		d.AddMember("package", mContextResult.package, alloc);
		for (auto intercollection : { true, false }) {
			rapidjson::Value typeCollectionNode(rapidjson::kArrayType);
			rapidjson::Value interfaceNode(rapidjson::kArrayType);
			const auto& sel = (intercollection) ? mContextResult.interfaces : mContextResult.typeCollections;
			if (sel.empty()) { continue; }
			for (auto it = sel.begin(); it != sel.end(); ++it) {
				rapidjson::Value obj(rapidjson::kObjectType);
				obj.AddMember("name", it->name, alloc);
				obj.AddMember("description", it->description, alloc);
				obj.AddMember("id", it->id, alloc);
				rapidjson::Value version(rapidjson::kObjectType);
				version.AddMember("major", it->major, alloc);
				version.AddMember("minor", it->minor, alloc);
				obj.AddMember("version", version, alloc);

				rapidjson::Value structType(rapidjson::kArrayType);
				rapidjson::Value enumType(rapidjson::kArrayType);
				for (auto itt = it->nodes.begin(); itt != it->nodes.end(); ++itt) {
					rapidjson::Value t(rapidjson::kObjectType);
					t.AddMember("name", itt->get()->name, alloc);
					t.AddMember("description", itt->get()->description, alloc);
					auto type = itt->get()->type;
					auto ret = std::find_if(kwType.begin(), kwType.end(), [&type](const auto& x) {return x.second == GetBaseType(type); });
					if (ret == kwType.end()) {
						throw FIDLException("Unexpected type name", ParseSeverity::Error);
					}

					rapidjson::Value sub(rapidjson::kArrayType);
					for (auto ittt = itt->get()->subnode.begin(); ittt != itt->get()->subnode.end(); ++ittt) {
						auto o = ittt->get();
						auto subtype = GetBaseType(o->getType());
						auto isarray = IsArrayType(o->getType());
						if (type == FIDLType::Struct) {
							rapidjson::Value structtype(rapidjson::kObjectType);
							auto ret = std::find_if(kwType.begin(), kwType.end(), [&subtype](const auto& x) {return x.second == GetBaseType(subtype); });
							if (ret == kwType.end()) {
								throw FIDLException("Unexpected type name", ParseSeverity::Error);
							}

							structtype.AddMember("name", o->name, alloc);
							if (subtype == FIDLType::UserDefined) {
								structtype.AddMember("type", o->detail, alloc);
							}
							else {
								structtype.AddMember("type", ret->first, alloc);
							}
							structtype.AddMember("isarray", isarray, alloc);
							structtype.AddMember("size", o->id, alloc);
							structtype.AddMember("description", o->description, alloc);
							sub.PushBack(structtype, alloc);
						}
						else if (type == FIDLType::Enumeration) {
							rapidjson::Value enumtype(rapidjson::kObjectType);
							if (subtype != FIDLType::EnumElement) {
								throw FIDLException("Unexpected type", ParseSeverity::Error);
							}
							enumtype.AddMember("name", o->name, alloc);
							enumtype.AddMember("id", o->id, alloc);
							enumtype.AddMember("description", o->description, alloc);
							sub.PushBack(enumtype, alloc);
						}
					}

					if (type == FIDLType::Struct) {
						t.AddMember("subtypes", sub, alloc);
						structType.PushBack(t, alloc);
					}
					else if (type == FIDLType::Enumeration) {
						t.AddMember("values", sub, alloc);
						enumType.PushBack(t, alloc);
					}
					else {
						throw FIDLException("Only support the struct, enumeration and method type", ParseSeverity::Error);
					}
				}

				rapidjson::Value methodType(rapidjson::kArrayType);
				for (auto itt = it->methods.begin(); itt != it->methods.end(); ++itt) {
					rapidjson::Value t(rapidjson::kObjectType);
					t.AddMember("name", itt->get()->name, alloc);
					t.AddMember("id", itt->get()->id, alloc);
					t.AddMember("description", itt->get()->description, alloc);

					rapidjson::Value methodIn(rapidjson::kArrayType);
					rapidjson::Value methodOut(rapidjson::kArrayType);
					for (auto inout : { true, false }) {
						const auto& sel = (inout) ? itt->get()->in : itt->get()->out;
						for (auto ittt = sel.begin(); ittt != sel.end(); ++ittt) {
							rapidjson::Value methodDef(rapidjson::kObjectType);
							auto o = ittt->get();
							auto subtype = GetBaseType(o->getType());
							auto isarray = IsArrayType(o->getType());
							auto ret = std::find_if(kwType.begin(), kwType.end(), [&subtype](const auto& x) {return x.second == GetBaseType(subtype); });
							if (ret == kwType.end()) {
								throw FIDLException("Unexpected type name", ParseSeverity::Error);
							}

							methodDef.AddMember("name", o->name, alloc);
							if (subtype == FIDLType::UserDefined) {
								methodDef.AddMember("type", o->detail, alloc);
								
							}
							else {
								methodDef.AddMember("type", ret->first, alloc);
							}
							methodDef.AddMember("isarray", isarray, alloc);
							methodDef.AddMember("size", o->id, alloc);
							methodDef.AddMember("description", o->description, alloc);

							if (inout) {
								methodIn.PushBack(methodDef, alloc);
							}
							else {
								methodOut.PushBack(methodDef, alloc);
							}
						}
					}
					t.AddMember("in", methodIn, alloc);
					t.AddMember("out", methodOut, alloc);
					methodType.PushBack(t, alloc);
				}
				obj.AddMember("enumeration", enumType, alloc);
				obj.AddMember("struct", structType, alloc);
				obj.AddMember("method", methodType, alloc);
				if (intercollection) {
					interfaceNode.PushBack(obj, alloc);
				}
				else {
					typeCollectionNode.PushBack(obj, alloc);
				}
			}
			if (intercollection) {
				d.AddMember("interface", interfaceNode, alloc);
			}
			else {
				d.AddMember("typecollection", typeCollectionNode, alloc);
			}

		}
	}
	catch (const FIDLException& ex) {
		d.RemoveAllMembers();
		d.AddMember("package", mContextResult.package, alloc);
		rapidjson::Value node(rapidjson::kObjectType);
		node.AddMember("line", 0, alloc);
		node.AddMember("what", ex.getMessage(), alloc);
		errorNode.PushBack(node, alloc);
	}
	d.AddMember("error", errorNode, alloc);
	d.AddMember("warning", warningNode, alloc);
	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	return buffer.GetString();
}

bool FIDLParser::GetResultFile(const unsigned char* path, int length) {
	std::ofstream file(reinterpret_cast<const char*>(path));
	if (!file.is_open()) {
		return false;
	}
	file << GetResult();
	return true;
}

ParsingContext FIDLParser::Parsing(const std::string& target, int length, int line) {
	try {
		auto str = Preprocess(target);
		if (!str.has_value()) {
			return ParsingContext{ ParseSeverity::None, line };
		}

		LocalParsing(str.value());
	}
	catch (const FIDLException& e) {
		return ParsingContext{ e.getSeverity(), line, e.getOffset(), e.what()};
	}
	return ParsingContext{ ParseSeverity::None, line, 0, "H" };
}

void FIDLParser::Parsed() {
	mContextResult.interfaces.clear();
	mContextResult.typeCollections.clear();
	mContextResult.package.clear();
	std::swap(mContext, mContextResult);

	while (!mScopeStack.empty()) {
		mScopeStack.pop();
	}
	mScopeStack.push(Scope::Root);
	mNextScope = Scope::Root;
	isMultiLineComment = false;
	isInterface = false;
	mScopeLevel = 0;
}

std::optional<std::string> RoSatParser::FIDL::FIDLParser::Preprocess(const std::string& target) {
	std::string str = target;

	CommentCheck(str);
	trim(str);

	if (str.size() == 0) { return std::nullopt; }
	return str;
}

void RoSatParser::FIDL::FIDLParser::LocalParsing(const std::string& str) {
	if (str.size() == 0) { return; }
	size_t posBegin = str.find_first_of('{');
	size_t posEnd = str.find_first_of('}');
	int pos = min(posBegin, posEnd);
	std::string lstr = str.substr(0, pos);
	std::string rstr;
	rtrim(lstr);
	if (mScopeStack.empty()) {
		throw(FIDLException("Bracket count not match", ParseSeverity::Warn));
	}

	if (auto pos = mScopeMapping.find(mScopeStack.top()); pos != mScopeMapping.end()) {
		if (lstr.size() != 0) {
			//std::cout << mScopeLevel << "|" << (int) mScopeStack.top() << "->" << lstr << std::endl;
			auto scopeFunc = std::bind(pos->second, this, lstr);
			scopeFunc();
		}
	}

	if (pos == -1) { return; }
	if (pos < str.size()) {
		rstr = str.substr(pos + 1);
	}

	if (pos == str.find_first_of('{')) {
		++mScopeLevel;
		mScopeStack.push(mNextScope);
	} else {
		--mScopeLevel;
		mScopeStack.pop();
		if (mScopeStack.empty()) {
			throw(FIDLException("Scope stack error", ParseSeverity::Error));
		}
		mNextScope = mScopeStack.top();
	}
	ltrim(rstr);
	LocalParsing(rstr);
}

void RoSatParser::FIDL::FIDLParser::CommentCheck(std::string& str) {
	tempStr = "";
	std::string interval = "";
	std::string comment = "";
	if (isMultiLineComment == false) {
		// One line unstructured comment
		RE2::FullMatch(str, R"((.*?)(\/\/)(.*)\s*)", &str, &interval, &comment);
		if (!interval.empty()) {
			rtrim(str);
			return;
		}
		// One line structured comment
		RE2::FullMatch(str, R"((.*?)(<\*\*)(.*?)\*\*>\s*)", &str, &interval, &comment);
		if (!interval.empty()) {
			rtrim(str);
			ltrim(comment);
			mComment = comment;
			ParseComment();
			return;
		}
		// Multi line unstructured comment start
		RE2::FullMatch(str, R"((.*?)(\/\*)(.*)\s*)", &str, &interval, &comment);
		if (!interval.empty()) {
			rtrim(str);
			isMultiLineComment = true;
			return;
		}
		// Multi line structured comment start
		RE2::FullMatch(str, R"((.*?)(\<\*\*)(.*)\s*)", &str, &interval, &comment);
		if (!interval.empty()) {
			rtrim(str);
			ltrim(comment);
			mComment = comment;
			isMultiLineComment = true;
			return;
		}
	} else {
		// Multi line unstructured comment end
		RE2::FullMatch(str, R"((.*?)(\*\/)(.*)\s*)", &comment, &interval, &tempStr);
		if (!interval.empty()) {
			swap(tempStr, str);
			ltrim(str);
			isMultiLineComment = false;
			return;
		}

		// Multi line structured comment end
		RE2::FullMatch(str, R"((.*?)(\*\*\>)(.*)\s*)", &comment, &interval, &tempStr);
		if (!interval.empty()) {
			swap(tempStr, str);
			ltrim(str);
			rtrim(comment);
			mComment += comment;
			isMultiLineComment = false;
			ParseComment();
			return;
		}
		mComment += str;
		str.clear();
		return;
	}
}

void RoSatParser::FIDL::FIDLParser::ParseComment() {
	std::string tag = "";
	std::string value = "";
	re2::StringPiece input(mComment);
	while(RE2::FindAndConsume(&input, R"(@(.*?):([^@]*))", &tag,  &value)) {
		trim(tag);
		trim(value);
		if (tag == "description") {
			mTag.setDescription(value);
		} else if (tag == "details") {
			mTag.setDetails(value);
		} else {
			throw(FIDLException("Unsupported tag:" + tag, ParseSeverity::Warn));
		}
	}
	mComment.clear();
}

void RoSatParser::FIDL::FIDLParser::ScopeCheck(int level) {
	tempStr = "";
	if (mScopeLevel != level) { throw FIDLException("Scope Error", ParseSeverity::Error); }
}


void RoSatParser::FIDL::FIDLParser::AddElement(const std::string& str) {
	std::string name = "";
	std::string type = "";
	auto container = GetCurrentContainer();
	RE2::FullMatch(str, R"((version)\s*)", &tempStr);
	if (tempStr == "version") {
		mNextScope = Scope::Version;
		return;
	}

	// 1. Array
	RE2::FullMatch(str, R"(.*(array)\s*([a-zA-Z0-9]*)\s*of\s*([a-zA-Z0-9]*))", &tempStr, &name, &type);
	if (tempStr == "array") {
		auto it = kwType.find(type);
		if (it != kwType.end()) {
			Add(ToArrayType(it->second), name);
		}
		else if (auto t = container->findName(type); t != FIDLType::None) {
			Add(ToArrayType(t), name);
		}
		else {
			throw FIDLException("Unknown Type:" + type, ParseSeverity::Warn);
		}
		return;
	}

	// 2. Enumeration
	RE2::FullMatch(str, matchRegex("enumeration"), &tempStr, &name);
	if (tempStr == "enumeration") {
		enumCounter = 0;
		Add(FIDLType::Enumeration, name);
		mNextScope = Scope::Enumeration;
		return;
	}

	// 3. Struct
	RE2::FullMatch(str, matchRegex("struct"), &tempStr, &name);
	if (tempStr == "struct") {
		Add(FIDLType::Struct, name);
		AddExtend(str);
		mNextScope = Scope::Struct;
		return;
	}
	
	// 4. Method
	RE2::FullMatch(str, matchRegex("method"), &tempStr, &name);
	if (tempStr == "method") {
		if (!isInterface) { throw FIDLException("Method can't be in the typeCollection", ParseSeverity::Error); }
		auto container = GetCurrentContainer();
		const auto& [description, details] = mTag.getValue();
		container->methods.push_back(std::make_shared<FIDLMethod>(name, description, getID(details)));
		mNextScope = Scope::Method;
		return;
	}

	// 5. Union
	RE2::FullMatch(str, matchRegex("union"), &tempStr, &name);
	if (tempStr == "union") {
		Add(FIDLType::Union, name);
		AddExtend(str);
		mNextScope = Scope::Union;
		return;
	}

	// 6. Map
	RE2::FullMatch(str, matchRegex("map"), &tempStr, &name);
	if (tempStr == "map") {
		Add(FIDLType::Map, name);
		mNextScope = Scope::Map;
		return;
	}

	// 7. Typedef
	RE2::FullMatch(str, R"(.*(typedef)\s*([a-zA-Z0-9]*)\s*is\s*([a-zA-Z0-9]*))", &tempStr, &name, &type);
	if (tempStr == "typedef") {
		auto it = kwType.find(type);
		if (it != kwType.end()) {
			Add(it->second, name);
		}
		else if (auto t = container->findName(type); t != FIDLType::None) {
			Add(t, name);
		}
		else {
			throw FIDLException("Unknown Type:" + type, ParseSeverity::Warn);
		}
		return;
	}
}

void RoSatParser::FIDL::FIDLParser::Add(FIDLType type, const std::string& name) {
	auto container = GetCurrentContainer();
	if (container->findName(name) != FIDLType::None) {
		throw FIDLException("Parameter has the same name:" + name, ParseSeverity::Error);
	}
	auto& current = container->nodes;
	const auto& [description, details] = mTag.getValue();
	current.push_back(std::make_shared<FIDLNode>(type, "", name, description, getID(details)));
}

void RoSatParser::FIDL::FIDLParser::AddExtend(const std::string& str) {
	auto container = GetCurrentContainer();
	std::string name = "";
	RE2::FullMatch(str, R"(.*(extends)\s*([a-zA-Z0-9_]*).*)", &tempStr, &name);
	if (tempStr == "extends") {
		auto t = container->findNode(name);
		if (t != nullptr) {
			const auto& node = t->subnode;
			for (auto it = node.begin(); it != node.end(); ++it) {
				Append(it->get()->type, it->get()->detail, it->get()->name);
			}
		}
		else {
			throw FIDL::FIDLException("Can't find extends:" + name, ParseSeverity::Warn);
		}
	}
}

void RoSatParser::FIDL::FIDLParser::Append(const FIDLType& type, const std::string& detail, const std::string& name, FIDLMethodType method, int value) {
	auto container = GetCurrentContainer();
	const auto& [description, details] = mTag.getValue();
	if (method == FIDLMethodType::None) {
		auto& node = container->nodes.back()->subnode;
		int id = (value == -1) ? getSize(details) : value;
		node.push_back(std::make_shared<FIDLNode>
			(type, detail, name, description, id));
	}
	else if (method == FIDLMethodType::In) {
		container->methods.back()->in.push_back(std::make_shared<FIDLNode>
			(type, detail, name, description, getSize(details)));
	}
	else if (method == FIDLMethodType::Out) {
		container->methods.back()->out.push_back(std::make_shared<FIDLNode>
			(type, detail, name, description, getSize(details)));
	}
}


void RoSatParser::FIDL::FIDLParser::TypeCheck(const std::string& str, FIDLMethodType ismethod) {
	std::string parent = "";
	std::string type = "";
	std::string name = "";
	auto container = GetCurrentContainer();
	if (ismethod != FIDLMethodType::None && container->methods.empty()) { throw FIDLException("No method", ParseSeverity::Error); }

	RE2::FullMatch(str, R"((?:([a-zA-Z0-9_]*)\.)?([a-zA-Z0-9_]*)(\[\])?\s+([a-zA-Z0-9_]*)\s*)", &parent, &type, &tempStr, &name);
	if (name.empty()) {
		throw FIDLException("Unvalid method member name", ParseSeverity::Warn);
	}

	bool isarray = (tempStr == "[]");

	auto it = kwType.find(type);
	
	if (it != kwType.end()) {
		Append(ToArrayType(it->second, isarray), "", name, ismethod);
	} else if (auto t = container->findNode(type); t != nullptr) {
		Append(ToArrayType(FIDLType::UserDefined, isarray), t->name, name, ismethod);
	} else if (!parent.empty()) {
		auto& containers = (isInterface) ? (mContext.interfaces) : (mContext.typeCollections);
		auto it = std::find_if(containers.begin(), containers.end(), [&parent](const auto& x) { return x.name == parent; });
		if (it != containers.end()) {
			if (auto t = it->findNode(type); t != nullptr) {
				Append(ToArrayType(FIDLType::UserDefined, isarray), t->name, name, ismethod);
			}
		} else {
			throw FIDLException("Unknown Parent:" + parent, ParseSeverity::Error);
		}
	} else {
		throw FIDLException("Unknown Type:" + type, ParseSeverity::Warn);
	}
}

FIDLContainer* RoSatParser::FIDL::FIDLParser::GetCurrentContainer() {
	auto& containers = (isInterface) ? (mContext.interfaces) : (mContext.typeCollections);
	if (containers.empty()) {
		throw FIDLException("Empty Container", ParseSeverity::Error);
	}
	return &(containers.back());
}

/********************
   Static Method Definition
********************/
int RoSatParser::FIDL::FIDLParser::getID(const std::string& str) {
	std::string temp;
	std::string value;
	int id = -1;
	RE2::FullMatch(str, R"((id) *= *([a-fA-Fx0-9]*))", &temp, &value);
	try {
		if (value.find_first_of('x') != -1 || value.find_first_of('X') != -1) {
			id = std::stoul(value, nullptr, 16);
		} else {
			id = std::stoul(value, nullptr, 10);
		}
	}
	catch (...) {}
	return id;
}

int RoSatParser::FIDL::FIDLParser::getSize(const std::string& str) {
	std::string temp;
	int id = -1;
	RE2::FullMatch(str, R"((size) *= *(\d*))", &temp, &id);
	return id;
}


/********************
   Scope Definition
********************/
void RoSatParser::FIDL::FIDLParser::ScopeRoot(const std::string& str) {
	ScopeCheck(0);
	std::string name = "";
	RE2::FullMatch(str, R"((package)\s*(.*))", &tempStr,  &mContext.package);
	RE2::FullMatch(str, matchRegex("interface"), &tempStr, &name);
	if (tempStr == "interface") {
		isInterface = true;
		const auto& [description, details] = mTag.getValue();
		mContext.interfaces.push_back({ name, description, getID(details), 0, 0});
		mNextScope = Scope::Interface;
	}
	RE2::FullMatch(str, matchRegex("typeCollection"), &tempStr, &name);
	if (tempStr == "typeCollection") {
		isInterface = false;
		const auto& [description, details] = mTag.getValue();
		mContext.typeCollections.push_back({ name, description, getID(details), 0, 0});
		mNextScope = Scope::TypeCollection;
	}
}

void RoSatParser::FIDL::FIDLParser::ScopeTypeCollection(const std::string& str) {
	ScopeCheck(1);
	AddElement(str);
}

void RoSatParser::FIDL::FIDLParser::ScopeInterface(const std::string& str) {
	ScopeCheck(1);
	AddElement(str);
}

void RoSatParser::FIDL::FIDLParser::ScopeVersion(const std::string& str) {
	ScopeCheck(2);
	int major = -1;
	int minor = -1;
	RE2::FullMatch(str, R"((major)\s*(\d*)\s*minor\s*(\d*))", &tempStr, &major, &minor);
	if (tempStr != "major") { return; }
	auto container = GetCurrentContainer();
	container->major = major;
	container->minor = minor;
}

void RoSatParser::FIDL::FIDLParser::ScopeEnumeration(const std::string& str) {
	ScopeCheck(2);
	std::string value = "";
	int id;
	RE2::FullMatch(str, R"(([a-zA-Z0-9_]*),?\s*(?:=\s*([a-fA-Fx0-9]*))?,?)", &tempStr, &value); //([a-zA-Z0-9_]*),? *(?:= *([a-fA-Fx0-9]*))?
	if (tempStr.empty()) { return; }
	if (value.empty()) {
		id = enumCounter++;
	} else {
		try {
			if (value.find_first_of('x') != -1 || value.find_first_of('X') != -1) {
				id = std::stoul(value, nullptr, 16);
				enumCounter = id+1;
			} else {
				id = std::stoul(value, nullptr, 10);
				enumCounter = id+1;
			}
		}
		catch (...) {
			id = enumCounter++;
		}
	}

	Append(FIDL::FIDLType::EnumElement,"", tempStr, FIDLMethodType::None, id);
}

void RoSatParser::FIDL::FIDLParser::ScopeStruct(const std::string& str) {
	ScopeCheck(2);
	TypeCheck(str);
}

void RoSatParser::FIDL::FIDLParser::ScopeMethod(const std::string& str) {
	ScopeCheck(2);
	RE2::FullMatch(str, R"((in)\s*)", &tempStr);
	if (tempStr == "in") {
		mNextScope = Scope::MethodIn;
		return;
	}
	RE2::FullMatch(str, R"((out)\s*)", &tempStr);
	if (tempStr == "out") {
		mNextScope = Scope::MethodOut;
		return;
	}
	RE2::FullMatch(str, R"((error)\s*)", &tempStr);
	if (tempStr == "error") {
		mNextScope = Scope::MethodError;
		return;
	}
}

void RoSatParser::FIDL::FIDLParser::ScopeMethodIn(const std::string& str) {
	ScopeCheck(3);
	TypeCheck(str, FIDLMethodType::In);
}

void RoSatParser::FIDL::FIDLParser::ScopeMethodOut(const std::string& str) {
	ScopeCheck(3);
	TypeCheck(str, FIDLMethodType::Out);
}

void RoSatParser::FIDL::FIDLParser::ScopeMethodError(const std::string& str) {
	ScopeCheck(3);
	throw FIDLException("No Implementation of 'MethodError'.", ParseSeverity::Warn);
}

void RoSatParser::FIDL::FIDLParser::ScopeUnion(const std::string& str) {
	ScopeCheck(2);
	throw FIDLException("No Implementation of 'Union'.", ParseSeverity::Warn);
}

void RoSatParser::FIDL::FIDLParser::ScopeMap(const std::string& str) {
	ScopeCheck(2);
	throw FIDLException("No Implementation of 'Map'.", ParseSeverity::Warn);
}

void RoSatParser::FIDL::FIDLParser::ScopeBroadcast(const std::string& str) {
	ScopeCheck(2);
	RE2::FullMatch(str, R"((out)\s*)", &tempStr);
	if (tempStr == "out") {
		mNextScope = Scope::BroadcastOut;
		return;
	}
}

void RoSatParser::FIDL::FIDLParser::ScopeBroadcastOut(const std::string& str) {
	ScopeCheck(3);
	throw FIDLException("No Implementation of 'Broadcast'.", ParseSeverity::Warn);
}

void RoSatParser::FIDL::FIDLParser::ScopePSM(const std::string& str) {
	ScopeCheck(2);
	RE2::FullMatch(str, R"((state)\s*)", &tempStr);
	if (tempStr == "state") {
		mNextScope = Scope::PSMState;
		return;
	}
}

void RoSatParser::FIDL::FIDLParser::ScopePSMState(const std::string& str) {
	ScopeCheck(3);
	throw FIDLException("No Implementation of 'PSM'.", ParseSeverity::Warn);
}
