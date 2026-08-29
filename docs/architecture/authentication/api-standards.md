# API representation standards

Status: Proposed

## Decision

Use JSON:API 1.1 resource documents for successful request and response bodies. Use RFC 9457 Problem Details for every error body.

This is a deliberate hybrid. It is not strict JSON:API compliance because JSON:API defines its own `errors` document.

The repository already requires ASP.NET Core `ProblemDetails`. The hybrid keeps one error format across every MoniPay module.

Do not claim that the complete API is JSON:API compliant. State this contract precisely:

> MoniPay uses JSON:API 1.1 resource documents for success and RFC 9457 Problem Details for errors.

## Media types

Successful request and response documents use:

```http
Content-Type: application/vnd.api+json
```

Error documents use:

```http
Content-Type: application/problem+json
```

Clients send:

```http
Accept: application/vnd.api+json, application/problem+json
```

A request with a body must use `application/vnd.api+json`. Return `415 Unsupported Media Type` for another request media type.

Return `415` if the JSON:API media type has unsupported parameters. JSON:API permits only `ext` and `profile` parameters.

Return `406 Not Acceptable` when the client accepts neither supported response media type.

Do not append a `charset` parameter to `application/vnd.api+json`.

## JSON:API success document

A successful document has these members:

```json
{
  "jsonapi": {
    "version": "1.1"
  },
  "data": {
    "type": "signups",
    "id": "1f7d854a-3996-4a6b-8cc8-1fe3e83bc205",
    "attributes": {}
  },
  "links": {
    "self": "/signups/1f7d854a-3996-4a6b-8cc8-1fe3e83bc205"
  }
}
```

Rules:

- `data` and an error body never coexist.
- `type` and `id` are strings.
- Resource types use lowercase plural words.
- JSON member names use `camelCase`.
- Resource identifiers stay in `id`. Do not repeat them inside `attributes`.
- Foreign resource identifiers use `relationships`. Do not use attributes such as `userId` in a resource representation.
- `links.self` appears only when the URI can identify that representation.
- Secrets can appear in a creation response once. They never appear in later reads.
- Timestamps use RFC 3339 UTC values through `DateTimeOffset`.
- Money uses integer minor units and an ISO currency code.
- Empty optional relationships use `data: null`.

The sign-up contract uses these resource types:

- `signups`
- `verification-code-deliveries`
- `phone-verifications`
- `signup-completions`
- `sessions`
- `session-refreshes`
- `users`

## JSON:API request document

A request wraps its resource in `data`:

```json
{
  "data": {
    "type": "signups",
    "attributes": {
      "phone": "237699123456",
      "termsVersion": "terms-2026-08",
      "privacyVersion": "privacy-2026-08"
    }
  }
}
```

For a command against an existing sign-up, the request resource includes the route identifier:

```json
{
  "data": {
    "type": "phone-verifications",
    "attributes": {
      "verificationCode": "123456"
    },
    "relationships": {
      "signUp": {
        "data": {
          "type": "signups",
          "id": "1f7d854a-3996-4a6b-8cc8-1fe3e83bc205"
        }
      }
    }
  }
}
```

The route identifier and relationship identifier must match. A mismatch returns `409 Conflict` with `resource-identity-mismatch`.

Unknown attributes return `400 Bad Request`. The API does not ignore misspelled security-sensitive fields.

## Problem Details error document

Use RFC 9457 members:

- `type`: Absolute problem-type URI or URN. This is the stable machine code.
- `status`: HTTP status code.
- `title`: Short localized summary for the problem type.
- `detail`: Localized detail for this occurrence, when safe.
- `instance`: The request path or an opaque occurrence URI.

Add these extension members:

- `traceId`: The server trace identifier.
- `errors`: Validation items for one validation problem.

Example:

```json
{
  "type": "urn:monipay:error:validation",
  "title": "The request is not valid.",
  "status": 422,
  "instance": "/signups",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    {
      "detail": "The phone number has an invalid length.",
      "pointer": "/data/attributes/phone"
    }
  ]
}
```

A validation item has:

- `detail`: Localized safe text.
- `pointer`: JSON Pointer to the invalid request member.

Do not add stack traces, exception messages, provider payloads, phone numbers, email addresses, or credential values.

## Status rules

| Condition | Status |
|---|---:|
| Malformed JSON | `400 Bad Request` |
| Invalid JSON:API document shape | `400 Bad Request` |
| Unsupported request media type | `415 Unsupported Media Type` |
| Unsupported response media type | `406 Not Acceptable` |
| Valid document with invalid attributes | `422 Unprocessable Content` |
| Missing or invalid credential | `401 Unauthorized` |
| Authenticated principal lacks the policy | `403 Forbidden` |
| Resource identity mismatch | `409 Conflict` |
| Uniqueness or state conflict | `409 Conflict` |
| Expired sign-up resource | `410 Gone` |
| Rate or attempt limit | `429 Too Many Requests` |
| Provider unavailable | `503 Service Unavailable` |

Use `404 Not Found` only when resource existence is safe to reveal. Authentication errors must not become `404` workarounds.

## ASP.NET Core implementation

Add transport-only records to `MoniPay.Kernel/Http/`:

- `JsonApiRequest<TData>`
- `JsonApiResponse<TData>`
- `JsonApiVersion`
- `JsonApiLinks`
- `JsonApiResourceIdentifier`
- `JsonApiRelationship`

These records contain no business rule and no ASP.NET Core dependency.

Each vertical slice owns its resource and attribute records. For example, the Start Sign-up slice owns `CreateSignUpResource` and `CreateSignUpAttributes`.

Add host error infrastructure under `MoniPay.Api/Errors/`:

- `MoniPayExceptionHandler`
- `MoniPayProblemDetailsWriter`
- `ValidationProblemItem`

`MoniPayExceptionHandler` maps domain exceptions to status and problem type. It does not expose exception messages.

`MoniPayProblemDetailsWriter` sets the media type, localization, `traceId`, and validation pointers.

Do not add a JSON:API NuGet package. The required document records are small, and no current package is approved.

## OpenAPI rules

For each route, OpenAPI declares:

- `application/vnd.api+json` for every success request and response body.
- `application/problem+json` for every documented error.
- Exact resource document schemas, not `object`.
- The route operation name from a module constant.
- Every possible status from the route contract.
- Security schemes for Sign-up, Registration, and Bearer tokens.

The generated OpenAPI document is the iOS contract. Regenerate it after each route or representation change.

## Contract tests

Each HTTP slice test asserts:

- Exact success `Content-Type` without `charset`.
- Exact error `Content-Type`.
- `jsonapi.version` equals `1.1`.
- Resource `type` and `id` are strings.
- Route and relationship identifiers match.
- Error `type`, `status`, `instance`, and `traceId` exist.
- Validation pointers name the correct JSON member.
- Unsupported content negotiation returns `406` or `415`.
- OpenAPI lists both media types and exact schemas.

## Standards

- [JSON:API 1.1](https://jsonapi.org/format/1.1/)
- [RFC 9457, Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457.html)
- [ASP.NET Core error handling](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/handle-errors)
