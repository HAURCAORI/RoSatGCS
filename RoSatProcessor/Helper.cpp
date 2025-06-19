#include "pch.h"
#include "Helper.h"
#include <sstream>
#include <memory>

std::string byteArrayToHexString(const char* byteArray, size_t length)
{
	std::ostringstream hexStream;
	hexStream << std::hex << std::setfill('0'); // Set up hexadecimal formatting
	for (size_t i = 0; i < length; ++i) {
		hexStream << std::setw(2) << static_cast<unsigned int>(static_cast<unsigned char>(byteArray[i]));
	}
	return hexStream.str();
}

std::vector<uint8_t> hexStringToBytes(const std::string& hexStr) {
    std::vector<uint8_t> bytes;
    std::string compact;

    for (char c : hexStr) {
        if (!std::isspace(static_cast<unsigned char>(c))) {
            compact += c;
        }
    }

    if (compact.size() % 2 != 0) {
        throw std::runtime_error("Hex string has an odd number of characters");
    }

    for (size_t i = 0; i < compact.size(); i += 2) {
        std::string byteStr = compact.substr(i, 2);
        bytes.push_back(static_cast<uint8_t>(std::stoi(byteStr, nullptr, 16)));
    }

    return bytes;
}


std::unordered_map<int, std::vector<uint8_t>> readCsvLookup(const std::string& filename) {
    std::unordered_map<int, std::vector<uint8_t>> lookup;
    std::ifstream file(filename);
    std::string line;

    std::getline(file, line);

    while (std::getline(file, line)) {
        std::istringstream ss(line);
        std::string idStr, valueStr;
        if (std::getline(ss, idStr, ',') && std::getline(ss, valueStr)) {
            int commandId = std::stoi(idStr);
            std::vector<uint8_t> payload = hexStringToBytes(valueStr);
            lookup[commandId] = payload;
        }
    }

    return lookup;
}