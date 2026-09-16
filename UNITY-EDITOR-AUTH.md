# Unity desktop editor authentication

Implemented in Agriloco-Deploy. Deploy this backend before updating the Unity desktop editor.
No database migration, shared API key, Azure client secret, or additional account store is required.
The existing Member record, MemberPasswords verifier/upgrade, active farm membership, and login
rate-limit policy are reused. Browser dashboard cookie authentication and Origin checks are unchanged.

## Unity request contract

Base URL: https://agriloco-ggaka4f4d8g7fwbn.canadacentral-01.azurewebsites.net

1. Ask the farmer for their existing dashboard username and password. Send over HTTPS:

POST /api/UnityEditorAuth/login
Content-Type: application/json

{"username":"<dashboard username>","password":"<dashboard password>"}

Success (200, Cache-Control: no-store; no cookie is issued):
{"accessToken":"<opaque token>","tokenType":"Bearer","expiresIn":1800,"farmId":1}

The farmId comes from the authenticated member, never from the login request.
Invalid credentials/inactive member or farm: 401. Invalid input: 400.
Unsupported content type: 415. Rate limit: 429.
HTTP is rejected. A browser Origin, if present, must be same-origin.

2. Keep the token in memory, not in a scene, prefab, PlayerPrefs, source, or logs.
Discard the password after login. Require the returned farmId to match the selected farm
before enabling editing; do not silently apply Farm 1's unsaved geometry to another farm.

3. Add this header to each request below:
Authorization: Bearer <accessToken>

GET /api/UnityMap/draft?farmId=1
POST /api/UnityMap/draft
POST /api/UnityMap/publish?farmId=1

Draft POST still uses the existing JSON contract:
{"farmId":1,"features":[ ... existing unchanged feature/point objects ... ]}
Content-Type: application/json

Publish POST still has no required request body. Save the draft successfully before publishing.
Do not fabricate an Origin header or copy dashboard cookies for bearer requests.

UnityWebRequest:
request.SetRequestHeader("Authorization", "Bearer " + accessToken);

4. On 401, retain unsaved geometry, clear the token and ask the farmer to log in again.
There is no refresh token; tokens expire after 30 minutes. On logout, discard the token.
On 403, stop the operation and show the farm-ownership/reference error; do not retry anonymously.
Do not disable TLS certificate validation or automatically forward the token across redirects.

## Boundaries and revocation

Only the three explicitly opted-in UnityMap draft/publish actions accept editor bearer tokens.
Other map write routes remain protected by their existing member-cookie/ownership checks.
GET /api/UnityMap/farm and GET /api/UnityMap/published remain anonymously accessible and unchanged;
the public WebGL viewer must not be given this login flow or token. Bearer tokens are not
processed by these public GET actions (their existing anonymous data filtering remains).

The token is an ASP.NET Core Data Protection authentication ticket with a dedicated purpose.
Every bearer request checks expiration, current active membership/farm, token farm binding,
and a fingerprint of the current password hash. Deactivation, membership reassignment, and
password changes invalidate existing editor tokens. The bound request farm is then checked
by FarmMapWrite, including existing cross-farm definition validation in SaveDraft.

Use the existing persistent ASP.NET Data Protection key ring across instances, as for member
cookies. Losing or replacing the key ring requires login again. No raw password is in the token.
Individual token revocation is not persisted; logout deletes the client copy and server expiry
bounds its lifetime. No database schema changes were made.

## Verification performed

Backend build passed (only existing application warnings).
Isolated copied SQLite fixture over local HTTPS:
- anonymous farm/published GET 200
- anonymous draft GET/save/publish 401
- invalid credentials 401; valid credentials return farm 1, 1800 seconds, and no cookie
- forged bearer 401; valid cookie cannot rescue an invalid bearer
- cross-farm draft read/save/publish 403
- foreign definition save 403
- editor token rejected on unrelated map write controller (401)
- own draft read/save/publish 200 without Origin/cookies
- published line and two points round-trip intact
- dashboard cookie publish missing/foreign Origin 403; same Origin 200
- foreign-Origin login 403

Tests did not contact Azure or mutate production data. The local test backend was stopped.
Expiry/revocation checks were reviewed in source; the test did not wait 30 minutes.