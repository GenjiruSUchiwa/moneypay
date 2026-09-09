import Foundation
import Testing

@testable import ApiClient

@Suite("AccountCreating")
struct AccountCreatingTests {

    @Test("The preview client returns its canned user")
    func returnsCannedUser() async throws {
        let user = try await PreviewAccountClient().createAccount(Self.request)
        #expect(user.id == "u_preview")
    }

    @Test("The preview client returns the user it was built with")
    func returnsInjectedUser() async throws {
        let expected = UserStateDTO(
            id: "u_custom",
            name: "Awa Ndongo",
            phone: "237600000002",
            email: "awa@example.cm",
            balanceFcfa: 20_000,
            cards: []
        )
        let client = PreviewAccountClient(result: .success(expected))
        #expect(try await client.createAccount(Self.request) == expected)
    }

    @Test("The preview client throws the failure it was built with")
    func throwsInjectedFailure() async {
        let client = PreviewAccountClient(result: .failure(.unreachable))
        await #expect(throws: ApiError.unreachable) {
            try await client.createAccount(Self.request)
        }
    }

    @Test("A delayed preview client still answers")
    func delayedClientStillAnswers() async throws {
        let client = PreviewAccountClient(delay: .milliseconds(1))
        let user = try await client.createAccount(Self.request)
        #expect(user.id == "u_preview")
    }

    @Test("A signup request carries name, phone and email as the POC expects")
    func requestCarriesPOCFields() throws {
        let json = try JSONEncoder().encode(Self.request)
        let text = String(bytes: json, encoding: .utf8) ?? ""
        #expect(text.contains("\"name\""))
        #expect(text.contains("\"phone\""))
        #expect(text.contains("\"email\""))
        #expect(!text.contains("+"))
    }

    @Test("ApiClient satisfies AccountCreating")
    func apiClientConforms() throws {
        let url = try #require(URL(string: "https://example.invalid"))
        let accounts: any AccountCreating = ApiClient(baseURL: url)
        #expect(accounts is ApiClient)
    }

    private static let request = SignupRequest(
        name: "Aristide Mbassi",
        phone: "237600000001",
        email: "a@example.cm"
    )
}
