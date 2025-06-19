#include "pch.h"
#include "DataFrame.h"
#include <sstream>
#include "crc16-ccitt.h"
#include "crc16-ccitt-false.h"
#include "crc32.h"

#pragma region DataFrameBuffer
RoSatProcessor::DataFrameBuffer::DataFrameBuffer(char* buffer, std::streamsize size)
	: buffer_(buffer), size_(size)
{
	setp(buffer_, buffer_ + size_);
	setg(buffer_, buffer_, buffer_ + size_);
}

std::streambuf::int_type RoSatProcessor::DataFrameBuffer::overflow(std::streambuf::int_type ch)
{
	if (ch != std::streambuf::traits_type::eof()) {
		if (pptr() >= epptr()) {
			// Buffer overflow
			return std::streambuf::traits_type::eof();
		}
		// Add the character to the buffer
		*pptr() = static_cast<char>(ch);
		pbump(1); // Move the put pointer
		return ch;
	}
	return std::streambuf::traits_type::eof();
}

std::streambuf::int_type RoSatProcessor::DataFrameBuffer::underflow()
{
	if (gptr() < egptr()) {
		return std::streambuf::traits_type::to_int_type(*gptr()); // Return the current character
	}
	return std::streambuf::traits_type::eof(); // No more data to read
}
#pragma endregion


#pragma region Constructors
RoSatProcessor::DataFrame::DataFrame()
	: std::ostream(this), size_(1), used_(0), endian_(Endian::LittleEndian), readPos_(0) {
	buffer_ = new char[1];
	setp(buffer_, buffer_);
	setg(buffer_, buffer_, buffer_);
}

RoSatProcessor::DataFrame::DataFrame(const char* buffer, std::streamsize size)
	: std::ostream(this), size_(size), used_(size), endian_(Endian::LittleEndian), readPos_(0) {
	buffer_ = new char[size];
	std::memcpy(buffer_, buffer, size);
	setp(buffer_, buffer_ + size);
	setg(buffer_, buffer_, buffer_ + size);
}

RoSatProcessor::DataFrame::DataFrame(const std::vector<uint8_t>& buffer)
	: std::ostream(this), size_(buffer.size()), used_(buffer.size()), endian_(Endian::LittleEndian), readPos_(0) {
	buffer_ = new char[buffer.size()];
	std::memcpy(buffer_, buffer.data(), buffer.size());
	setp(buffer_, buffer_ + used_);
	setg(buffer_, buffer_, buffer_ + used_);
}

RoSatProcessor::DataFrame::DataFrame(const DataFrame& rhs)
	: std::ostream(this), size_(rhs.used_), used_(rhs.used_), endian_(rhs.endian_), readPos_(rhs.readPos_) {
	buffer_ = new char[rhs.used_];
	std::memcpy(buffer_, rhs.buffer_, rhs.used_);
	setp(buffer_, buffer_ + used_);
	setg(buffer_, buffer_, buffer_ + used_);
}

RoSatProcessor::DataFrame& RoSatProcessor::DataFrame::operator=(const DataFrame& rhs) {
	if (this == &rhs) {
		return *this;
	}
	size_ = rhs.used_;
	used_ = rhs.used_;
	endian_ = rhs.endian_;
	readPos_ = rhs.readPos_;

	delete[] buffer_;
	buffer_ = nullptr;

	buffer_ = new char[rhs.used_];
	std::memcpy(buffer_, rhs.buffer_, rhs.used_);
	setp(buffer_, buffer_ + used_);
	setg(buffer_, buffer_, buffer_ + used_);

	return *this;
}

RoSatProcessor::DataFrame::DataFrame(DataFrame&& rhs) noexcept
	: std::ostream(this), buffer_(rhs.buffer_), size_(rhs.size_), used_(rhs.used_), endian_(rhs.endian_), readPos_(rhs.readPos_) {
	rhs.buffer_ = nullptr;
}

