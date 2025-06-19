#include "pch.h"
#include "AsyncWebSocket.h"
#include <boost/asio/ssl.hpp>
#include <boost/asio/strand.hpp>

AsyncWebSocket::AsyncWebSocket()
	: resolver_(boost::asio::make_strand(ioContext_)), sslContext_(boost::asio::ssl::context::tlsv12_client), useTLS_(false), connected_(false), connecting_(false) {
}

AsyncWebSocket::AsyncWebSocket(const std::string& host, const std::string& port, bool useTLS)
    : resolver_(boost::asio::make_strand(ioContext_)), sslContext_(boost::asio::ssl::context::tlsv12_client), connected_(false), connecting_(false), useTLS_(useTLS), host_(host), port_(port){
}

AsyncWebSocket::~AsyncWebSocket() {
    close();
    ioContext_.stop();
}

void AsyncWebSocket::connect(const std::string& host, const std::string& port, bool useTLS) {
    if (connected_ && host_ == host && port_ == port) return;
	if (connecting_) return;

    close();
    ioContext_.restart();

	if (!host.empty() && !port.empty()) {
        host_ = host;
        port_ = port;
        useTLS_ = useTLS;
	}
    
    address_ = host_ + ":" + port_;

	if (host_.empty() || port_.empty()) {
        spdlog::error("Host or port is empty");
		return;
	}

	connecting_ = true;
    spdlog::info("Try to connect websocket server: " + Address());

    if (useTLS_) {
        sslStream_ = std::make_unique<ssl_stream>(boost::asio::make_strand(ioContext_), sslContext_);
        wsTLS_ = std::make_unique<websocket::stream<ssl_stream&>>(*sslStream_);
        if (!SSL_set_tlsext_host_name(wsTLS_->next_layer().native_handle(), host_.c_str()))
        {
            beast::error_code ec{
                static_cast<int>(::ERR_get_error()),
                boost::asio::error::get_ssl_category() };
            std::cerr << "SSL host name: " << ec.message() << "\n";
			connecting_ = false;
            return;
        }

        wsTLS_->next_layer().set_verify_callback(ssl::host_name_verification(host));
		wsTLS_->auto_fragment(true);
		wsTLS_->write_buffer_bytes(kChunkSize);
    }
    else {
        ws_ = std::make_unique<websocket::stream<tcp::socket>>(boost::asio::make_strand(ioContext_));
        ws_->auto_fragment(true);
        ws_->write_buffer_bytes(kChunkSize);
    }


    resolver_.async_resolve(host_, port_, beast::bind_front_handler(&AsyncWebSocket::onResolve, shared_from_this()));

    if (backgroundThread_.joinable()) {
        backgroundThread_.join();
    }

    std::thread t = std::thread([this]() { ioContext_.run(); });
    backgroundThread_.swap(t);
}

std::string AsyncWebSocket::Address() const
{
    return address_;
}

std::string AsyncWebSocket::Host() const
{
    return host_;
}

std::string AsyncWebSocket::Port() const
{
    return port_;
}

void AsyncWebSocket::setCallback(const std::function<void(const std::string&)>& callback) {
    callback_ = callback;
}

void AsyncWebSocket::clearCallback() {
    callback_ = nullptr;
}

bool AsyncWebSocket::isConnected() const { return connected_; }

void AsyncWebSocket::onResolve(const beast::error_code& ec, tcp::resolver::results_type results)
{
	if (!ec) {
        if (useTLS_) {
            asio::async_connect(sslStream_->lowest_layer(), results, beast::bind_front_handler(&AsyncWebSocket::onConnect, shared_from_this()));
        }
        else {
            asio::async_connect(ws_->next_layer(), results, beast::bind_front_handler(&AsyncWebSocket::onConnect, shared_from_this()));
        }
	}
	else {
        connecting_ = false;
        spdlog::error("Resolve failed: " + ec.message());
	}
}



void AsyncWebSocket::onConnect(const beast::error_code& ec, tcp::resolver::results_type::endpoint_type ep) {
    if (!ec) {
        if (useTLS_) {
			wsTLS_->next_layer().async_handshake(ssl::stream_base::client, beast::bind_front_handler(&AsyncWebSocket::onSSLHandshake, shared_from_this()));
        }
        else {
            ws_->set_option(websocket::stream_base::timeout::suggested(beast::role_type::client));
			ws_->set_option(websocket::stream_base::decorator([](websocket::request_type& req) {
				req.set(beast::http::field::user_agent, std::string(BOOST_BEAST_VERSION_STRING) + " websocket-client-async");
				}));

			ws_->async_handshake(address_, "/", beast::bind_front_handler(&AsyncWebSocket::onHandshake, shared_from_this()));
        }
    }
    else {
        connecting_ = false;
        spdlog::error("Connection failed: " + ec.message());
    }
}

