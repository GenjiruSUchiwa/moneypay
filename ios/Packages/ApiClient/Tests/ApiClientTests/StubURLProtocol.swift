import Foundation
import Synchronization

nonisolated final class StubURLProtocol: URLProtocol {
    struct Exchange: Sendable {
        var status = 200
        var headers: [String: String] = [:]
        var body = Data()
        var fails = false
        var received: URLRequest?
        var receivedBody: Data?
    }

    static let exchange = Mutex(Exchange())

    static func reset(status: Int = 200, headers: [String: String] = [:], body: Data = Data(), fails: Bool = false) {
        exchange.withLock { $0 = Exchange(status: status, headers: headers, body: body, fails: fails) }
    }

    static func session() -> URLSession {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [StubURLProtocol.self]
        return URLSession(configuration: configuration)
    }

    static var received: URLRequest? { exchange.withLock { $0.received } }
    static var receivedBody: Data? { exchange.withLock { $0.receivedBody } }

    override static func canInit(with request: URLRequest) -> Bool { true }
    override static func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        let request = self.request
        let body = request.httpBody ?? request.httpBodyStream.map(Self.drain)
        let exchange = Self.exchange.withLock {
            $0.received = request
            $0.receivedBody = body
            return $0
        }
        guard !exchange.fails, let url = request.url,
              let response = HTTPURLResponse(url: url, statusCode: exchange.status, httpVersion: nil, headerFields: exchange.headers)
        else {
            client?.urlProtocol(self, didFailWithError: URLError(.cannotConnectToHost))
            return
        }
        client?.urlProtocol(self, didReceive: response, cacheStoragePolicy: .notAllowed)
        client?.urlProtocol(self, didLoad: exchange.body)
        client?.urlProtocolDidFinishLoading(self)
    }

    override func stopLoading() {}

    private static func drain(_ stream: InputStream) -> Data {
        stream.open()
        defer { stream.close() }
        var data = Data()
        var buffer = [UInt8](repeating: 0, count: 4096)
        while stream.hasBytesAvailable {
            let read = stream.read(&buffer, maxLength: buffer.count)
            guard read > 0 else { break }
            data.append(buffer, count: read)
        }
        return data
    }
}
