#pragma once

#ifndef FIDL_PARSER_TYPES_H_
#define FIDL_PARSER_TYPES_H_
#include <string>
#include <memory>
#include <vector>

#define ARRAYMASK 0x80
#define TYPEMASK 0x7F


namespace RoSatParser {
	namespace FIDL {

		using FIDLEnumeration = std::vector<std::string>;

		enum class FIDLType : uint8_t {
			None = 0x00, UInt8 = 0x01, Int8 = 0x02, UInt16 = 0x03,
			Int16 = 0x04, UInt32 = 0x05, Int32 = 0x06, UInt64 = 0x07,
			Int64 = 0x08, Integer = 0x09, Boolean = 0x0A, Float = 0x0B,
			Double = 0x0C, String = 0x0D, ByteBuffer = 0x0E, Enumeration = 0x0F,
			Struct = 0x10, Method = 0x11, Union = 0x12, Map = 0x13,
			EnumElement = 0x14, UserDefined = 0x15, Array = 0x80
		};

		static FIDLType ToArrayType(FIDLType type, bool isarray = true) {
			if (!isarray) { return static_cast<FIDLType>(static_cast<uint8_t>(type) & TYPEMASK); }
			return static_cast<FIDLType>(static_cast<uint8_t>(type) | ARRAYMASK);
		}

		static FIDLType GetBaseType(FIDLType type) {
			return static_cast<FIDLType>(static_cast<uint8_t>(type) & TYPEMASK);
		}
		static bool IsArrayType(FIDLType type) {
			return (static_cast<uint8_t>(type) & ARRAYMASK) == ARRAYMASK;
		}

		enum class FIDLMethodType {
			None, In, Out
		};

		class FIDLNode {
		public:
			FIDLNode() = default;
			FIDLNode(FIDLType type, const std::string& detail, const std::string& name, const std::string& description, int id)
				: type(type), detail(detail), name(name), description(description), id(id), subnode() {}
			~FIDLNode() = default;
			
			
			FIDLType getType() const;
			std::string getDetail() const;
			std::string getName() const;
			
		private:
			std::vector<std::shared_ptr<FIDLNode>> subnode;
			FIDLType type;
			std::string detail;
			std::string name;
			std::string description;
			int id;
			friend class FIDLParser;
		};

		class FIDLMethod {
		public:
			FIDLMethod() = default;
			FIDLMethod(const std::string& name, const std::string& description, int id) 
				: name(name), description(description), id(id) {}
			~FIDLMethod() = default;
		private:
			std::string name;
			std::string description;
			int id;
			std::vector<std::shared_ptr<FIDLNode>> in;
			std::vector<std::shared_ptr<FIDLNode>> out;
			friend class FIDLParser;
		};

		struct FIDLContainer {
			std::string name;
			std::string description;
			int id;
			int major;
			int minor;
			std::vector<std::shared_ptr<FIDLNode>> nodes;
			std::vector< std::shared_ptr<FIDLMethod>> methods;

			FIDL::FIDLType findName(const std::string& name) const;
			FIDLNode* findNode(const std::string& name) const;
		};

		struct FIDLContext {
			std::string package;
			std::vector<FIDLContainer> interfaces;
			std::vector<FIDLContainer> typeCollections;
		};

		class FIDLTag {
		public:
			[[nodiscard]] std::tuple<std::string, std::string> getValue();
			void setDescription(const std::string& str);
			void setDetails(const std::string& str);
		private:
			bool valid;
			std::string description;
			std::string details;
		};
	}
}



#endif
