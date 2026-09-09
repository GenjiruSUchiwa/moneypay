import Foundation
import OSLog

public enum LogLevel: String, Sendable, CaseIterable {
    case debug, info, error
}

public protocol Logging: Sendable {
    func log(_ level: LogLevel, _ message: String)
}

public struct OSLogging: Logging {
    private let logger: Logger

    public init(subsystem: String = "com.monipay.app", category: String) {
        self.logger = Logger(subsystem: subsystem, category: category)
    }

    public func log(_ level: LogLevel, _ message: String) {
        switch level {
        case .debug: logger.debug("\(message, privacy: .public)")
        case .info: logger.info("\(message, privacy: .public)")
        case .error: logger.error("\(message, privacy: .public)")
        }
    }
}
