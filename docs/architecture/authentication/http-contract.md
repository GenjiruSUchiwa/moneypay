# Sign-up HTTP contract

Status: Proposed

## Representation contract

Successful bodies use JSON:API 1.1 resource documents with `application/vnd.api+json`.

Error bodies use RFC 9457 Problem Details with `application/problem+json`.

This hybrid is intentional. The API does not claim strict JSON:API compliance for errors.

Read [API representation standards](api-standards.md) before implementing a slice.

## Route inventory

| Method | Route | Vertical slice | Authorization | Success |
|---|---|---|---|---|
| `POST` | `/signups` | `SignUps/Start` | Anonymous, rate limited | `202 Accepted` |
| `GET` | `/signups/{signUpId:guid}` | `SignUps/Get` | Sign-up or Registration token | `200 OK` |
| `POST` | `/signups/{signUpId:guid}/verification-code-deliveries` | `SignUps/ResendCode` | Sign-up token, rate limited | `202 Accepted` |
| `POST` | `/signups/{signUpId:guid}/phone-verifications` | `SignUps/VerifyPhone` | Sign-up token, rate limited | `200 OK` |
| `POST` | `/signups/{signUpId:guid}/completions` | `SignUps/Complete` | Registration token | `201 Created` or `200 OK` on retry |
| `POST` | `/session-refreshes` | `Sessions/Refresh` | Refresh token in body, rate limited | `200 OK` |
| `GET` | `/sessions/current` | `Sessions/GetCurrent` | Bearer token | `200 OK` |
| `DELETE` | `/sessions/current` | `Sessions/RevokeCurrent` | Bearer token | `204 No Content` |
| `GET` | `/users/me` | `Users/CurrentUser` | Bearer token | `200 OK` |

The .NET API does not add a compatibility `POST /signup` route. The POC stays active until the iOS client and .NET contract change together.

## Headers

| Header | Use |
|---|---|
| `Accept` | Request JSON:API success and Problem Details error media types. |
| `Content-Type` | Identify a JSON:API request document. |
| `Accept-Language` | Select localized errors and the stored user locale. |
| `Authorization` | Carry a Sign-up, Registration, or Bearer credential. |
| `X-MoniPay-Client` | Carry the app version for compatibility checks. |
| `Traceparent` | Carry the distributed trace context. |

Clients send:

```http
Accept: application/vnd.api+json, application/problem+json
Content-Type: application/vnd.api+json
```

Every sign-up and session response also sends:

```http
Cache-Control: no-store
Pragma: no-cache
```

## Start sign-up

`POST /signups`

Operation name: `StartSignUp`

Request:

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

The phone is digits-only E.164 data. It has no leading plus sign and no spaces.

Response:

```json
{
  "jsonapi": {
    "version": "1.1"
  },
  "data": {
    "type": "signups",
    "id": "1f7d854a-3996-4a6b-8cc8-1fe3e83bc205",
    "attributes": {
      "status": "codePending",
      "codeDelivery": "queued",
      "signUpToken": "<opaque-random-token>",
      "codeExpiresAt": "2026-08-29T19:05:00Z",
      "canResendAt": "2026-08-29T19:01:00Z",
      "signUpExpiresAt": "2026-08-29T19:15:00Z"
    },
    "links": {
      "self": "/signups/1f7d854a-3996-4a6b-8cc8-1fe3e83bc205"
    }
  }
}
```

The response has the same shape for a new phone and an existing phone. This rule prevents account enumeration before phone verification.

The sign-up token is a high-entropy bearer credential. It appears only in this response and never in a later GET response.

## Get sign-up state

`GET /signups/{signUpId:guid}`

Operation name: `GetSignUp`

Use one credential:

```http
Authorization: SignUp <sign-up-token>
```

or:

```http
Authorization: Registration <registration-token>
```

The response contains the `signups` resource with status and timing attributes. It never returns a sign-up token or registration token.

