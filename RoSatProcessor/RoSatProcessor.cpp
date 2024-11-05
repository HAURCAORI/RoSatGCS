#include <iostream>
#include <librdkafka/rdkafka.h>
#include <string>

#include <assert.h>
#include <filesystem>
#include <chrono>

#include "re2/re2.h"
#include "absl/strings/str_format.h"
#include "fidl_parser_interface.h"
#include "boost/asio.hpp"
#include <Windows.h>

#include "crc16-ccitt.h"
#include "crc32.h"

static void msg_process(rd_kafka_message_t* message);

enum ReceiveState {
    Null = 0,
    None = 1,
    Begin,
    HandShake,
    Sync,
    CP
};

#pragma pack(push,1)

struct Tail {
    uint16_t crc;
    uint8_t end = 0x22;
};

struct MacResponse {
    uint8_t begin = 0x21;
    uint8_t pragma = 0x66;
    uint8_t version = 0x01;
    uint8_t channel = 0x02;
    uint32_t seq;
    uint8_t error = 0x00;
    uint16_t crc;
    uint8_t end = 0x22;
};

struct Header{
    uint8_t begin = 0x21;
    uint8_t pragma = 0x66;
    uint8_t version = 0x01;
    uint8_t channel = 0x02;
    uint32_t seq;
    uint8_t inter = 0x00;
    uint8_t priority = 0x07;
    uint8_t target = 0x44; //0x66
    uint8_t source = 0x44; // 0x01
    uint8_t protocol = 0x0C;
    uint8_t size;
};

struct HandshakeHeader {
    uint8_t begin = 0x21;
    uint8_t pragma = 0x66;
    uint8_t version = 0x01;
    uint8_t channel = 0x02;
    uint32_t seq;
    uint8_t inter = 0x00;
    uint8_t destaddr = 0x44;
    uint8_t protocol = 0x02;
    uint8_t unknown1 = 0x02;
    uint32_t unknown2 = 0x00;
    uint8_t size;   
};

struct HandshakeData {
    uint16_t version = 0x01;
    uint16_t mtu = 0x08;
    uint64_t flag = 0x00;
    uint64_t seq;
    uint16_t protocolversion = 0x01;
};

struct HandshakeResponsePacket {
    HandshakeHeader header;
    HandshakeData data;
    Tail tail;
};

struct TPHeader {
    uint64_t id;
    uint8_t protocolId;
    uint8_t flag; // datafram 0x0A / sync 0x14 / status 0x1E
};

struct TPSync {
    uint64_t id;
    uint8_t protocolId;
    uint8_t flag = 0x14; // datafram 0x0A / sync 0x14 / status 0x1E
    uint64_t context;
    uint16_t systemType;
    uint64_t systemAddress;
    uint64_t localAddress;
    uint32_t datagram;
};

struct TPSyncPacket {
    HandshakeHeader header;
    TPSync tp;
    Tail tail;
};

struct TPStatus {
    uint64_t id;
    uint8_t protocolId;
    uint8_t flag = 0x1E;// datafram 0x0A / sync 0x14 / status 0x1E
    uint32_t recevied;
    uint32_t maxBurst;
    uint32_t success;
    uint8_t error;
};

struct TPStatusPacket {
    HandshakeHeader header;
    TPStatus tp;
    Tail tail;
};

struct CPHeader {
    uint32_t preamble = 0x45632563; //63256345
    uint16_t version = 0x0101;
    uint16_t size = 0x22;
    uint64_t flag = 0;
    uint64_t cmdId;
    uint32_t cmdType;
    uint8_t trip = 0x01;
    uint32_t burst = 0xffffffff;
    uint8_t err = 0;
    uint8_t packet_size;
};

struct TPData {
    uint64_t id;
    uint8_t protocolId;
    uint8_t flag = 0x0A;// datafram 0x0A / sync 0x14 / status 0x1E
    uint32_t address;
};

struct FP {
    uint16_t protocol = 0x0A;
    uint32_t funcID = 0x80000016;
    uint16_t seqID = 0x00;
    uint8_t err = 0x00;
    byte data[69] = { 0 }; //67
};

struct CPPacket {
    HandshakeHeader header;
    TPData tp;
    //CPHeader cp;
    FP fp;
    //uint32_t crc32;
    Tail tail;
};


struct FPResponse {
    Header header;
    FP fp;
    Tail tail;
};


struct Req {
    uint16_t protocol = 0x0A;
    uint32_t funcID = 0x16;
    uint16_t seqID = 0x00;
    uint8_t err = 0x00;
    uint16_t crc;
    uint8_t end = 0x22;
};


#pragma pack(pop)

