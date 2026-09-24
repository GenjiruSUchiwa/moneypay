import Platform

nonisolated public enum SessionStoreError: Error, Sendable, Equatable {
    case noSession
    case session(SessionError)
    case credentialStorage(CredentialStoreError)

    var endsSession: Bool {
        self == .session(.sessionInvalid) || self == .session(.refreshTokenReused)
    }
}