`codeDelivery` reports the last verification-code delivery: `queued`, `sent`, `failed`, or `expired`. The code is delivered by a background worker after the request commits, so the client polls this route when no SMS arrives before it offers a resend. See [Notification architecture](notifications.md).

This route supports mobile recovery after an app interruption.

## Resend the code

`POST /signups/{signUpId:guid}/verification-code-deliveries`

Operation name: `CreateVerificationCodeDelivery`

Header:

```http
Authorization: SignUp <sign-up-token>
```

The request has no body.

The response contains the updated `signups` resource. It updates `codeExpiresAt` and `canResendAt`.

A resend replaces the prior code. It does not reset failed attempts or extend the sign-up lifetime.

## Verify the phone

`POST /signups/{signUpId:guid}/phone-verifications`

Operation name: `CreatePhoneVerification`

Header:

```http
Authorization: SignUp <sign-up-token>
```

Request:

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

Response:

```json
{
  "jsonapi": {
    "version": "1.1"
  },
  "data": {
    "type": "signups",
    "id": "1f7d854a-3996-4a6b-8cc8-1fe3e83bc205",
    "attributes": {
      "status": "phoneVerified",
      "registrationToken": "<opaque-random-token>",
      "signUpExpiresAt": "2026-08-29T19:15:00Z"
    },
    "links": {
      "self": "/signups/1f7d854a-3996-4a6b-8cc8-1fe3e83bc205"
    }
  }
}
```

Successful phone verification invalidates the sign-up token. The registration token appears only in this response.

If the verified phone already belongs to a user, the route returns `409 Conflict`. Revealing this fact is safe after phone control is proven.

## Complete sign-up

`POST /signups/{signUpId:guid}/completions`

Operation name: `CreateSignUpCompletion`

Header:

```http
Authorization: Registration <registration-token>
```

Request:

