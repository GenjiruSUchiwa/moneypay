import Foundation

public struct PreviewAccountClient: AccountCreating {
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

    public func createAccount(_: SignupRequest) async throws -> UserStateDTO {
        if delay > .zero { try? await Task.sleep(for: delay) }
        return try result.get()
    }
}
