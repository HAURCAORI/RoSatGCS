#pragma once
#include <msgpack.hpp>
#include <string>
#include <vector>
#include "DataFrame.h"

#pragma region Enumeration
enum class QueryType {
    None,
    ACK,
    Error,
    Command,
    Radio,
    Data,
    Schedule,
    Service,
    Config,
    Debug,
    FwUpdate,
    Cancel
};
MSGPACK_ADD_ENUM(QueryType);

enum class DispatcherType {
    NoResponse,
    ImmediateResponse,
    Postpone,
    FileTransfer
};
MSGPACK_ADD_ENUM(DispatcherType);

enum class DataType {
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float,
    Double,
    String,
    ByteArray,
    Boolean
};
MSGPACK_ADD_ENUM(DataType);

#pragma endregion

namespace RoSatProcessor {
    template<typename T>
    struct PacketBase abstract {
        DataFrame static SerializePacket(const T& packet) {
            msgpack::sbuffer sbuf;
            msgpack::packer<msgpack::sbuffer> packer(sbuf);
            packer.pack(packet);
            return DataFrame(sbuf.data(), sbuf.size());
        }

        T static DeserializePacket(const DataFrame& data) {
            msgpack::object_handle oh = msgpack::unpack(reinterpret_cast<const char*>(data.data()), data.size());
            msgpack::object obj = oh.get();
            T packet{};
            obj.convert(packet);
            return packet;
        }

		T static DeserializePacket(const std::vector<uint8_t>& data) {
			return DeserializePacket(DataFrame(data));
		}
    };

    struct QueryPacket : PacketBase<QueryPacket> {
        std::array<uint8_t, 16> Id = {};
        std::string Name = "";
        QueryType Type = QueryType::None;
        DispatcherType Dispatcher = DispatcherType::NoResponse;
        std::vector<uint8_t> Payload = {};

        QueryPacket() = default;
        QueryPacket(const std::array<uint8_t, 16>& id, const std::string& name, QueryType type, DispatcherType dispatcher, const std::vector<uint8_t>& payload = {})
            : Id(id), Name(name), Type(type), Dispatcher(dispatcher), Payload(payload) {}

        MSGPACK_DEFINE(Id, Name, Type, Dispatcher, Payload);
    };

    // Error Packet
    struct ErrorPacket : PacketBase<ErrorPacket> {
        std::array<uint8_t, 16> QueryId = {};
        std::uint32_t Code = 0;
        std::string Error = "";

        ErrorPacket() = default;
        ErrorPacket(uint32_t code, const std::string& error) : Code(code), Error(error) {}

        MSGPACK_DEFINE(Code, Error);
    };

	struct ProcessorConfigPacket : PacketBase<ProcessorConfigPacket> {
        std::array<uint8_t, 16> QueryId = {};
		std::string IP = "";
		int Port = 0;
		bool TLS = false;

		ProcessorConfigPacket() = default;
		ProcessorConfigPacket(const std::string& ip, int port, bool tls) : IP(ip), Port(port), TLS(tls) {}
		MSGPACK_DEFINE(IP, Port, TLS);
	};

    struct ProcessorDebugPacket : PacketBase<ProcessorDebugPacket> {
		std::array<uint8_t, 16> QueryId = {};
		bool Debug = false;
		ProcessorDebugPacket() = default;
		ProcessorDebugPacket(bool debug) : Debug(debug) {}
		MSGPACK_DEFINE(Debug);
    };

    struct CancelPacket : PacketBase<CancelPacket> {
        std::array<uint8_t, 16> QueryId = {};
        uint64_t CommandId = 0;
        CancelPacket() = default;
        CancelPacket(uint64_t commandId) : CommandId(commandId) {}
        MSGPACK_DEFINE(CommandId);
	};


#pragma region Command Packet
    struct CommandCpPacket : PacketBase<CommandCpPacket> {
        std::array<uint8_t, 16> QueryId = {};
        uint8_t ModuleMac = 0;
        uint16_t Gateway = 0; // cmdType
        uint64_t CommandId = 0;
        std::string SatelliteId = "";
        std::string NoProgressTimeout = "";
        std::string ReadTimeout = "";
        std::string WriteTimeout = "";
        std::vector<uint8_t> Payload = {};

        CommandCpPacket() = default;
        CommandCpPacket(uint8_t moduleMac, uint16_t gateway, uint64_t commandId, const std::string& satelliteId,
            const std::string& noProgressTimeout = "", const std::string& readTimeout = "", const std::string& writeTimeout = "", const std::vector<uint8_t>& payload = {})
			: ModuleMac(moduleMac), Gateway(gateway), CommandId(commandId), SatelliteId(satelliteId),
            NoProgressTimeout(noProgressTimeout), ReadTimeout(readTimeout), WriteTimeout(writeTimeout), Payload(payload) {}

