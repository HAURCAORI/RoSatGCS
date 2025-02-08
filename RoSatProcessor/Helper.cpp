#include "pch.h"
#include "Helper.h"
#include <sstream>

std::string byteArrayToHexString(const char* byteArray, size_t length)
{
	std::ostringstream hexStream;
	hexStream << std::hex << std::setfill('0'); // Set up hexadecimal formatting
	for (size_t i = 0; i < length; ++i) {
		hexStream << std::setw(2) << static_cast<unsigned int>(static_cast<unsigned char>(byteArray[i]));
	}
	return hexStream.str();
}
