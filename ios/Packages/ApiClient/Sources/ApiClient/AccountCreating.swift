import Foundation

/// Creating a MoniPay account. The sign-up flow depends on this, never on `ApiClient`:
/// swapping the POC for `server/` must not touch a single view.
public protocol AccountCreating: Sendable {
    func createAccount(_ request: SignupRequest) async throws -> UserStateDTO
}

extension ApiClient: AccountCreating {
    /// `POST /signup` — see `poc/README.md`. Untyped `throws`: `send(_:)` maps transport and
    /// status failures onto `ApiError`, but `post(_:_:)` also propagates the encoder's
    /// `EncodingError`, so `throws(ApiError)` would not compile.
    public func createAccount(_ request: SignupRequest) async throws -> UserStateDTO {
        try await signUp(request)
    }
}
