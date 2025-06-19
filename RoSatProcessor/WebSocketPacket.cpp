#include "pch.h"
#include "WebSocketPacket.h"
#include "QueryPacket.h"
#include <type_traits>
#include <filesystem>

#define RAPIDJSON_NOMEMBERITERATORCLASS 
#include <rapidjson/document.h>
#include <rapidjson/writer.h>
#include <rapidjson/stringbuffer.h>

#include "base64.h"

#pragma region CreatePacket

uint64_t RoSatProcessor::WebSocketPacket::id = 0;

#pragma region AddMember
template<typename T, typename Enable = void>
struct AddMemberHelper {
	static void Add(rapidjson::Document& d, const char* key, const T& value, rapidjson::Document::AllocatorType& allocator)
	{
		static_assert(false, "Unsupported type");
	}
};

template<typename T>
struct AddMemberHelper<T, typename std::enable_if_t<std::is_integral_v<T>>> {
	static void Add(rapidjson::Document& d, const char* key, T value, rapidjson::Document::AllocatorType& allocator)
	{
		d.AddMember(rapidjson::StringRef(key), value, allocator);
	}
};

template<typename T>
struct AddMemberHelper<T, typename std::enable_if_t<std::is_floating_point_v<T>>> {
	static void Add(rapidjson::Document& d, const char* key, T value, rapidjson::Document::AllocatorType& allocator)
	{
		d.AddMember(rapidjson::StringRef(key), value, allocator);
	}
};

template<typename T>
struct AddMemberHelper<T, typename std::enable_if_t<std::is_same_v<std::decay_t<T>,std::string>>> {
	static void Add(rapidjson::Document& d, const char* key, const T& value, rapidjson::Document::AllocatorType& allocator)
	{
		if (value.empty()) {
			d.AddMember(rapidjson::StringRef(key), rapidjson::Value(rapidjson::kNullType), allocator);
		} else {
			d.AddMember(rapidjson::StringRef(key), rapidjson::StringRef(value.c_str()), allocator);
		}
	}
};

template<typename T>
struct AddMemberHelper<T, typename std::enable_if_t<std::is_same_v<std::decay_t<T>, const char*> || std::is_same_v<std::decay_t<T>, char*>>> {
	static void Add(rapidjson::Document& d, const char* key, const T value, rapidjson::Document::AllocatorType& allocator)
	{
		if (value != NULL && value[0] != '\0') {
			d.AddMember(rapidjson::StringRef(key), rapidjson::StringRef(value), allocator);
		} else {
			d.AddMember(rapidjson::StringRef(key), rapidjson::Value(rapidjson::kNullType), allocator);
		}
	}
};

template<typename T>
struct AddMemberHelper<T, typename std::enable_if_t<std::is_same_v<std::decay_t<T>, std::vector<uint8_t>>>> {
	static void Add(rapidjson::Document& d, const char* key, const T& value, rapidjson::Document::AllocatorType& allocator)
	{
		if (value.empty()) {
			d.AddMember(rapidjson::StringRef(key), rapidjson::Value(rapidjson::kNullType), allocator);
			return;
		}

		rapidjson::Value p(rapidjson::kArrayType);
		for (auto& i : value) {
			p.PushBack(i, allocator);
		}
		d.AddMember(rapidjson::StringRef(key), p, allocator);
	}
};

template<typename T>
struct AddMemberHelper<T, typename std::enable_if_t<std::is_same_v<std::decay_t<T>, std::array<uint8_t, 16>> || std::is_same_v<std::decay_t<T>, std::array<uint8_t, 32>>>> {
	static void Add(rapidjson::Document& d, const char* key, const T& value, rapidjson::Document::AllocatorType& allocator)
	{
		if (value.empty()) {
			d.AddMember(rapidjson::StringRef(key), rapidjson::Value(rapidjson::kNullType), allocator);
			return;
		}

		rapidjson::Value p(rapidjson::kArrayType);
		for (auto& i : value) {
			p.PushBack(i, allocator);
		}
		d.AddMember(rapidjson::StringRef(key), p, allocator);
	}
};

template <typename T>
void AddMember(rapidjson::Document& d, const char* key, const T& value, rapidjson::Document::AllocatorType& allocator, bool(*f)(const T&) = nullptr) {
	if (f == nullptr || f(value)) AddMemberHelper<T>::Add(d, key, value, allocator);
}
#pragma endregion


static bool StringNotEmptyValidation(const std::string& str) { return !str.empty(); }

