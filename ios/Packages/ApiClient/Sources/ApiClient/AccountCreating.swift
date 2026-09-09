import Foundation

public protocol AccountCreating: Sendable {
    func createAccount(_ request: SignupRequest) async throws -> UserStateDTO
}

extension ApiClient: AccountCreating {
    public func createAccount(_ request: SignupRequest) async throws -> UserStateDTO {
        try await signUp(request)
    }
}