RoSatProcessor::DataFrame& RoSatProcessor::DataFrame::operator=(DataFrame&& rhs) noexcept {
	if (this == &rhs) {
		return *this;
	}
	size_ = rhs.size_;
	used_ = rhs.used_;
	endian_ = rhs.endian_;
	readPos_ = rhs.readPos_;

	delete[] buffer_;
	buffer_ = nullptr;

	buffer_ = rhs.buffer_;
	rhs.buffer_ = nullptr;
	setp(buffer_, buffer_ + used_);
	setg(buffer_, buffer_, buffer_ + used_);

	return *this;
}

RoSatProcessor::DataFrame::~DataFrame() {
	delete[] buffer_;
}
#pragma endregion

#pragma region Public
std::streamsize RoSatProcessor::DataFrame::size() const {
	return used_;
}

const char* RoSatProcessor::DataFrame::data() const {
	return buffer_;
}

std::unique_ptr<std::streambuf> RoSatProcessor::DataFrame::get() const
{
	return std::make_unique<DataFrameBuffer>(buffer_, used_);
}

void RoSatProcessor::DataFrame::setEndian(Endian endian) {
	endian_ = endian;
}

uint16_t RoSatProcessor::DataFrame::crc16(size_t start)
{
	return crc16(start, used_ - 1);
}

uint16_t RoSatProcessor::DataFrame::crc16_false(size_t start)
{
	return crc16_false(start, used_ - 1);
}

uint16_t RoSatProcessor::DataFrame::crc16(size_t start, size_t end)
{
	if (start > end || end >= used_) { return 0; }
	size_t len = end - start + 1;
	return crc16_ccitt_calc(buffer_ + start, len);
}

uint16_t RoSatProcessor::DataFrame::crc16_false(size_t start, size_t end)
{
	if (start > end || end >= used_) { return 0; }
	size_t len = end - start + 1;
	return crc16_ccitt_false_calc(buffer_ + start, len);
}

uint32_t RoSatProcessor::DataFrame::crc32(size_t start)
{
	return crc32(start, used_ - 1);
}

uint32_t RoSatProcessor::DataFrame::crc32(size_t start, size_t end)
{
	if (start > end || end >= used_) { return 0; }
	size_t len = end - start + 1;
	return MACCRC32_Calc32(buffer_ + start, len);
}

RoSatProcessor::DataFrame& RoSatProcessor::DataFrame::append(const char* data, std::size_t len, Endian endian) {
	while (len > remain()) {
		std::size_t copyLen = remain();
		memcpy(pptr(), data, copyLen, endian);
		pbump((int)copyLen);
		used_ += copyLen;

		alloc();

		data += copyLen;
		len -= copyLen;
	}
	memcpy(pptr(), data, len, endian);
	pbump((int)len);
	used_ += len;

	return *this;
}

RoSatProcessor::DataFrame& RoSatProcessor::DataFrame::appendFromHex(const std::string_view& str)
{
	size_t bufferIndex = 0;
	char highNibble = 0;
	bool isHighNibble = true;

	for (size_t i = 0; i < str.length(); ++i) {
		char c = str[i];
		if (std::isspace(c)) {
			continue; // Ignore whitespace
		}
		if ((c == '0' && (i + 1 < str.length()) &&
			(str[i + 1] == 'x' || str[i + 1] == 'X'))) {
			++i; // Skip '0x' or '0X' prefix
			continue;
		}
		if (!std::isxdigit(c)) {
			throw std::invalid_argument("Invalid character in hex string.");
		}

		// Convert hex digit to its numeric value
		char nibble = std::isdigit(c) ? (c - '0') : (std::tolower(c) - 'a' + 10);

		if (isHighNibble) {
			highNibble = nibble;
			isHighNibble = false;
		}
		else {
			this->put((highNibble << 4) | nibble);
			isHighNibble = true;
		}
	}
	return *this;
}

RoSatProcessor::DataFrame RoSatProcessor::DataFrame::slice(size_t start)
{
	return slice(start, used_ -1);
}

RoSatProcessor::DataFrame RoSatProcessor::DataFrame::slice(size_t start, size_t end)
{
	if (start > end || end >= used_) { throw std::invalid_argument("Invalid slice index"); }
	return DataFrame(buffer_ + start, end - start + 1);
}

