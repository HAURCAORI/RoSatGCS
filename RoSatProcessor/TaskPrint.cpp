#include "TaskPrint.h"
#include "pmt/pmt.h"

class char_ptr_streambuf : public std::streambuf {
public:
	char_ptr_streambuf(char* data, std::streamsize size) {
		// Set the get area (start, current, and end of buffer)
		setg(data, data, data + size);
	}

protected:
	// Called when more data is needed by the stream
	int_type underflow() override {
		if (gptr() < egptr()) {
			// Return the next character
			return traits_type::to_int_type(*gptr());
		}
		else {
			// EOF: End of buffer
			return traits_type::eof();
		}
	}
};


std::string byteArrayToHexString(const char* byteArray, size_t length) {
	std::ostringstream hexStream;
	hexStream << std::hex << std::setfill('0'); // Set up hexadecimal formatting
	for (size_t i = 0; i < length; ++i) {
		hexStream << std::setw(2) << static_cast<unsigned int>(static_cast<unsigned char>(byteArray[i]));
	}
	return hexStream.str();
}


RoSatProcessor::TaskPrint::TaskPrint(PCWSTR pszServiceName, PCWSTR pszTaskName) :
	RoSatTask(pszServiceName, pszTaskName, TRUE, 50), m_socket(m_context, zmq::socket_type::sub)
{
	
}

RoSatProcessor::TaskPrint::TaskPrint(const TaskPrint& src)
	: TaskPrint(src.m_serviceName, src.m_taskName)
{

}

BOOL RoSatProcessor::TaskPrint::initialize()
{
	
	m_socket.bind("tcp://127.0.0.1:60000");
	m_socket.set(zmq::sockopt::subscribe, "");

	return TRUE;
}

void RoSatProcessor::TaskPrint::task()
{
	
	zmq::message_t message;
	if (m_socket.recv(message, zmq::recv_flags::none)) {
		std::string data(static_cast<char*>(message.data()), message.size());

		char_ptr_streambuf buffer(static_cast<char*>(message.data()), message.size());
		pmt::pmt_t msg = pmt::deserialize(buffer);

		pmt::pmt_t payload = pmt::cdr(msg);

		if (pmt::is_u8vector(payload)) {
			size_t len = pmt::length(payload);
			const uint8_t* data = pmt::u8vector_elements(payload, len);
			printf("foo = %lu\n", (unsigned long)len);

			std::string str = byteArrayToHexString((char*) data, len);
			str += "\r\n";
			print(str);
			// Process the data bytes...
		}

		//
		//std::string str = byteArrayToHexString(static_cast<char*>(message.data()), message.size());
		//str += "\r\n";
		//print(str);
		//std::cout << "Received: " << data << std::endl;
	}
}

void RoSatProcessor::TaskPrint::starting()
{
	
}

void RoSatProcessor::TaskPrint::stopped()
{

}
