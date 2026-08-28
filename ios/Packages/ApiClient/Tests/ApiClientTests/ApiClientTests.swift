import Foundation
import Testing

@testable import ApiClient

@Suite("ApiClient")
struct ApiClientTests {

    @Test("DTOs use the snake_case keys of the POC")
    func encodesSnakeCaseKeys() throws {
        let json = try JSONEncoder().encode(TopUpRequest(userId: "u1", amountFcfa: 25))
        let text = String(bytes: json, encoding: .utf8) ?? ""
        #expect(text.contains("\"user_id\""))
        #expect(text.contains("\"amount_fcfa\""))
    }

    @Test("A POC response decodes into UserStateDTO")
    func decodesUserState() throws {
        let payload = Data(#"{"id":"u1","name":"Awa","balance_fcfa":20000,"cards":[]}"#.utf8)
        let user = try JSONDecoder().decode(UserStateDTO.self, from: payload)
        #expect(user.id == "u1")
        #expect(user.balanceFcfa == 20_000)
        #expect(user.cards?.isEmpty == true)
    }

    @Test("Without an APIBaseURL key, no client is built")
    func noClientWithoutBaseURL() {
        #expect(ApiClient.fromBundle(Bundle(for: Marker.self)) == nil)
    }
}

private final class Marker {}