void AsyncWebSocket::onSSLHandshake(const beast::error_code& ec) {
    if (!ec) {
        wsTLS_->set_option(websocket::stream_base::timeout::suggested(beast::role_type::client));
        wsTLS_->set_option(websocket::stream_base::decorator([](websocket::request_type& req) {
            req.set(beast::http::field::user_agent, std::string(BOOST_BEAST_VERSION_STRING) + " websocket-client-async-ssl");
            }));

		wsTLS_->async_handshake(address_, "/", beast::bind_front_handler(&AsyncWebSocket::onHandshake, shared_from_this()));
    }
    else {
        connecting_ = false;
        spdlog::error("SSL handshake failed: " + ec.message());
    }
}

void AsyncWebSocket::onHandshake(const beast::error_code& ec) {
	if (!ec) {
        connected_ = true;
        connecting_ = false;
        spdlog::info("Websocket Connected: " + Address());
        doRead();
	}
	else {
        connecting_ = false;
        spdlog::error("WebSocket Handshake failed: " + ec.message());
	}
}

void AsyncWebSocket::write(const std::string& message) {
    if (connected_) {
        auto buffer = asio::buffer(message);
        if (useTLS_) {
            if (buffer.size() > kChunkSize) {
                wsTLS_->write(buffer);
            } else {
                wsTLS_->async_write(buffer, beast::bind_front_handler(&AsyncWebSocket::onWrite, shared_from_this()));
            }

        }
        else {
            if (buffer.size() > kChunkSize) {
                ws_->write(buffer);
            } else {
                ws_->async_write(buffer, beast::bind_front_handler(&AsyncWebSocket::onWrite, shared_from_this()));
            }
        }
    }
}

void AsyncWebSocket::close() {
	if (!connected_) return;
	if (useTLS_) {
        beast::error_code ec;
        wsTLS_->next_layer().lowest_layer().shutdown(tcp::socket::shutdown_both, ec);
        if (ec && ec != boost::system::errc::not_connected) {
            std::cerr << "Shutdown error: " << ec.message() << std::endl;
        }

		wsTLS_->async_close(websocket::close_code::normal, beast::bind_front_handler(&AsyncWebSocket::onCloseTLS, shared_from_this()));

	}
	else {
        beast::error_code ec;
        ws_->next_layer().shutdown(tcp::socket::shutdown_both, ec);
        if (ec && ec != boost::system::errc::not_connected) {
            std::cerr << "Shutdown error: " << ec.message() << std::endl;
        }

		ws_->async_close(websocket::close_code::normal, beast::bind_front_handler(&AsyncWebSocket::onClose, shared_from_this()));
	}
}

void AsyncWebSocket::doRead() {
    if (useTLS_) {
        wsTLS_->async_read(buffer_, beast::bind_front_handler(&AsyncWebSocket::onRead, shared_from_this()));
    }
    else {
        ws_->async_read(buffer_, beast::bind_front_handler(&AsyncWebSocket::onRead, shared_from_this()));
    }
}

void AsyncWebSocket::onRead(const beast::error_code& ec, std::size_t bytes_transferred) {
    boost::ignore_unused(bytes_transferred);
    if (!ec) {
        std::string message = beast::buffers_to_string(buffer_.data());
        buffer_.consume(buffer_.size());

        if (callback_) callback_(message);

        doRead();
    }
    else {
        if (ec.value() != 995) {
            spdlog::error("Read error: " + ec.message());
            close();
        }
    }
}

void AsyncWebSocket::onWrite(const beast::error_code& ec, std::size_t bytes_transferred) {
    boost::ignore_unused(bytes_transferred);
    if (ec) {
        spdlog::error("Write error: " + ec.message());
    }
}

void AsyncWebSocket::onCloseTLS(const beast::error_code& ec)
{
    if (ec && ec != boost::asio::error::operation_aborted && ec.value() != 10058) {
        spdlog::error("WebSocket Close Error: " + ec.message());
    }
    sslStream_->next_layer().cancel();
    sslStream_->next_layer().close();
	sslStream_->async_shutdown(beast::bind_front_handler(&AsyncWebSocket::onClose, shared_from_this()));
}


void AsyncWebSocket::onClose(const beast::error_code& ec)
{
    if (ec != boost::asio::error::operation_aborted && !(ec.value() == 10058 || ec.value() == 10009)) {
        spdlog::error("Close error: " + ec.message() + std::to_string(ec.value()));
	}

    connected_ = false;
    spdlog::info("Websocket closed: " + Address());
    asio::post(boost::asio::make_strand(ioContext_), [self = shared_from_this()] {
        if (self->useTLS_) {
            self->wsTLS_.reset();
            self->sslStream_.reset();
        }
        else {
            self->ws_.reset();
        }
    });

}

