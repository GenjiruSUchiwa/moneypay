# ADR 0001: JSON:API documents for success, Problem Details for errors

Status: Proposed
Date: 2026-08-29
Scope: `server/`, `docs/api/openapi.json`, `ios/Packages/ApiClient`

## Context

The sign-up design is the first HTTP surface with resources, relationships, and many refusal cases. The repository already requires ASP.NET Core `ProblemDetails` for errors. JSON:API 1.1 defines its own `errors` document, so strict JSON:API compliance and the existing error rule conflict.

## Decision

Use JSON:API 1.1 resource documents with `application/vnd.api+json` for every successful request and response body. Use RFC 9457 Problem Details with `application/problem+json` for every error body.

The API states this hybrid explicitly. It does not claim strict JSON:API compliance.

No JSON:API package is added. The document records live in `MoniPay.Kernel/Http/` and are small.

## Consequences

- One error format across every module: `type` is the stable machine code, `title` and `detail` are localized, `traceId` is always present.
- Every slice owns its resource and attribute records, so OpenAPI schemas stay exact.
- Clients send `Accept: application/vnd.api+json, application/problem+json`; anything else gets `406` or `415`.
- The iOS `ApiClient` decodes two envelopes, not one. The fixtures used by the server contract tests are shared with the Swift tests.
- A future tool that expects JSON:API `errors` documents will not work without an adapter. That cost is accepted.

## Alternatives rejected

- Strict JSON:API, including `errors`: breaks the repository-wide Problem Details rule and loses `traceId` and validation pointers as first-class members.
- Plain JSON records without an envelope: loses relationships and self links, and leaves the shape of each resource to convention rather than contract.

## References

- [API representation standards](../architecture/authentication/api-standards.md)
- [JSON:API 1.1](https://jsonapi.org/format/1.1/)
- [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html)