```json
{
  "data": {
    "type": "signup-completions",
    "attributes": {
      "firstName": "Aristide",
      "lastName": "Mbassi",
      "email": "aristide@example.cm",
      "deviceId": "1207158c-15fc-446d-a28a-702c564332ef"
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

The request does not contain a passcode, biometric flag, KYC field, or card-provider field.

Response:

```json
{
  "jsonapi": {
    "version": "1.1"
  },
  "data": {
    "type": "sessions",
    "id": "4cd89435-dcc2-45ce-804b-eafc1ad4b553",
    "attributes": {
      "tokenType": "Bearer",
      "accessToken": "<jwt>",
      "accessTokenExpiresAt": "2026-08-29T19:12:00Z",
      "refreshToken": "<opaque-random-token>",
      "refreshTokenExpiresAt": "2026-09-28T19:02:00Z"
    },
    "relationships": {
      "user": {
        "links": {
          "related": "/users/me"
        },
        "data": {
          "type": "users",
          "id": "22961289-422c-4b91-a5eb-1365b1e7c67b"
        }
      }
    },
    "links": {
      "self": "/sessions/current"
    }
  }
}
```

The first completion returns `201 Created`. The `Location` header is `/sessions/current`.

A safe retry with the same valid registration token returns `200 OK`. The backend revokes the prior bootstrap session and returns a new session.

## Refresh the session

`POST /session-refreshes`

Operation name: `CreateSessionRefresh`

Request:

```json
{
  "data": {
    "type": "session-refreshes",
    "attributes": {
      "refreshToken": "<opaque-random-token>",
      "deviceId": "1207158c-15fc-446d-a28a-702c564332ef"
    }
  }
}
```

The response contains the `sessions` resource with new access and refresh credentials.

A successful refresh consumes the old refresh token. Reuse revokes the complete token family.

## Get the current session

`GET /sessions/current`

Operation name: `GetCurrentSession`

The response contains a `sessions` resource without raw access or refresh tokens.

Attributes include:

- `createdAt`
- `lastSeenAt`
- `accessTokenExpiresAt`

The user relationship points to `/users/me`.

## Revoke the current session

`DELETE /sessions/current`

Operation name: `DeleteCurrentSession`

The endpoint reads `sid` from the bearer token. It marks the session revoked and returns `204 No Content` with no body.

## Get the current user

`GET /users/me`

Operation name: `GetCurrentUser`

Response:

```json
{
  "jsonapi": {
    "version": "1.1"
  },
  "data": {
    "type": "users",
    "id": "22961289-422c-4b91-a5eb-1365b1e7c67b",
    "attributes": {
      "firstName": "Aristide",
      "lastName": "Mbassi",
      "phone": "237699123456",
      "email": "aristide@example.cm",
      "locale": "fr-CM",
      "createdAt": "2026-08-29T19:02:00Z"
    },
    "links": {
      "self": "/users/me"
    }
  }
}
```

This contract has no KYC attribute. The KYC status contract remains blocked by the KYC product decision.

## Problem Details contract

Example:

```json
{
  "type": "urn:monipay:error:verification-code-invalid",
  "title": "The verification code is invalid.",
  "status": 422,
  "instance": "/signups/1f7d854a-3996-4a6b-8cc8-1fe3e83bc205/phone-verifications",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    {
      "detail": "The verification code does not match.",
      "pointer": "/data/attributes/verificationCode"
    }
  ]
}
```

The French title and detail live in `SessionMessages.fr.resx`. C# source contains English neutral resources and stable URNs only.

| Problem type suffix | Status | Meaning |
|---|---:|---|
| `malformed-json` | `400` | The body is not valid JSON. |
| `jsonapi-document-invalid` | `400` | The JSON:API document shape is invalid. |
| `not-acceptable` | `406` | The client accepts no supported response type. |
| `unsupported-media-type` | `415` | The request media type is unsupported. |
| `validation` | `422` | One or more attributes are invalid. |
| `resource-identity-mismatch` | `409` | The route and relationship identifiers differ. |
| `signup-state-invalid` | `409` | The sign-up is not in a state that allows this action. |
| `concurrent-modification` | `409` | Another request changed the resource first. Read it again and retry. |
| `rate-limited` | `429` | An IP or phone limit was reached. |
| `verification-code-invalid` | `422` | The code does not match. |
| `verification-code-expired` | `410` | The current code expired. A resend remains possible. |
| `signup-expired` | `410` | The sign-up lifetime ended. |
| `signup-attempt-limit` | `429` | The code-attempt limit was reached. |
| `signup-resend-limit` | `429` | The resend limit was reached. |
| `signup-resend-too-soon` | `429` | The resend cooldown has not elapsed. |
| `signup-token-invalid` | `401` | The Sign-up credential is invalid or expired. |
| `phone-already-registered` | `409` | A verified phone already belongs to a user. |
| `email-already-registered` | `409` | The normalized email already belongs to a user. |
| `registration-token-invalid` | `401` | The completion credential is invalid or expired. |
| `session-invalid` | `401` | The access or refresh credential is invalid. |
| `refresh-token-reused` | `401` | A consumed refresh token was used again. |
| `verification-delivery-unavailable` | `503` | Delivery could not be queued. The sign-up transaction was rolled back. |
| `internal` | `500` | An unexpected server failure occurred. |

Every `429` response includes `Retry-After` when the backend knows the remaining delay.

## Constants

`MoniPay.Sessions` declares resource, route, operation, tag, summary, policy, and rate-limit constants beside each feature group.

`MoniPay.Users` declares the same constants beside the Current User slice.

Cross-cutting values live in `MoniPay.Kernel`:

- `MoniPayHeaders`
- `MoniPayPolicies`
- `MoniPayClaimTypes`
- `MoniPayErrorTypes`
- `MoniPayMediaTypes`

Tests and endpoint metadata reference these constants. They do not repeat string literals.
