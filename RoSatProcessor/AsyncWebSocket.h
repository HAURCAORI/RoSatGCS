#pragma once
#include <boost/beast.hpp>
#include <boost/beast/ssl.hpp>
#include <boost/utility.hpp>
#include <thread>
#include <memory>
#include <functional>

namespace asio = boost::asio;
namespace beast = boost::beast;
namespace websocket = beast::websocket;
namespace ssl = boost::asio::ssl;
using tcp = asio::ip::tcp;
using ssl_stream = beast::ssl_stream<beast::tcp_stream>;

namespace RoSatProcessor {
    class DataFrame;
}

class AsyncWebSocket : public std::enable_shared_from_this<AsyncWebSocket>, private boost::noncopyable {
public:
    AsyncWebSocket();
    AsyncWebSocket(const std::string& host, const std::string& port, bool useTLS = false);
    virtual ~AsyncWebSocket();

    void connect(const std::string& host = "", const std::string& port = "", bool useTLS = false);
	std::string Address() const;
	std::string Host() const;
	std::string Port() const;
    void close();
    //void write(const char* data, size_t size);
    void write(const std::string& message);
    //void write(const RoSatProcessor::DataFrame& buf);

    void setCallback(const std::function<void(const std::string&)>& callback);
    void clearCallback();

    bool isConnected() const;
private:
    void onResolve(const beast::error_code& ec, tcp::resolver::results_type results);
    void onConnect(const beast::error_code& ec, tcp::resolver::results_type::endpoint_type ep);
    void onSSLHandshake(const beast::error_code& ec);
    void onHandshake(const beast::error_code& ec);
    void doRead();
    void onRead(const beast::error_code& ec, std::size_t bytes_transferred);
    void onWrite(const beast::error_code& ec, std::size_t bytes_transferred);
    void onCloseTLS(const beast::error_code& ec);
	void onClose(const beast::error_code& ec);

    std::string host_;
    std::string port_;
    std::string address_;
    asio::io_context ioContext_;
    tcp::resolver resolver_;

    boost::asio::ssl::context sslContext_;
    std::unique_ptr<ssl_stream> sslStream_;
    std::unique_ptr<websocket::stream<ssl_stream&>> wsTLS_;
    std::unique_ptr<websocket::stream<tcp::socket>> ws_;
    beast::flat_buffer buffer_;

    std::thread backgroundThread_;
    
    bool useTLS_;
    std::atomic<bool> connected_;
    std::atomic<bool> connecting_;

    std::function<void(const std::string&)> callback_;

    const std::size_t kChunkSize = 8192;
};