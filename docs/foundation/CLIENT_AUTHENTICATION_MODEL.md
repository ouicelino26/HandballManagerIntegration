# Client authentication model

## Decision

HandWStat Administration is a distributed WPF desktop application and therefore a public client. It does not contain, request, persist, or transmit an application client secret.

Authentication uses the existing user login endpoint. The API returns a short-lived JWT together with the administrative role and permission claims. Server authorization remains authoritative for every protected operation.

## Session lifecycle

1. The login view sends only the entered username and password over the configured API transport.
2. `ApiAuthService` validates the response and gives the JWT to `IAdminSessionService`.
3. `MemoryAdminSessionStorage` keeps the session in process memory only.
4. `AdminSessionHandler` attaches the bearer token to administrative API requests.
5. An expired local token or an HTTP 401 clears the session and requires a new login.
6. An HTTP 403 preserves the authenticated session and is displayed as a permission error.
7. Logout clears both the legacy authentication state and the central administrative session.

No refresh-token flow is claimed because the API does not currently expose one. No token or raw authentication response may be written to the UI or logs.

## Configuration

`ApiSettings` contains only the API base URL, application identity, environment label, and request timeout. The technical URL is not shown on the login screen. Environment and API availability are shown in the authenticated shell without exposing credentials.

## Known manual action

The historical shared secret exposure is documented separately. Rotation remains manual and mandatory before any production publication, even though the current WPF tree no longer requires that secret.
