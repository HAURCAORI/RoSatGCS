#include "pch.h"
#include "TaskSerial.h"
#include "crc16-ccitt.h"

#include "cryptopp/cryptlib.h"
#include "cryptopp/filters.h"
#include "cryptopp/modes.h"
#include "cryptopp/base64.h"
#include "cryptopp/aes.h"
#include "cryptopp/hex.h"

/**/
static int64_t SessionID = 0;
static int64_t PacketId = 0;
static int32_t DataSize = 0;
void RoSatProcessor::TaskSerial::callback(const char* ptr, size_t len)
{
	DataFrame df_key;
	df_key << (uint64_t)0 << (uint64_t)0 << (uint64_t)0 << (uint64_t)0;
	DataFrame df_iv;
	df_iv << (uint64_t)0 << (uint64_t)0;


	DataFrame df(ptr, len);
	df.normalize();
	auto temp = df.slice(10,df.getc(0x02_b) + 9);

	// º¹È£È­
	std::string decryptedtext;
	CryptoPP::AES::Decryption dec((CryptoPP::byte*)df_key.data(), CryptoPP::AES::MAX_KEYLENGTH);
	CryptoPP::CBC_Mode_ExternalCipher::Decryption cbc(dec, (CryptoPP::byte*)df_iv.data());
	CryptoPP::StreamTransformationFilter stfDecryptor(cbc, new CryptoPP::StringSink(decryptedtext));
	stfDecryptor.Put((CryptoPP::byte*)(temp.data()), temp.size());
	stfDecryptor.MessageEnd();
	DataFrame rec(decryptedtext.data(), decryptedtext.size());


	DataFrame header;
	DataFrame packet;
	std::string ciphertext;
	if (rec.getc(0) == 0x01_b) {
		std::cout << "\r\nHandshake Init" << std::endl;
		rec >> rec.set(0x1F_b) >> SessionID;
		

		packet.appendFromHex("0100 0000 0000000000000000") << SessionID << uint16_t(0);
		header << 0x02_b << 0x11_b << 0x1A_b << header.crc16_false(0) << packet << header.crc32(0);

		CryptoPP::AES::Encryption enc((CryptoPP::byte*)df_key.data(), CryptoPP::AES::MAX_KEYLENGTH);
		CryptoPP::CBC_Mode_ExternalCipher::Encryption cbce(enc, (CryptoPP::byte*)df_iv.data());
		CryptoPP::StreamTransformationFilter stfEncryptor(cbce, new CryptoPP::StringSink(ciphertext));
		stfEncryptor.Put((CryptoPP::byte*)(header.data()), header.size());
		stfEncryptor.MessageEnd();
	}
	else if (rec.getc(0) == 0x00_b) {
		if (rec.getc(0x0E_b) == 0x14_b) {
			std::cout << "\r\nTP sync" << std::endl;
			rec >> rec.set(0x05_b) >> PacketId;
			

			packet << PacketId << 0x0A_b << 0x1E_b << DataSize << 0xFFFFFFFF << uint32_t(0) << 0x00_b;
			header << 0x00_b << 0x11_b << 0x1B_b << header.crc16_false(0) << packet << header.crc32(0);

			CryptoPP::AES::Encryption enc((CryptoPP::byte*)df_key.data(), CryptoPP::AES::MAX_KEYLENGTH);
			CryptoPP::CBC_Mode_ExternalCipher::Encryption cbce(enc, (CryptoPP::byte*)df_iv.data());
			CryptoPP::StreamTransformationFilter stfEncryptor(cbce, new CryptoPP::StringSink(ciphertext));
			stfEncryptor.Put((CryptoPP::byte*)(header.data()), header.size());
			stfEncryptor.MessageEnd();

			rec >> rec.set(0x29_b) >> DataSize;

			ciphertext.insert(0, 1, '\x00');
		}
	}
	
	DataFrame ret(ciphertext.data(), ciphertext.size());

	std::cout << "rec:" << rec << std::endl;
	std::cout << "ret:" << header << std::endl;
	std::cout << "enc:" << ret << std::endl;


	serial.write(ret);
	
}
/* FP
static int rx = 0;
static int tx = 0;
static int64_t SessionID = 0;
static uint32_t receive = 0;
void RoSatProcessor::TaskSerial::callback(const char* ptr, size_t len)
{
	RoSatProcessor::DataFrame df(ptr, len);
	RoSatProcessor::DataFrame res;
	RoSatProcessor::DataFrame data;
	df.normalize();
	int32_t seq;
	
	

	uint8_t proto = 0x66_b;
	// Send ACK
	df >> df.set(4) >> seq;
	res << 0x21_b << proto << 0x01_b << 0x02_b << seq << 0x00_b << res.crc16(0x01) << 0x22_b;

	
	// Processing
	if (df.getc(0x03_b) == 0x04_b) {} //Sync
	else if (df.getc(0x0A_b) == 0x01_b) {  //Handshake Init
		std::cout << "Handshake Init" << std::endl;
		df >> df.set(0x2B_b) >> SessionID;

		data << 0x21_b << proto << 0x01_b << 0x02_b << seq << 0x00_b
			<< df.getc(0x09_b) << 0x02_b << 0x02_b << uint32_t(0) << 0x16_b
			<< uint16_t(1) << uint16_t(8) << uint64_t(0) << SessionID
			<< uint16_t(1) << data.crc16(1) << 0x22_b;
		
		serial.write(data);
	}
	else if (df.getc(0x0A_b) == 0x03_b && df.getc(0x1A_b) == 0x14_b) {
		std::cout << "Sync" << std::endl;
		uint64_t packetid;
		df >> df.set(0x11_b) >> packetid;

		data << 0x21_b << proto << 0x01_b << 0x02_b << seq << 0x00_b
			<< df.getc(0x09_b) << 0x03_b << 0x02_b << uint32_t(0) << 0x17_b
			<< packetid << 0x0A_b << 0x1E_b << receive << 0xFFFFFF
			<< receive << 0x00_b << data.crc16(1) << 0x22_b;
		
	} else if (df.getc(0x0A_b) == 0x03_b && df.getc(0x1A_b) == 0x0A_b) {
		std::cout << "Data" << std::endl;
		receive = 0x0A_b;
	}
	
	std::cout << "IN[" << rx++ << "]:" << df << "\r\n\r\n";

	std::cout << "RES[" << tx++ << "]:" << res << "\r\n\r\n";
	serial.write(res);
	if (data.size() > 2) {
		std::cout << "RES[" << tx++ << "]:" << data << "\r\n\r\n";
		serial.write(data);
	}
}
	*/
	