std::vector<char> HexToBytes(const std::string& hex) {
    std::vector<char> bytes;

    for (unsigned int i = 0; i < hex.length(); i += 2) {
        std::string byteString = hex.substr(i, 2);
        char byte = (char)strtol(byteString.c_str(), NULL, 16);
        bytes.push_back(byte);
    }
    return bytes;
}

std::string uint8_vector_to_hex_string(const std::vector<uint8_t>& v) {
    std::stringstream ss;
    ss << std::hex << std::setfill('0');
    std::vector<uint8_t>::const_iterator it;

    for (it = v.begin(); it != v.end(); it++) {
        ss << std::uppercase << std::setw(2) << static_cast<unsigned>(*it);
    }
    
    return ss.str();
}

class SimpleSerial
{
public:
    SimpleSerial(std::string port, unsigned int baud_rate) : io(), serial(io, port) {
        serial.set_option(boost::asio::serial_port_base::baud_rate(baud_rate));
    }

    void writeString(std::string s) {
        auto ret = HexToBytes(s);
        auto ptr = ret.data();
        boost::asio::write(serial, boost::asio::buffer(ptr, ret.size()));
        printf("Send:");
    }
    void writeByte(uint8_t* ptr, size_t size) {
        boost::asio::write(serial, boost::asio::buffer(ptr, size));
        printf("Send:");
        std::vector<uint8_t> sended;
        bool escape = false;
        uint8_t tmp = 0x20;
        for (int i = 0; i < size; ++i) {
            printf("%02X", *(ptr + i));
            if ((i > 0 && i < size-1) &&(*(ptr + i) == 0x20 || *(ptr + i) == 0x21 || *(ptr + i) == 0x22)) {
                tmp = 0x20;
                boost::asio::write(serial, boost::asio::buffer(&tmp, 1));
                sended.push_back(tmp);
                tmp = ~(*(ptr + i));
                boost::asio::write(serial, boost::asio::buffer(&tmp, 1));
                sended.push_back(tmp);
                continue;
            }
            boost::asio::write(serial, boost::asio::buffer(ptr + i, 1));
            sended.push_back(*(ptr + i));
        }
        printf("\r\n");
        //std::cout << "sended:" << uint8_vector_to_hex_string(sended) << std::endl;
    }