void RoSatProcessor::DataFrame::normalize()
{
	for (char* it = buffer_; it != buffer_ + used_; ++it) {
		if (*it == 0x20 && (it + 1) != buffer_ + used_) {
			std::memmove(it, it + 1, (buffer_ + used_) - it);
			*it = ~*(it);
			--used_;
		}
	}
	setp(buffer_ + used_, buffer_ + used_);
}

char RoSatProcessor::DataFrame::getc(size_t pos)
{
	return (pos > (size_t)used_) ? 0 : buffer_[pos];
}

void RoSatProcessor::DataFrame::setPos(size_t pos) {
	readPos_ = pos > (size_t)used_ ? (size_t)used_ : pos;
}

size_t RoSatProcessor::DataFrame::read(char* dest, size_t len)
{
	return size_t();
}

std::string RoSatProcessor::DataFrame::toString() const {
	static constexpr char hexDigits[] = "0123456789ABCDEF";
	std::string result(used_ * 3, '\0');

	for (size_t i = 0; i < (size_t)used_; ++i) {
		unsigned char byte = static_cast<unsigned char>(buffer_[i]);
		result[3 * i] = hexDigits[byte >> 4];
		result[3 * i + 1] = hexDigits[byte & 0xF];
		result[3 * i + 2] = ' ';
	}
	return result;
}

std::vector<uint8_t> RoSatProcessor::DataFrame::toVector() const
{
	return std::vector<uint8_t>(buffer_, buffer_ + used_);
}

char& RoSatProcessor::DataFrame::operator[](unsigned int i) {
	if (i >= used_) {
		throw std::out_of_range("Index out of range");
	}
	return buffer_[i];
}

const char& RoSatProcessor::DataFrame::operator[](unsigned int i) const {
	if (i >= used_) {
		throw std::out_of_range("Index out of range");
	}
	return buffer_[i];
}
#pragma endregion

#pragma region Protected
std::streambuf::int_type RoSatProcessor::DataFrame::overflow(std::streambuf::int_type ch) {
	if (pptr() >= epptr() && remain() <= 0) {
		alloc();
	}
	*pptr() = static_cast<char>(ch);
	pbump(1);
	used_++;
	return ch;
}

std::streambuf::int_type RoSatProcessor::DataFrame::underflow() {
	if (gptr() < egptr()) {
		return std::streambuf::traits_type::to_int_type(*gptr()); // Return the current character
	}
	return std::streambuf::traits_type::eof(); // No more data to read
}
#pragma endregion

#pragma region Private
void RoSatProcessor::DataFrame::alloc() {
	std::streamsize newSize = size_ * 2;
	char* newBuffer = new char[newSize];
	memcpy(newBuffer, buffer_, used_, Endian::LittleEndian);
	delete[] buffer_;

	buffer_ = newBuffer;
	size_ = newSize;

	setp(buffer_ + used_, buffer_ + used_);
}

std::size_t RoSatProcessor::DataFrame::remain() const {
	return size_ - used_;
}

void* RoSatProcessor::DataFrame::memcpy(void* dest, const void* src, size_t len, Endian endian) {
	Endian e = (endian == Endian::None) ? endian_ : endian;
	return (e == Endian::LittleEndian) ? memcpy_le(dest, src, len) : memcpy_be(dest, src, len);
}

void* RoSatProcessor::DataFrame::memcpy_le(void* dest, const void* src, size_t len) {
	char* d = reinterpret_cast<char*>(dest);
	const char* s = reinterpret_cast<const char*>(src);
	while (len--)
		*d++ = *s++;
	return dest;
}

void* RoSatProcessor::DataFrame::memcpy_be(void* dest, const void* src, size_t len) {
	char* d = reinterpret_cast<char*>(dest) + len - 1;
	const char* s = reinterpret_cast<const char*>(src);
	while (len--)
		*d-- = *s++;
	return dest;
}

#pragma endregion