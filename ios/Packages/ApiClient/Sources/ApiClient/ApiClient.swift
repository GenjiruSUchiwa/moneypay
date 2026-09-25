import Foundation

public struct ApiClient: Sendable {
    enum Method: String {
        case get = "GET"
        case post = "POST"
        case delete = "DELETE"
    }

    enum Header {
        static let accept = "Accept"
        static let authorization = "Authorization"
        static let client = "X-MoniPay-Client"
        static let contentType = "Content-Type"
        static let retryAfter = "Retry-After"
    }

    enum Credential {
        case signUp(String)
        case bearer(String)

        var header: String {
            switch self {
            case .signUp(let token): "SignUp \(token)"
            case .bearer(let token): "Bearer \(token)"
            }
        }
    }

    private let baseURL: URL
    private let session: URLSession
    private let clientVersion: String

    public init(baseURL: URL, session: URLSession = .shared, clientVersion: String = Self.version(of: .main)) {
        self.baseURL = baseURL
        self.session = session
        self.clientVersion = clientVersion
    }

    public static func fromBundle(_ bundle: Bundle = .main) -> ApiClient? {
        guard let raw = bundle.object(forInfoDictionaryKey: "APIBaseURL") as? String,
              let url = URL(string: raw), !raw.isEmpty else { return nil }
        return ApiClient(baseURL: url, clientVersion: version(of: bundle))
    }

    public static func version(of bundle: Bundle) -> String {
        bundle.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "0"
    }

    func send<Response: Decodable, Failure: JsonApiFailure>(
        _ method: Method,
        _ path: String,
        credential: Credential? = nil,
        body: (some Encodable & Sendable)? = Data?.none,
        failing: Failure.Type
    ) async throws(Failure) -> Response {
        let data = try await exchange(method, path, credential: credential, body: body, failing: failing)
        do {
            return try JsonApiCoding.decoder.decode(Response.self, from: data)
        } catch {
            throw Failure.decoding
        }
    }

    @discardableResult
    func exchange<Failure: JsonApiFailure>(
        _ method: Method,
        _ path: String,
        credential: Credential? = nil,
        body: (some Encodable & Sendable)? = Data?.none,
        failing: Failure.Type
    ) async throws(Failure) -> Data {
        var request = URLRequest(url: baseURL.appending(path: path))
        request.httpMethod = method.rawValue
        request.setValue(JsonApiCoding.accept, forHTTPHeaderField: Header.accept)
        request.setValue(clientVersion, forHTTPHeaderField: Header.client)
        if let credential {
            request.setValue(credential.header, forHTTPHeaderField: Header.authorization)
        }
        if let body {
            guard let encoded = try? JsonApiCoding.encoder.encode(body) else { throw Failure.decoding }
            request.setValue(JsonApiCoding.mediaType, forHTTPHeaderField: Header.contentType)
            request.httpBody = encoded
        }
        let data: Data
        let response: URLResponse
        do {
            (data, response) = try await session.data(for: request)
        } catch {
            throw Failure.unreachable
        }
        if let http = response as? HTTPURLResponse, !(200..<300).contains(http.statusCode) {
            throw Failure(try? JsonApiCoding.decoder.decode(ProblemDetails.self, from: data), retryAfter: http.retryAfter)
        }
        return data
    }

    public func signUp(_ body: SignupRequest) async throws -> UserStateDTO {
        try await post("signup", body)
    }

    public func topUp(_ body: TopUpRequest) async throws -> UserStateDTO {
        try await post("topup", body)
    }

    public func createCard(_ body: CardRequest) async throws -> UserStateDTO {
        try await post("card", body)
    }

    public func user(id: String) async throws -> UserStateDTO {
        var components = URLComponents(url: baseURL.appending(path: "user"), resolvingAgainstBaseURL: false)
        components?.queryItems = [URLQueryItem(name: "id", value: id)]
        guard let url = components?.url else { throw ApiError.unreachable }
        return try await send(URLRequest(url: url))
    }

    private func post<Body: Encodable>(_ path: String, _ body: Body) async throws -> UserStateDTO {
        var request = URLRequest(url: baseURL.appending(path: path))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: Header.contentType)
        request.httpBody = try JSONEncoder().encode(body)
        return try await send(request)
    }

    private func send(_ request: URLRequest) async throws -> UserStateDTO {
        let data: Data
        let response: URLResponse
        do {
            (data, response) = try await session.data(for: request)
        } catch {
            throw ApiError.unreachable
        }
        if let http = response as? HTTPURLResponse, !(200..<300).contains(http.statusCode) {
            throw ApiError.status(http.statusCode, body: String(bytes: data, encoding: .utf8) ?? "")
        }
        do {
            return try JSONDecoder().decode(UserStateDTO.self, from: data)
        } catch {
            throw ApiError.decoding(String(describing: error))
        }
    }
}

private extension HTTPURLResponse {
    var retryAfter: Duration? {
        value(forHTTPHeaderField: ApiClient.Header.retryAfter).flatMap(Int.init).map { .seconds($0) }
    }
}
