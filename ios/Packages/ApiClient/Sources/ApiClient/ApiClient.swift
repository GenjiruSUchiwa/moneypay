import Foundation

public struct ApiClient: Sendable {
    enum Method: String {
        case get = "GET"
        case post = "POST"
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
        signUpToken: String? = nil,
        body: (some Encodable & Sendable)? = Data?.none,
        failing: Failure.Type
    ) async throws(Failure) -> Response {
        var request = URLRequest(url: baseURL.appending(path: path))
        request.httpMethod = method.rawValue
        request.setValue(JsonApiCoding.accept, forHTTPHeaderField: "Accept")
        request.setValue(clientVersion, forHTTPHeaderField: "X-MoniPay-Client")
        if let signUpToken {
            request.setValue("SignUp \(signUpToken)", forHTTPHeaderField: "Authorization")
        }
        if let body {
            guard let encoded = try? JsonApiCoding.encoder.encode(body) else { throw Failure.decoding }
            request.setValue(JsonApiCoding.mediaType, forHTTPHeaderField: "Content-Type")
            request.httpBody = encoded
        }
        return try await exchange(request, failing: failing)
    }

    private func exchange<Response: Decodable, Failure: JsonApiFailure>(
        _ request: URLRequest,
        failing: Failure.Type
    ) async throws(Failure) -> Response {
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
        do {
            return try JsonApiCoding.decoder.decode(Response.self, from: data)
        } catch {
            throw Failure.decoding
        }
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
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
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
        value(forHTTPHeaderField: "Retry-After").flatMap(Int.init).map { .seconds($0) }
    }
}
