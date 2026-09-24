import Foundation

nonisolated struct JsonApiRequest<Attributes: Codable & Sendable & Hashable>: Codable, Sendable, Hashable {
    struct Resource: Codable, Sendable, Hashable {
        let type: String
        let attributes: Attributes
        let relationships: [String: JsonApiRelationship]?
    }

    let data: Resource

    init(type: String, attributes: Attributes, relationships: [String: JsonApiRelationship]? = nil) {
        data = Resource(type: type, attributes: attributes, relationships: relationships)
    }
}

nonisolated struct JsonApiResponse<Attributes: Codable & Sendable & Hashable>: Codable, Sendable, Hashable {
    let data: JsonApiResource<Attributes>
}

nonisolated struct JsonApiResource<Attributes: Codable & Sendable & Hashable>: Codable, Sendable, Hashable {
    let type: String
    let id: String
    let attributes: Attributes
    let relationships: [String: JsonApiRelationship]?

    func relatedId(_ name: String) -> String? {
        relationships?[name]?.data.id
    }
}

nonisolated struct JsonApiRelationship: Codable, Sendable, Hashable {
    let data: JsonApiResourceIdentifier
}

nonisolated struct JsonApiResourceIdentifier: Codable, Sendable, Hashable {
    let type: String
    let id: String
}

nonisolated enum JsonApiCoding {
    static let mediaType = "application/vnd.api+json"
    static let problemMediaType = "application/problem+json"
    static let accept = "\(mediaType), \(problemMediaType)"

    private static let timestamp = Date.ISO8601FormatStyle(includingFractionalSeconds: true)

    static let decoder: JSONDecoder = {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .custom { decoder in
            let container = try decoder.singleValueContainer()
            let raw = try container.decode(String.self)
            do {
                return try timestamp.parse(raw)
            } catch {
                throw DecodingError.dataCorruptedError(in: container, debugDescription: "Invalid ISO-8601 timestamp")
            }
        }
        return decoder
    }()

    static let encoder: JSONEncoder = {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .custom { date, encoder in
            var container = encoder.singleValueContainer()
            try container.encode(date.formatted(timestamp))
        }
        return encoder
    }()
}