		MSGPACK_DEFINE(ModuleMac, Gateway, CommandId, SatelliteId, NoProgressTimeout, ReadTimeout, WriteTimeout, Payload);
    };

    struct CommandCpResultPacket : PacketBase<CommandCpResultPacket> {
        std::array<uint8_t, 16> QueryId = {};
        uint64_t CommandId = 0;
        std::vector<uint8_t> Payload = {};

        CommandCpResultPacket() = default;
        CommandCpResultPacket(uint64_t commandId, const std::vector<uint8_t>& payload = {})
			: CommandId(commandId), Payload(payload) {}

		MSGPACK_DEFINE(CommandId, Payload);
    };
    
	struct CommandAesPacket : PacketBase<CommandAesPacket> {
        std::array<uint8_t, 16> QueryId = {};
		std::array<uint8_t, 16> IV = {};
        std::array<uint8_t, 32> Key = {};

        CommandAesPacket() = default;
        CommandAesPacket(const std::array<uint8_t, 16>& iv, const std::array<uint8_t, 32>& key)
			: IV(iv), Key(key) {}

		MSGPACK_DEFINE(IV, Key);
	};

	struct CommandRadioPacket : PacketBase<CommandRadioPacket> {
        std::array<uint8_t, 16> QueryId = {};
        CommandAesPacket AES = {};
        uint32_t DownlinkFrequency = 0;
		uint32_t UplinkFrequency = 0;
        bool Encrypted = false;
        uint32_t RFConfig = 0;
        uint8_t RemoteRadioMac = 0;

        CommandRadioPacket() = default;

        CommandRadioPacket(const CommandAesPacket& aes, uint32_t downlinkFrequency, uint32_t uplinkFrequency, bool encrypted, uint32_t rfConfig, uint8_t remoteRadioMac)
			: AES(aes), DownlinkFrequency(downlinkFrequency), UplinkFrequency(uplinkFrequency), Encrypted(encrypted), RFConfig(rfConfig), RemoteRadioMac(remoteRadioMac) {}

		MSGPACK_DEFINE(AES, DownlinkFrequency, UplinkFrequency, Encrypted, RFConfig, RemoteRadioMac);
	};

	struct CommandRadioResultPacket : PacketBase<CommandRadioResultPacket> {
        std::array<uint8_t, 16> QueryId = {};
        CommandAesPacket AES = {};
        uint32_t DownlinkFrequency = 0;
        uint32_t UplinkFrequency = 0;
        bool Encrypted = false;
        uint32_t RFConfig = 0;
        uint8_t RemoteRadioMac = 0;

        CommandRadioResultPacket() = default;
        CommandRadioResultPacket(const CommandAesPacket& aes, uint32_t downlinkFrequency, uint32_t uplinkFrequency, bool encrypted, uint32_t rfConfig, uint8_t remoteRadioMac)
			: AES(aes), DownlinkFrequency(downlinkFrequency), UplinkFrequency(uplinkFrequency), Encrypted(encrypted), RFConfig(rfConfig), RemoteRadioMac(remoteRadioMac) {
		}

		MSGPACK_DEFINE(AES, DownlinkFrequency, UplinkFrequency, Encrypted, RFConfig, RemoteRadioMac);
	};

	struct CommandRotatorPacket : PacketBase<CommandRotatorPacket> {
        std::array<uint8_t, 16> QueryId = {};
		uint32_t Azimuth = 0;
		uint32_t Elevation = 0;

        CommandRotatorPacket() = default;
        CommandRotatorPacket(uint32_t azimuth, uint32_t elevation)
			: Azimuth(azimuth), Elevation(elevation) {}

		MSGPACK_DEFINE(Azimuth, Elevation);
	};

    struct CommandRotatorResultPacket : PacketBase<CommandRotatorResultPacket> {
        std::array<uint8_t, 16> QueryId = {};
        uint32_t Azimuth = 0;
        uint32_t Elevation = 0;

        CommandRotatorResultPacket() = default;
        CommandRotatorResultPacket(uint32_t azimuth, uint32_t elevation)
            : Azimuth(azimuth), Elevation(elevation) {
        }

		MSGPACK_DEFINE(Azimuth, Elevation);
    };

	struct CommandBeaconPacket : PacketBase<CommandBeaconPacket> {
        std::array<uint8_t, 16> QueryId = {};
        std::string Duration = "";
        std::vector<uint8_t> AX25Frame = {};

