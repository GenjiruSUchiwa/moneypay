import Foundation

/// In-memory `AccountCreating` for previews, the `Gallery` catalog, and the app when
/// `AppConfiguration.api` is nil (empty `APIBaseURL`). It answers whatever it was
/// built with and never touches the network.
public struct PreviewAccountClient: AccountCreating {
    /// A canned answer shaped like the POC's, with a fictional sandbox number.
    public static let sampleUser = UserStateDTO(
        id: "u_preview",
        name: "Aristide Mbassi",
        phone: "237600000001",
        email: "aristide@example.cm",
        balanceFcfa: 0,
        cards: []
    )

    private let delay: Duration
    private let result: Result<UserStateDTO, ApiError>

    public init(
        delay: Duration = .zero,
        result: Result<UserStateDTO, ApiError> = .success(PreviewAccountClient.sampleUser)
    ) {
        self.delay = delay
        self.result = result
    }

    /// The request is deliberately ignored — this is a stub, not a fake server.
    public func createAccount(_: SignupRequest) async throws -> UserStateDTO {
        if delay > .zero { try? await Task.sleep(for: delay) }
        return try result.get()
    }
}