    std::string read() {
        ReceiveState state = ReceiveState::None;
        uint8_t buffer = 0x00;
        bool isescape = false;;
        std::vector<uint8_t> vec;
        auto start = std::chrono::high_resolution_clock::now();
        int counter = 0;
        bool check = false;
        for (;;)
        {
            boost::asio::read(serial, boost::asio::buffer(&buffer, 1));
            if (buffer == 0x22) {
                vec.push_back(buffer);
                std::cout << uint8_vector_to_hex_string(vec) << std::endl;

                auto elapse = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::high_resolution_clock::now() - start);
                if (elapse.count() > 3000) {
                    state = ReceiveState::None;
                }
                start = std::chrono::high_resolution_clock::now();


                /*
                * state = ReceiveState::Null;

                Header header;
                memcpy(&header, vec.data(), sizeof(header));

                MacResponse response;
                response.pragma = 0x55;
                response.seq = header.seq;
                response.crc = crc16_ccitt_calc(((uint8_t*)&response) + 1, sizeof(MacResponse) - 4);
                writeByte((uint8_t*)&response, sizeof(MacResponse));

                FPResponse result;
                result.header.pragma = 0x55;
                result.header.seq = header.seq;
                result.header.size = sizeof(FP);
                result.tail.crc = crc16_ccitt_calc(((uint8_t*)&result) + 1, sizeof(FPResponse) - 4);
                writeByte((uint8_t*)&result, sizeof(FPResponse));
                */

                switch (state) {
                case ReceiveState::None:
                {
                    Header header;
                    memcpy(&header, vec.data(), sizeof(header));

                    MacResponse response;
                    response.seq = header.seq;
                    response.crc = crc16_ccitt_calc(((uint8_t*)&response) + 1, sizeof(MacResponse) - 4);
                    // send twice
                    writeByte((uint8_t*)&response, sizeof(MacResponse));

                    state = ReceiveState::Begin;

                    counter = 0;
                    check = false;

                    break;
                }
                case ReceiveState::Begin:
                {
                    Header header;
                    memcpy(&header, vec.data(), sizeof(header));

                    MacResponse response;
                    response.seq = header.seq;
                    response.crc = crc16_ccitt_calc(((uint8_t*)&response) + 1, sizeof(MacResponse) - 4);
                    writeByte((uint8_t*)&response, sizeof(MacResponse));
                    
                    state = ReceiveState::HandShake;

                    break;
                }
                case ReceiveState::HandShake:
                {
                    
                    HandshakeHeader header;
                    memcpy(&header, vec.data(), sizeof(header));

                    MacResponse response;
                    response.seq = header.seq;
                    response.crc = crc16_ccitt_calc(((uint8_t*)&response) + 1, sizeof(MacResponse) - 4);
                    writeByte((uint8_t*)&response, sizeof(MacResponse));


                    if (header.protocol != 0x01) {
                        break;
                    }
                    HandshakeResponsePacket packet;
                    packet.header.seq = header.seq;
                    packet.header.size = sizeof(HandshakeData);
                    packet.data.seq = header.seq;
                    packet.tail.crc = crc16_ccitt_calc(((uint8_t*)&packet) + 1, sizeof(HandshakeResponsePacket) - 4);

                    writeByte((uint8_t*)&packet, sizeof(HandshakeResponsePacket));
                    
                    state = ReceiveState::Sync;
                    printf("Sync\r\n");

                    break;
                }
                case ReceiveState::Sync:
                {
                    if (vec.size() < sizeof(TPSyncPacket)) {
                        break;
                    }

                    TPSyncPacket received;
                    memcpy(&received, vec.data(), sizeof(TPSyncPacket));

                    MacResponse response;
                    response.seq = received.header.seq;
                    response.crc = crc16_ccitt_calc(((uint8_t*)&response) + 1, sizeof(MacResponse) - 4);
                    writeByte((uint8_t*)&response, sizeof(MacResponse));


                    

                    TPStatusPacket packet;
                    packet.header.seq = received.header.seq;
                    packet.header.size = sizeof(TPStatus);
                    packet.header.protocol = 0x03;
                    packet.tp.id = received.tp.id;
                    packet.tp.protocolId = received.tp.protocolId;
                    packet.tp.recevied = 0;
                    packet.tp.maxBurst = 0;
                    packet.tp.success = 0;
                    packet.tp.error = 0;
                    packet.tail.crc = crc16_ccitt_calc(((uint8_t*)&packet) + 1, sizeof(TPStatusPacket) - 4);
                    writeByte((uint8_t*)&packet, sizeof(TPStatusPacket));

                    if (received.tp.flag == 0x0A) {
                        //counter++;
                    }
                    counter = 0x30;
                    
                    if (check) {
                        state = ReceiveState::CP;
                    }
                    check = true;
                    Sleep(100);
                    break;
                }
                case ReceiveState::CP:
                {
                    HandshakeHeader received;
                    memcpy(&received, vec.data(), sizeof(HandshakeHeader));

                    MacResponse response;
                    response.seq = received.seq;
                    response.crc = crc16_ccitt_calc(((uint8_t*)&response) + 1, sizeof(MacResponse) - 4);
                    writeByte((uint8_t*)&response, sizeof(MacResponse));

                    CPPacket packet;
                    packet.header.seq = received.seq;
                    packet.header.size = sizeof(TPData) + sizeof(FP);//sizeof(TPData) + sizeof(CPHeader) + sizeof(FP) + 4;
                    packet.header.protocol = 0x03;
                    packet.tp.id = 0x16;//0x16;
                    packet.tp.protocolId = 0x0A;
                    packet.tp.address = 0x00;//0x44;
                    //packet.cp.cmdId = 0x0000000000000016;
                    //packet.cp.cmdType =  0x80000016;
                    //packet.cp.packet_size = sizeof(FP);
                    //packet.crc32 = MACCRC32_Calc32((uint8_t*)&packet + sizeof(HandshakeHeader) + sizeof(TPData), sizeof(CPHeader)+ sizeof(FP), 0xffffffff);
                    packet.tail.crc = crc16_ccitt_calc(((uint8_t*)&packet) + 1, sizeof(CPPacket) - 4);

                    writeByte((uint8_t*)&packet, sizeof(CPPacket));
                    //writeString("216601020100000000440302000000008116000000000000000A0A0000000063256345010120DD00000000000000000016000000000000001600000001FFFFFFFF004C0A0016000000010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002EBC271B6DFE22");
                    state = ReceiveState::Sync;
                    Sleep(100);

                    break;
                }
                }
             

                vec.clear();

                continue;
            }
            if (buffer == 0x20 && isescape == false) {
                isescape = true;
                continue;
            }
            if (isescape) {
                isescape = false;
                buffer = ~buffer;
            }
            vec.push_back(buffer);
            //printf("%02X", buffer);
        }
    }

private:
    boost::asio::io_service io;
    boost::asio::serial_port serial;
};