RoSatProcessor::WebSocketPacket::WebSocketPacket() : m_id(0), m_packet(""), m_priority(WebSocketPriority::Normal), m_query_id({}) {

}

RoSatProcessor::WebSocketPacket::WebSocketPacket(uint64_t id, const std::string& packet, WebSocketPriority priority)
	: m_id(id), m_packet(packet), m_priority(priority), m_query_id({}) {
}


std::array<uint8_t, 16> RoSatProcessor::WebSocketPacket::QueryId() const
{
	return m_query_id;
}

uint64_t RoSatProcessor::WebSocketPacket::Id() const
{
	return m_id;
}

std::string RoSatProcessor::WebSocketPacket::Get() const
{
	return m_packet;
}

RoSatProcessor::DataFrame RoSatProcessor::WebSocketPacket::Serialize() const
{
	return WebSocketPacket::Serialize(*this);
}

RoSatProcessor::DataFrame RoSatProcessor::WebSocketPacket::Serialize(const WebSocketPacket& packet)
{
	DataFrame frame;
	frame << packet.m_query_id << packet.m_id << packet.m_packet;
	return frame;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::Deserialize(const DataFrame& frame)
{
	WebSocketPacket packet;
	frame >> packet.m_query_id >> packet.m_id >> packet.m_packet;
	return packet;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateCPCommand(const CommandCpPacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	AddMember(d, "cmdId", packet.CommandId, allocator);						// required
	AddMember(d, "cmdType", packet.Gateway, allocator);						// required
	AddMember(d, "id", id, allocator);									// required
	AddMember(d, "moduleMac", packet.ModuleMac, allocator);					// required
	AddMember(d, "noProgressTimeout", packet.NoProgressTimeout, allocator);	// nullable
	AddMember(d, "readTimeout", packet.ReadTimeout, allocator);				// nullable
	AddMember(d, "satId", packet.SatelliteId, allocator);					// required
	AddMember(d, "tripType", 1, allocator);									// required
	AddMember(d, "type", "CPCommand", allocator);							// required
	AddMember(d, "writeTimeout", packet.WriteTimeout, allocator);			// nullable

	AddMember(d, "payload", packet.Payload, allocator); 					// nullable

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateCPCommandOneWay(const CommandCpPacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	AddMember(d, "cmdId", packet.CommandId, allocator);						// required
	AddMember(d, "cmdType", packet.Gateway, allocator);						// required
	AddMember(d, "id", id, allocator);									// required
	AddMember(d, "moduleMac", packet.ModuleMac, allocator);					// required
	AddMember(d, "noProgressTimeout", packet.NoProgressTimeout, allocator); // nullable
	AddMember(d, "payload", packet.Payload, allocator); 					// nullable
	AddMember(d, "satId", packet.SatelliteId, allocator);					// required
	AddMember(d, "tripType", 1, allocator);									// required
	AddMember(d, "type", "CPCommandOneWay", allocator);						// required
	AddMember(d, "writeTimeout", packet.WriteTimeout, allocator);			// nullable	

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateFWUpd(const FirmwareUpdatePacket& packet)
{
	
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	std::string path = packet.FilePath;
	std::string filename = std::filesystem::path(path).filename().string();

	AddMember(d, "boardRevision", packet.BoardRevision, allocator);			// ########
	AddMember(d, "cmdId", packet.CommandId, allocator);						// required
	AddMember(d, "cpuType", packet.CpuType, allocator);						// ########
	AddMember(d, "fileName", filename, allocator);							// ########
	AddMember(d, "flags", packet.Flags, allocator);							// ########
	AddMember(d, "fwType", packet.FWType, allocator);						// ########
	AddMember(d, "fwVerMaj", packet.FWVerMaj, allocator);					// ########	
	AddMember(d, "fwVerMin", packet.FWVerMin, allocator);					// ########
	AddMember(d, "id", id, allocator);										// required
	AddMember(d, "moduleConfig", packet.ModuleConfig, allocator);			// ########
	AddMember(d, "moduleMac", packet.ModuleMac, allocator);					// required
	AddMember(d, "moduleType", packet.ModuleType, allocator);				// required
	AddMember(d, "noProgressTimeout", "5s", allocator);						// ########
	AddMember(d, "readTimeout", "999s", allocator);							// nullable
	AddMember(d, "satId", packet.SatelliteId, allocator);					// required
	AddMember(d, "subModule", packet.Submodule, allocator);					// required
	AddMember(d, "type", "FWUpd", allocator);								// required
	AddMember(d, "writeTimeout", "999s", allocator);						// nullable
	
	std::ifstream file(path, std::ios::binary);
	if (!file)
	{
		AddMember(d, "payload", "", allocator);
	}
	else
	{
		std::string file_contents((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
		std::string encoded = base64_encode(file_contents, false);

		rapidjson::Value k("payload", allocator);
		rapidjson::Value v(encoded.c_str(), static_cast<rapidjson::SizeType>(encoded.length()), allocator);
		d.AddMember(k, v, allocator);
	}

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateFWUpdBundle(const FirmwareUpdatePacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	std::string path = packet.FilePath;
	std::string filename = std::filesystem::path(path).filename().string();
	
	AddMember(d, "cmdId", packet.CommandId, allocator);				// required
	AddMember(d, "fileName", filename, allocator);					// ########	
	AddMember(d, "id", id, allocator);								// required
	AddMember(d, "moduleMac", packet.ModuleMac, allocator);			// required
	AddMember(d, "noProgressTimeout", "5s", allocator);				// ########
	AddMember(d, "readTimeout", "999s", allocator);					// nullable
	AddMember(d, "satId", packet.SatelliteId, allocator);			// required
	AddMember(d, "type", "FWUpdBundle", allocator);					// required
	AddMember(d, "writeTimeout", "999s", allocator);				// nullable

	std::ifstream file(path, std::ios::binary);
	if (!file)
	{
		AddMember(d, "payload", "", allocator);
	}
	else
	{
		std::string file_contents((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
		std::string encoded = base64_encode(file_contents, false);

		rapidjson::Value k("payload", allocator);
		rapidjson::Value v(encoded.c_str(), static_cast<rapidjson::SizeType>(encoded.length()), allocator);
		d.AddMember(k, v, allocator);
	}

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateFilePacket(const FirmwareUpdatePacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	std::string path = packet.FilePath;
	std::string filename = std::filesystem::path(path).filename().string();

	AddMember(d, "boardRevision", packet.BoardRevision, allocator);			// ########
	AddMember(d, "cmdId", packet.CommandId, allocator);						// required
	AddMember(d, "cpuType", packet.CpuType, allocator);						// ########
	AddMember(d, "fileName", filename, allocator);							// ########
	AddMember(d, "flags", packet.Flags, allocator);							// ########
	AddMember(d, "fwType", packet.FWType, allocator);						// ########
	AddMember(d, "fwVerMaj", packet.FWVerMaj, allocator);					// ########
	AddMember(d, "fwVerMin", packet.FWVerMin, allocator);					// ########
	AddMember(d, "id", id, allocator);										// required
	AddMember(d, "moduleConfig", packet.ModuleConfig, allocator);			// ########
	AddMember(d, "moduleMac", packet.ModuleMac, allocator);					// required
	AddMember(d, "moduleType", packet.ModuleType, allocator);				// required
	AddMember(d, "noProgressTimeout", "5s", allocator);						// ########
	AddMember(d, "readTimeout", "999s", allocator);							// nullable
	AddMember(d, "satId", packet.SatelliteId, allocator);					// required
	AddMember(d, "subModule", packet.Submodule, allocator);					// required
	AddMember(d, "type", "File", allocator);								// required
	AddMember(d, "writeTimeout", "999s", allocator);						// nullable

	std::ifstream file(path, std::ios::binary);
	if (!file)
	{
		AddMember(d, "payload", "", allocator);
	}
	else
	{
		std::string file_contents((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
		std::string encoded = base64_encode(file_contents, false);

		rapidjson::Value k("payload", allocator);
		rapidjson::Value v(encoded.c_str(), static_cast<rapidjson::SizeType>(encoded.length()), allocator);
		d.AddMember(k, v, allocator);
	}
	
	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateInitRadio(const CommandRadioPacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	auto aesIV = base64_encode(packet.AES.IV.data(), 16, false);
	auto aesKey = base64_encode(packet.AES.Key.data(), 32, false);

	AddMember(d, "aesIV", aesIV, allocator);								// nullable
	AddMember(d, "aesKey", aesKey, allocator);								// nullable
	AddMember(d, "downlinkFrequency", packet.DownlinkFrequency, allocator); // ########
	AddMember(d, "encrypted", packet.Encrypted, allocator);					// ########
	AddMember(d, "id", id, allocator);										// required
	AddMember(d, "rfConfig", packet.RFConfig, allocator);					// ########
	AddMember(d, "type", "InitRadio", allocator);							// required
	AddMember(d, "uplinkFrequency", packet.UplinkFrequency, allocator);		// ########

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateUpdateRadio(const CommandRadioPacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	//AddMember(d, "aesIV", packet.AES.IV, allocator);						// nullable
	//AddMember(d, "aesKey", packet.AES.Key, allocator);					// nullable
	AddMember(d, "downlinkFrequency", packet.DownlinkFrequency, allocator); // ########
	//AddMember(d, "encrypted", packet.Encrypted, allocator);				// ########
	AddMember(d, "id", id, allocator);										// required
	AddMember(d, "rfConfig", packet.RFConfig, allocator);					// ########
	AddMember(d, "type", "UpdateRadio", allocator);							// required
	AddMember(d, "uplinkFrequency", packet.UplinkFrequency, allocator);		// ########

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateUpdateAESKey(const CommandRadioPacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();
	auto aesIV = base64_encode(packet.AES.IV.data(), 16, false);
	auto aesKey = base64_encode(packet.AES.Key.data(), 32, false);

	AddMember(d, "aesIV", aesIV, allocator);								// nullable
	AddMember(d, "aesKey", aesKey, allocator);								// nullable
	//AddMember(d, "downlinkFrequency", packet.DownlinkFrequency, allocator); // ########
	AddMember(d, "encrypted", packet.Encrypted, allocator);					// ########
	AddMember(d, "id", id, allocator);										// required
	//AddMember(d, "rfConfig", packet.RFConfig, allocator);					// ########
	AddMember(d, "type", "UpdateAESKey", allocator);						// required
	//AddMember(d, "uplinkFrequency", packet.UplinkFrequency, allocator);	// ########

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateRadioConn(const CommandRadioPacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	AddMember(d, "id", id, allocator);										// required
	AddMember(d, "remoteRadioMac", packet.RemoteRadioMac, allocator);		// required
	AddMember(d, "type", "RadioConn", allocator);							// required

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

	
RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateRotatorSetPosition(const CommandRotatorPacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	AddMember(d, "azimuth", packet.Azimuth, allocator);						// required
	AddMember(d, "elevation", packet.Elevation, allocator);					// required
	AddMember(d, "id", id, allocator);										// required
	AddMember(d, "type", "RotatorSetPosition", allocator);					// required

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateBeaconListen(const CommandBeaconPacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	AddMember(d, "duration", packet.Duration, allocator);					// ########
	AddMember(d, "id", id, allocator);										// required
	AddMember(d, "type", "BeaconListen", allocator);						// required
	
	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

RoSatProcessor::WebSocketPacket RoSatProcessor::WebSocketPacket::CreateCancel(const CommandStatePacket& packet)
{
	WebSocketPacket ret;
	rapidjson::Document d;
	d.SetObject();

	rapidjson::Document::AllocatorType& allocator = d.GetAllocator();

	AddMember(d, "id", id, allocator);										// required
	AddMember(d, "type", "Cancel", allocator);								// required

	ret.m_query_id = packet.QueryId;
	ret.m_id = id++;

	rapidjson::StringBuffer buffer;
	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	d.Accept(writer);

	ret.m_packet = buffer.GetString();
	return ret;
}

std::string RoSatProcessor::WebSocketPacket::GetType(std::string_view packet)
{
	rapidjson::Document d;
	rapidjson::ParseResult r = d.Parse(packet.data());
	if (!r) { throw std::exception("Invalid json packet"); }

	if (!d.HasMember("type") || !d["type"].IsString()) {
		throw std::exception("Invalid received packet type");
	}

	return d["type"].GetString();
}

uint64_t RoSatProcessor::WebSocketPacket::GetRequestId(std::string_view packet)
{
	rapidjson::Document d;
	rapidjson::ParseResult r = d.Parse(packet.data());
	if (!r) { throw std::exception("Invalid json packet"); }

	if (!d.HasMember("requestId") || !d["requestId"].IsInt()) {
		throw std::exception("Invalid received packet requestId");
	}

	return d["requestId"].GetInt();
}

#pragma endregion

#pragma region ParsePacket
RoSatProcessor::CommandCpResultPacket RoSatProcessor::WebSocketPacket::ParseCpResult(std::string_view packet)
{
	CommandCpResultPacket p;
	rapidjson::Document d;
	rapidjson::ParseResult r = d.Parse(packet.data());
	if (!r) { throw std::exception("Invalid json packet"); }
	
	if (d.HasMember("cmdId") && d["cmdId"].IsInt()) {
		p.CommandId = d["cmdId"].GetInt();
	} else {
		throw std::exception("Invalid cmdId");
	}

	if (d.HasMember("payload")) {
		if (d["payload"].IsString()) {
			auto str = d["payload"].GetString();
			p.Payload = base64_decode_vector(str);

			/*
			for (const auto& i : d["payload"].GetArray()) {
				if (i.IsInt()) {
					p.Payload.push_back(i.GetInt());
				} else {
					throw std::exception("Invalid payload element");
				}
			}*/
		} else if (d["payload"].IsNull()) {
			
		} else {
			throw std::exception("Invalid payload");
		}
	}

	return p;
}

RoSatProcessor::FirmwareUpdateResultPacket RoSatProcessor::WebSocketPacket::ParseFwUpdateResult(std::string_view packet)
{
	FirmwareUpdateResultPacket ret;
	rapidjson::Document d;
	rapidjson::ParseResult r = d.Parse(packet.data());
	if (!r) { throw std::exception("Invalid json packet"); }

	if (d.HasMember("cmdId") && d["cmdId"].IsInt()) {
		ret.CommandId = d["cmdId"].GetInt();
	}
	else {
		throw std::exception("Invalid cmdId");
	}

	return ret;
}

RoSatProcessor::CommandRadioResultPacket RoSatProcessor::WebSocketPacket::ParseRadioResult(std::string_view packet)
{
	CommandRadioResultPacket p;
	rapidjson::Document d;
	rapidjson::ParseResult r = d.Parse(packet.data());
	if (!r) { throw std::exception("Invalid json packet"); }

	if (d.HasMember("aesIV") && d["aesIV"].IsString()) {
		auto vec = base64_decode_vector(d["aesIV"].GetString());
		if (vec.size() != 16) throw std::exception("Invalid aesIV size");
		std::copy(vec.begin(), vec.begin() + 16, p.AES.IV.begin());
	}
	if (d.HasMember("aesKey") && d["aesKey"].IsString()) {
		auto vec = base64_decode_vector(d["aesKey"].GetString());
		if (vec.size() != 32) throw std::exception("Invalid aesKey size");
		std::copy(vec.begin(), vec.begin() + 16, p.AES.Key.begin());
	}
	if (d.HasMember("uplinkFrequency") && d["uplinkFrequency"].IsInt()) {
		p.UplinkFrequency = d["uplinkFrequency"].GetInt();
	}
	if (d.HasMember("downlinkFrequency") && d["downlinkFrequency"].IsInt()) {
		p.DownlinkFrequency = d["downlinkFrequency"].GetInt();
	}
	if (d.HasMember("encrypted") && d["encrypted"].IsBool()) {
		p.Encrypted = d["encrypted"].GetBool();
	}
	if (d.HasMember("rfConfig") && d["rfConfig"].IsInt()) {
		p.RFConfig = d["rfConfig"].GetInt();
	}

	return p;
}


RoSatProcessor::CommandRotatorResultPacket RoSatProcessor::WebSocketPacket::ParseRotatorResult(std::string_view packet)
{
	CommandRotatorResultPacket ret;
	return ret;
}

RoSatProcessor::CommandBeaconPacket RoSatProcessor::WebSocketPacket::ParseBeaconResult(std::string_view packet)
{
	CommandBeaconPacket ret;
	return ret;
}

RoSatProcessor::CommandStatePacket RoSatProcessor::WebSocketPacket::ParseStateResult(std::string_view packet)
{
	CommandStatePacket p;
	rapidjson::Document d;
	rapidjson::ParseResult r = d.Parse(packet.data());
	if (!r) { throw std::exception("Invalid json packet"); }

	if (d.HasMember("error") && d["error"].IsString()) {
		p.Error = d["error"].GetString();
	}

	if (d.HasMember("info") && d["info"].IsString()) {
		p.Info = d["info"].GetString();
	}

	if (d.HasMember("notifier") && d["notifier"].IsInt()) {
		p.Notifier = d["notifier"].GetInt();
	}

	if (d.HasMember("data")) {
		if (d["data"].IsString()) {
			auto str = d["data"].GetString();
			p.Data = base64_decode_vector(str);
		}
		else if (d["data"].IsNull()) {

		} else {
			throw std::exception("Invalid payload");
		}
	}

	return p;
}
bool RoSatProcessor::WebSocketPacket::operator<(const WebSocketPacket& rhs) const
{
	return this->m_priority < rhs.m_priority;
}
bool RoSatProcessor::WebSocketPacket::operator==(const WebSocketPacket& other) const
{
	return this->m_id == other.m_id;
}
#pragma endregion