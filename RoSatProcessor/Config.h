#pragma once
#include <iostream>
#include <fstream>
#include <string>
#include <unordered_map>
#include <memory>
#include <msgpack.hpp>

#define DEFAULT_CONFIG_PATH R"(RoSatProcessor.config)"

#define DEFINE_PROPERTY(name, type, default_value) \
private: \
    Property<type> name{ this, #name, #type, default_value }; \
public: \
    static type Get##name() noexcept { return Config::Instance().Get<type>(#name); } \
    static void Set##name(type value) noexcept { Config::Instance().Set<type>(#name, value); }

#include <filesystem>
#ifdef _WIN32
#include <windows.h>
#elif
#include <unistd.h>
#endif

static std::filesystem::path GetExeDirectory()
{
#ifdef _WIN32
    // Windows specific
    wchar_t szPath[MAX_PATH];
    GetModuleFileNameW(NULL, szPath, MAX_PATH);
#else
    // Linux specific
    char szPath[PATH_MAX];
    ssize_t count = readlink("/proc/self/exe", szPath, PATH_MAX);
    if (count < 0 || count >= PATH_MAX)
        return {}; // some error
    szPath[count] = '\0';
#endif
    return std::filesystem::path{ szPath }.parent_path() / "";
}

namespace RoSatProcessor {
    class Config {
    private:
        Config() = default;
        Config(const Config&) = delete;
        Config& operator=(const Config&) = delete;
        Config(Config&&) = delete;
        Config& operator=(Config&&) = delete;

        struct BaseProperty { virtual ~BaseProperty() = default; };
        std::unordered_map<std::string, std::unique_ptr<BaseProperty>> m0;

        template<typename T>
        struct Property : public BaseProperty {
            Config* p;
            std::string name;
            std::string type;
            T value;
            
            Property(Config* p, const std::string& name, const std::string& type, T&& value)
                : p(p), name(name), type(type), value(std::forward<T>(value)) {
                p->m0[name] = std::unique_ptr<Property<T>>(this);
            }
            virtual ~Property() = default;

            MSGPACK_DEFINE(value);
        };

    public:
        static Config& Instance() {
            static Config instance;
            return instance;
        }

        template<typename T>
        static T Get(const std::string& s1) {
            auto mapIter = Config::Instance().m0.find(s1);
            if (mapIter == Config::Instance().m0.end()) { throw std::invalid_argument(s1); }

            auto* prop = dynamic_cast<Property<T>*>(mapIter->second.get());
            if (!prop) throw std::bad_cast();
            return prop->value;
        }

        template<typename T>
        static void Set(const std::string& s1, const T& value) {
            auto mapIter = Config::Instance().m0.find(s1);
            if (mapIter == Config::Instance().m0.end()) { throw std::invalid_argument(s1); }

            auto* prop = dynamic_cast<Property<T>*>(mapIter->second.get());
            if (!prop) throw std::bad_cast();
            prop->value = value;
        }
        
        static bool Export(const std::string& path = "") {
            std::string p = path;
            if (p.empty())
                p = GetConfigPath();
            if (p == DEFAULT_CONFIG_PATH)
                p = GetExeDirectory().string() + DEFAULT_CONFIG_PATH;

            std::ofstream fout(p, std::ios::binary);
			if (!fout) return false;

            msgpack::sbuffer sbuf;
            msgpack::packer<msgpack::sbuffer> packer(sbuf);
            packer.pack(Instance());

            fout.write(sbuf.data(), sbuf.size());

            fout.close();
        }

        static bool Import(const std::string& path = "") {
            std::string p = path;
            if (p.empty())
                p = GetConfigPath();
            if (p == DEFAULT_CONFIG_PATH)
				p = GetExeDirectory().string() + DEFAULT_CONFIG_PATH;

            std::ifstream fin(p, std::ios::binary);
            if (!fin) return false;

            try {
                std::vector<char> buffer((std::istreambuf_iterator<char>(fin)), std::istreambuf_iterator<char>());

                msgpack::object_handle oh = msgpack::unpack(buffer.data(), buffer.size());
                msgpack::object obj = oh.get();
                obj.convert(Instance());
            }
            catch(const std::exception&) {
                return false;
            }

            return true;
        }

        DEFINE_PROPERTY(ConfigPath, std::string, DEFAULT_CONFIG_PATH);
        DEFINE_PROPERTY(UplinkFrequency, uint32_t, 0);
		DEFINE_PROPERTY(DownlinkFrequency, uint32_t, 0);
		DEFINE_PROPERTY(Encrypted, bool, false);
        DEFINE_PROPERTY(WebSocketHost, std::string, "127.0.0.1");
		DEFINE_PROPERTY(WebSocketPort, std::string, "6660");
        DEFINE_PROPERTY(WebSocketTLS, bool, false);
		DEFINE_PROPERTY(DebugMode, bool, false);

        MSGPACK_DEFINE(UplinkFrequency, DownlinkFrequency, WebSocketHost, WebSocketPort)

    };
}