        CommandBeaconPacket() = default;
        CommandBeaconPacket(const std::string& duration, const std::vector<uint8_t>& ax25Frame)
			: Duration(duration), AX25Frame(ax25Frame) {}

		MSGPACK_DEFINE(Duration, AX25Frame);
	};

	struct CommandStatePacket : PacketBase<CommandStatePacket> {
        std::array<uint8_t, 16> QueryId = {};
        std::string Error = "";
		std::vector<uint8_t> Data = {};
        std::string Info = "";
        std::string Notifier = "";

		CommandStatePacket() = default;
		CommandStatePacket(const std::string& error, const std::vector<uint8_t>& data, const std::string& info, const std::string& notifier)
			: Error(error), Data(data), Info(info), Notifier(notifier) {
		}

		MSGPACK_DEFINE(Error, Data, Info, Notifier);
	};
#pragma endregion
#pragma region File & Firmware update
    struct FirmwareUpdatePacket : PacketBase<FirmwareUpdatePacket> {
        std::array<uint8_t, 16> QueryId = {};
		std::string FilePath = "";
		std::string SatelliteId = "";
		uint8_t ModuleMac = 0;
		uint16_t BoardRevision = 0;
		uint16_t CpuType = 0;
		uint16_t Submodule = 0;
		uint16_t FWType = 0;
		uint16_t FWVerMaj = 0;
		uint16_t FWVerMin = 0;
        uint16_t ModuleType = 0;
		uint16_t ModuleConfig = 0;
		uint64_t Flags = 0;
		uint64_t CommandId = 0;
		bool IsBundle = false;
		bool IsFile = false;

        FirmwareUpdatePacket() = default;
        FirmwareUpdatePacket(const std::string& filePath, const std::string& satelliteId, uint8_t moduleMac,
            uint16_t boardRevision, uint16_t cpuType, uint16_t submodule, uint16_t fwType,
            uint16_t fwVerMaj, uint16_t fwVerMin, uint16_t moduleType, uint16_t moduleConfig,
			uint64_t flags, uint64_t commandId, bool isbundle, bool isfile) : FilePath(filePath), SatelliteId(satelliteId), ModuleMac(moduleMac), BoardRevision(boardRevision),
			CpuType(cpuType), Submodule(submodule), FWType(fwType), FWVerMaj(fwVerMaj), FWVerMin(fwVerMin),
			ModuleType(moduleType), ModuleConfig(moduleConfig), Flags(flags), CommandId(commandId), IsBundle(isbundle), IsFile(isfile) {
		}

		MSGPACK_DEFINE(FilePath, SatelliteId, ModuleMac, BoardRevision, CpuType, Submodule,
			FWType, FWVerMaj, FWVerMin, ModuleType, ModuleConfig, Flags, CommandId, IsBundle, IsFile);
    };


    struct FirmwareUpdateResultPacket : PacketBase<FirmwareUpdateResultPacket> {
        std::array<uint8_t, 16> QueryId = {};
        uint64_t CommandId = 0;

        FirmwareUpdateResultPacket() = default;
        FirmwareUpdateResultPacket(uint64_t commandId)
            : CommandId(commandId) {
        }

        MSGPACK_DEFINE(CommandId);
    };
#pragma endregion


#pragma region Data Packet
    struct BeaconTimestampPacket : PacketBase<BeaconTimestampPacket> {
		std::array<uint8_t, 16> QueryId = {};
		uint32_t Unix = 0;       // Unix time
        uint16_t SubTicks = 0; // 0.1ms
        BeaconTimestampPacket() = default;
		BeaconTimestampPacket(uint32_t unix, uint16_t subTicks)
			: Unix(unix), SubTicks(subTicks) {
		}
		MSGPACK_DEFINE(Unix, SubTicks);
	};


    struct BeaconDataPacket : PacketBase<BeaconDataPacket> {
        std::array<uint8_t, 16> QueryId = {};
		uint16_t DataID = 0;
		DataType Type = DataType::UInt8;
		uint16_t Count = 1;
		std::vector<BeaconTimestampPacket> Timestamps = {}; // Array of timestamps
        std::vector<uint8_t> Data = {}; // Raw data
        BeaconDataPacket() = default;
        BeaconDataPacket(uint16_t dataID, DataType type, uint16_t count, const std::vector<BeaconTimestampPacket>& timestamps, const std::vector<uint8_t>& data)
			: DataID(dataID), Type(type), Count(count), Timestamps(timestamps), Data(data) {
		}
		MSGPACK_DEFINE(DataID, Type, Count, Timestamps, Data);
	};

#pragma endregion

#pragma region Service

#pragma endregion
}