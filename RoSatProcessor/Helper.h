#pragma once
#include <iomanip>
#include <string>

std::string byteArrayToHexString(const char* byteArray, size_t length);

std::vector<uint8_t> hexStringToBytes(const std::string& hexStr);

std::unordered_map<int, std::vector<uint8_t>> readCsvLookup(const std::string& filename);