RoSatProcessor::TaskSerial::TaskSerial(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 1000)
{

}

RoSatProcessor::TaskSerial::TaskSerial(const TaskSerial& src)
	: TaskSerial(src.m_serviceName, src.m_taskName)
{
	
}

void RoSatProcessor::TaskSerial::stop()
{
	RoSatTask::stop();
}

void RoSatProcessor::TaskSerial::Enqueue(const DataFrame& value)
{
}

BOOL RoSatProcessor::TaskSerial::initialize()
{
	serial.open("COM11", 115200);
	const auto func = std::bind(&RoSatProcessor::TaskSerial::callback, this, std::placeholders::_1, std::placeholders::_2);
	serial.setCallback(func);
	return !serial.errorStatus();
}

void RoSatProcessor::TaskSerial::task()
{

	
}

void RoSatProcessor::TaskSerial::starting()
{
	/*
	DataFrame header;
	DataFrame packet;
	packet.appendFromHex("0100 FFFF 0000000000000000 0100000000000000 0100");
	header << 0x01_b << 0x11_b << 0x1A_b << header.crc16_false(0) << packet << header.crc32(0);

	
	
	DataFrame df_key;
	df_key << (uint64_t)0 << (uint64_t)0 << (uint64_t)0 << (uint64_t)0;

	DataFrame df_iv;
	df_iv << (uint64_t)0 << (uint64_t)0;


	std::string ciphertext;
	CryptoPP::AES::Encryption enc((CryptoPP::byte*)df_key.data(), CryptoPP::AES::MAX_KEYLENGTH);
	CryptoPP::CBC_Mode_ExternalCipher::Encryption cbce(enc, (CryptoPP::byte*)df_iv.data());

	CryptoPP::StreamTransformationFilter stfEncryptor(cbce, new CryptoPP::StringSink(ciphertext));
	stfEncryptor.Put((CryptoPP::byte*)(header.data()), header.size());
	stfEncryptor.MessageEnd();
	DataFrame ret_enc(ciphertext.data(), ciphertext.size());

	std::cout << ret_enc << std::endl;


	DataFrame test;
	test.appendFromHex("70 7B 0A E8 49 13 35 3E BB 07 78 96 F8 C6 68 E8 1B 31 40 7B 2B 90 5A 1A 64 A2 9B F8 F2 F7 87 45 96 E6 BB F3 F6 38 28 FB 7B 24 4E E5 34 E4 8B B4");

	std::string decryptedtext;


	CryptoPP::AES::Decryption dec((CryptoPP::byte*) df_key.data(), CryptoPP::AES::MAX_KEYLENGTH);
	CryptoPP::CBC_Mode_ExternalCipher::Decryption cbc(dec, (CryptoPP::byte*)df_iv.data());

	CryptoPP::StreamTransformationFilter stfDecryptor(cbc, new CryptoPP::StringSink(decryptedtext));
	stfDecryptor.Put((CryptoPP::byte*)(test.data()), test.size());
	stfDecryptor.MessageEnd();

	DataFrame ret(decryptedtext.data(), decryptedtext.size());


	std::cout << "ret" << std::endl;
	std::cout << ret << std::endl;*/

}

void RoSatProcessor::TaskSerial::stopped()
{
	serial.close();
}

