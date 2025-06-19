#pragma once
#include <string>
#include <array>

namespace RoSatProcessor {
	struct CommandCpPacket;
	struct CommandRadioPacket;
	struct CommandAesPacket;
	struct CommandRotatorPacket;
	struct CommandBeaconPacket;
	struct CommandStatePacket;
	struct CommandCpResultPacket;
	struct CommandRadioResultPacket;
	struct CommandRotatorResultPacket;
	struct FirmwareUpdatePacket;
	struct FirmwareUpdateResultPacket;
	class DataFrame;

	enum class WebSocketPriority {
		Low = 0,
		Normal = 1,
		High = 2,
		Critical = 3
	};

	class WebSocketPacket {
	private:
		static uint64_t id;
		std::array<uint8_t, 16> m_query_id;
		uint64_t m_id;
		std::string m_packet;
		WebSocketPriority m_priority;
		WebSocketPacket();
		WebSocketPacket(uint64_t id, const std::string& packet, WebSocketPriority priority = WebSocketPriority::Normal);
	public:
		WebSocketPacket(const WebSocketPacket&) = default;
		WebSocketPacket(WebSocketPacket&&) noexcept = default;
		WebSocketPacket& operator=(const WebSocketPacket&) = default;
		WebSocketPacket& operator=(WebSocketPacket&&) noexcept = default;

		std::array<uint8_t, 16> QueryId() const;
		uint64_t Id() const;
		std::string Get() const;
		DataFrame Serialize() const;
		static DataFrame Serialize(const WebSocketPacket& packet);
		static WebSocketPacket Deserialize(const DataFrame& frame);

		static WebSocketPacket CreateCPCommand(const CommandCpPacket& packet);
		static WebSocketPacket CreateCPCommandOneWay(const CommandCpPacket& packet);
		static WebSocketPacket CreateFWUpd(const FirmwareUpdatePacket& packet);
		static WebSocketPacket CreateFWUpdBundle(const FirmwareUpdatePacket& packet);
		static WebSocketPacket CreateFilePacket(const FirmwareUpdatePacket& packet);
		static WebSocketPacket CreateInitRadio(const CommandRadioPacket& packet);
		static WebSocketPacket CreateUpdateRadio(const CommandRadioPacket& packet);
		static WebSocketPacket CreateUpdateAESKey(const CommandRadioPacket& packet);
		static WebSocketPacket CreateRadioConn(const CommandRadioPacket& packet);
		static WebSocketPacket CreateRotatorSetPosition(const CommandRotatorPacket& packet);
		static WebSocketPacket CreateBeaconListen(const CommandBeaconPacket& packet);
		static WebSocketPacket CreateCancel(const CommandStatePacket& packet);

		static std::string GetType(std::string_view packet);
		static uint64_t GetRequestId(std::string_view packet);

		static CommandCpResultPacket ParseCpResult(std::string_view packet);
		static FirmwareUpdateResultPacket ParseFwUpdateResult(std::string_view packet);
		static CommandRadioResultPacket ParseRadioResult(std::string_view packet);
		static CommandRotatorResultPacket ParseRotatorResult(std::string_view packet);
		static CommandBeaconPacket ParseBeaconResult(std::string_view packet);
		static CommandStatePacket ParseStateResult(std::string_view packet);

		bool operator<(const WebSocketPacket& rhs) const;
		bool operator==(const WebSocketPacket& other) const;
	};
}

namespace std {
	template<>
	class hash<RoSatProcessor::WebSocketPacket> {
	public:
		size_t operator()(const RoSatProcessor::WebSocketPacket& rhs) const { return rhs.Id(); }
	};
}