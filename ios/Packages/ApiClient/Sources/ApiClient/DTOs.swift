import Foundation

public struct SignupRequest: Codable, Sendable, Hashable {
    public var name: String
    public var phone: String
    public var email: String

    public init(name: String, phone: String, email: String) {
        self.name = name
        self.phone = phone
        self.email = email
    }
}

public struct TopUpRequest: Codable, Sendable, Hashable {
    public var userId: String
    public var amountFcfa: Int

    public init(userId: String, amountFcfa: Int) {
        self.userId = userId
        self.amountFcfa = amountFcfa
    }

    public enum CodingKeys: String, CodingKey, Sendable {
        case userId = "user_id"
        case amountFcfa = "amount_fcfa"
    }
}

public struct CardRequest: Codable, Sendable, Hashable {
    public var userId: String
    public var amountUsd: Int

    public init(userId: String, amountUsd: Int) {
        self.userId = userId
        self.amountUsd = amountUsd
    }

    public enum CodingKeys: String, CodingKey, Sendable {
        case userId = "user_id"
        case amountUsd = "amount_usd"
    }
}

public struct UserStateDTO: Codable, Sendable, Hashable {
    public init(id: String, name: String?, phone: String?, email: String?, balanceFcfa: Int?, cards: [CardDTO]?) {
        self.id = id
        self.name = name
        self.phone = phone
        self.email = email
        self.balanceFcfa = balanceFcfa
        self.cards = cards
    }

    public var id: String
    public var name: String?
    public var phone: String?
    public var email: String?
    public var balanceFcfa: Int?
    public var cards: [CardDTO]?

    public enum CodingKeys: String, CodingKey, Sendable {
        case id, name, phone, email, cards
        case balanceFcfa = "balance_fcfa"
    }
}

public struct CardDTO: Codable, Sendable, Hashable {
    public init(id: String, last4: String?, brand: String?, currency: String?, balanceUsd: Double?) {
        self.id = id
        self.last4 = last4
        self.brand = brand
        self.currency = currency
        self.balanceUsd = balanceUsd
    }

    public var id: String
    public var last4: String?
    public var brand: String?
    public var currency: String?
    public var balanceUsd: Double?

    public enum CodingKeys: String, CodingKey, Sendable {
        case id, last4, brand, currency
        case balanceUsd = "balance_usd"
    }
}

public enum ApiError: Error, Sendable, Equatable {
    case unreachable
    case status(Int, body: String)
    case decoding(String)
}
