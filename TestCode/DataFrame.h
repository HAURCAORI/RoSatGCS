#pragma once
#include <iostream>
#include <streambuf>

namespace RoSatProcessor {

	constexpr unsigned char operator"" _b(unsigned long long value) {
		return static_cast<unsigned char>(value);
	}

	enum class Endian {
		None,
		LittleEndian,
		BigEndian
	};

	class DataFrameBuffer : public std::streambuf {
	public:
		DataFrameBuffer(char* buffer, std::streamsize size);

	protected:
		std::streambuf::int_type overflow(std::streambuf::int_type ch) override;
		std::streambuf::int_type underflow() override;

	private:
		char* buffer_;
		std::streamsize size_;
	};


	class DataFrame : public std::streambuf, private std::ostream {
	private:
		struct Endt {};
		struct Sett { size_t s; };
		struct Skpt { size_t s; };
	public:
		DataFrame();
		DataFrame(const char* buffer, std::streamsize size);
		DataFrame(const DataFrame& rhs);
		DataFrame& operator=(const DataFrame& rhs);
		DataFrame(DataFrame&& rhs) noexcept;
		DataFrame& operator=(DataFrame&& rhs) noexcept;

		virtual ~DataFrame();

		std::streamsize size() const;
		const char* data() const;
		std::unique_ptr<std::streambuf> get() const;
		void setEndian(Endian endian);


		DataFrame& append(const char* data, std::size_t len, Endian endian = Endian::None);
		void normalize();
		void setPos(size_t pos = 0);
		static Sett set(size_t pos = 0) { return Sett{ pos }; }
		static Skpt skip(size_t pos = 0) { return Skpt{ pos }; }
		size_t read(char* dest, size_t len);
		
		std::string toString() const;

		

		char& operator[](unsigned int i);
		const char& operator[](unsigned int i) const;

	
		constexpr static const Endt endl{};

		template <typename T>
		friend DataFrame& operator<<(DataFrame&, const T&);
		friend DataFrame& operator<<(DataFrame&, const char*);
		friend std::ostream& operator<<(std::ostream&, const DataFrame&);
		template <typename T>
		friend DataFrame& operator>>(DataFrame&, T&);

		template <typename T>
		friend DataFrame& operator>>(DataFrame&, const T&);
		//friend DataFrame& operator>>(DataFrame&, const Endt&);
		//friend DataFrame& operator>>(DataFrame&, const Sett&);

	protected:
		std::streambuf::int_type overflow(std::streambuf::int_type ch) override;
		std::streambuf::int_type underflow() override;

	private:

		void alloc();
		std::size_t remain() const;
		void* memcpy(void* dest, const void* src, size_t len, Endian endian);
		void* memcpy_le(void* dest, const void* src, size_t len);
		void* memcpy_be(void* dest, const void* src, size_t len);

		char* buffer_;
		std::streamsize size_;
		std::streamsize used_;
		Endian endian_;
		size_t readPos_;
	};

#pragma region overload << operators
	template<typename T>
	inline DataFrame&
		operator<<(DataFrame& out, const T& value) {
		static_assert(false, "Not implemented for type T");
		return out;
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const uint8_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 1);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const uint16_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 2);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const uint32_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 4);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const uint64_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 8);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const int8_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 1);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const int16_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 2);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const int32_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 4);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const int64_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 8);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const char& value) {
		return out.append(reinterpret_cast<const char*>(&value), 1);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const char16_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 2);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const char32_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 4);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const float_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 4);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const double_t& value) {
		return out.append(reinterpret_cast<const char*>(&value), 8);
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const std::string& value) {
		return out.append(value.c_str(), value.size());
	}

	template <>
	inline DataFrame& operator<<(DataFrame& out, const Endian& value) {
		out.setEndian(value);
		return out;
	}

	inline DataFrame& operator<<(DataFrame& out, const char* value) {
		return out.append(value, strlen(value));
	}

	inline std::ostream& operator<<(std::ostream& os, const DataFrame& a) {
		return os << a.toString();
	}
#pragma endregion

#pragma region overload >> operators
	template<typename T>
	inline DataFrame&
		operator>>(DataFrame& df, T& value) {
		constexpr size_t sz = sizeof(T);
		char temp[sz] = { 0 };

		size_t len = df.readPos_ + sz > (size_t) df.used_ ? df.used_ - df.readPos_ : sz;

		if (len > 0) {
			std::memcpy(temp, df.buffer_ + df.readPos_, len);
		}

		df.readPos_ += len;
		std::memcpy(&value, temp, sz);
		
		return df;
	}

	template<typename T>
	inline DataFrame&
		operator>>(DataFrame& df, const T& value) {
		static_assert(false, "Not implemented for const type T");
		return df;
	}

	template<>
	inline DataFrame&
		operator>>(DataFrame& df, const DataFrame::Endt	& value) {
		df.readPos_ = 0;
		return df;
	}

	template<>
	inline DataFrame&
		operator>>(DataFrame& df, const DataFrame::Sett& pos) {
		df.readPos_ = pos.s > (size_t) df.used_ ? (size_t) df.used_ : pos.s;
		return df;
	}

	template<>
	inline DataFrame&
		operator>>(DataFrame& df, const DataFrame::Skpt& pos) {
		df.readPos_ = (df.readPos_ + pos.s > (size_t) df.used_) ?
			(size_t) df.used_ : df.readPos_ + pos.s;
		return df;
	}


#pragma endregion
}