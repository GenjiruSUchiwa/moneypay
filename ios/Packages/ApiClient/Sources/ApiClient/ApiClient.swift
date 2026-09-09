import Foundation

public struct ApiClient: Sendable {
    private let baseURL: URL
    private let session: URLSession

    public init(baseURL: URL, session: URLSession = .shared) {
        self.baseURL = baseURL
        self.session = session
    }

    public static func fromBundle(_ bundle: Bundle = .main) -> ApiClient? {
        guard let raw = bundle.object(forInfoDictionaryKey: "APIBaseURL") as? String,
              let url = URL(string: raw), !raw.isEmpty else { return nil }
        return ApiClient(baseURL: url)
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