using namespace RoSatParser;
int main()
{
    auto obj = SimpleSerial("COM5", 19200);
    obj.read();
    

    
    auto vec = HexToBytes("6325634501012200000000000000000016000000000000001600000001FFFFFFFF004C0A001600000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
    uint8_t* ptr = reinterpret_cast<uint8_t*>(vec.data());
    uint32_t value = MACCRC32_Calc32(ptr, vec.size(),0xffffffff);
    for (int i = 0; i < 4; ++i) {
        printf("%02X", *(((uint8_t*) &value)+i));
    }
    
    

    /*
    FIDL::FIDLParser* parser = FIDL::CreateParser();

    std::string path = R"(C:\Users\sunny\Desktop\RoSat\FIDL)";
    std::string savepath = R"(C:\Users\sunny\Desktop\RoSat\FIDLResult\)";
    
    for (const auto& file : std::filesystem::directory_iterator(path)) {
        if (file.path().extension() != ".fidl") {
            continue;
        }
        auto start = std::chrono::high_resolution_clock::now();

        std::cout << "Parsing:" << file.path().filename() << std::endl;

        std::string resultfile = file.path().filename().string();
        resultfile = resultfile.substr(0, resultfile.find_first_of('.')) + ".json";

        auto ret = FIDL::ParseStringToFile(parser, file.path().string(), savepath + resultfile);
        if (ret != ParseResult::Success) {
            std::cout << "Parse Error" << std::endl;
            StringResult str = FIDL::GetResult(parser);
            std::cout << str << std::endl;
            DisposeString(str);
            break;
        }
        auto milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::high_resolution_clock::now() - start);
        std::cout << "->Elapsed:" << milliseconds.count() << "ms" << std::endl;
        
    }
    return 0;

    path = R"(C:\Users\sunny\Desktop\RoSat\FIDL\ConOps.fidl)";
    auto ret = FIDL::ParseString(parser, path);

    StringResult str = FIDL::GetResult(parser);
    std::cout << str << std::endl;
    DisposeString(str);



    /*
    FIDLParser::FIDLFileParser* parser = FIDLParser::Create();
    FIDLParser::StringResult str = FIDLParser::getExtension(parser);
    std::cout << "Extension:" << str << std::endl;
    FIDLParser::Dispose(str);
    
    parser->Parse("C:\\Users\\sunny\\Desktop\\RoSat\\FIDL\\Beacons.fidl");
    str = FIDLParser::getPath(parser);
    std::cout << "Path:" << str << std::endl;
    FIDLParser::Dispose(str);
    FIDLParser::Destroy(parser);*/
    return 0;
}

/*
char hostname[128] = "host";
char errstr[512];
rd_kafka_resp_err_t err;
rd_kafka_conf_t* conf = rd_kafka_conf_new();

if (rd_kafka_conf_set(conf, "client.id", hostname,
    errstr, sizeof(errstr)) != RD_KAFKA_CONF_OK) {
    fprintf(stderr, "%% %s\n", errstr);
    exit(1);
}

if (rd_kafka_conf_set(conf, "group.id", "foo",
    errstr, sizeof(errstr)) != RD_KAFKA_CONF_OK) {  
    fprintf(stderr, "%% %s\n", errstr);
    exit(1);
}

if (rd_kafka_conf_set(conf, "bootstrap.servers", "58.239.56.213:39093",
    errstr, sizeof(errstr)) != RD_KAFKA_CONF_OK) {
    fprintf(stderr, "%% %s\n", errstr);
    exit(1);
}


/* Create Kafka consumer handle */
/*

rd_kafka_t* rk;
if (!(rk = rd_kafka_new(RD_KAFKA_CONSUMER, conf,
    errstr, sizeof(errstr)))) {
    fprintf(stderr, "%% Failed to create new consumer: %s\n", errstr);
    exit(1);
}

bool running = true;

rd_kafka_poll_set_consumer(rk);
conf = NULL;

const char* topic = "mysql.datas.Sample";

rd_kafka_topic_partition_list_t* subscription = rd_kafka_topic_partition_list_new(1);
rd_kafka_topic_partition_list_add(subscription, topic, RD_KAFKA_PARTITION_UA);

// Subscribe to the list of topics.

err = rd_kafka_subscribe(rk, subscription);
if (err) {
    fprintf(stderr, "%% Failed to subscribe\n");
    exit(1);
}


while (running) {
    rd_kafka_message_t* rkmessage = rd_kafka_consumer_poll(rk, 1000);
    std::cout << "-" << std::endl;
    if (rkmessage) {
        msg_process(rkmessage);
        rd_kafka_message_destroy(rkmessage);
    }
}

static void msg_process(rd_kafka_message_t* message) {
    std::string st = std::string((char*) message->payload);
    std::cout << st << std::endl;
}
*/