# Map write security review

Validated locally on September 9, 2026. No Git operations, remote SQL changes, Azure deployment, DNS changes, or Unity source changes were performed.

## A. Map write endpoints found

All of the following now require an authenticated active Member authorized for the target farm:

| Route | Operation |
| --- | --- |
| POST `/api/UnityMap/draft` | Replace draft geometry/features/points |
| POST `/api/UnityMap/publish?farmId=...` | Replace published map from draft |
| POST `/api/Map/cells` | Create/update legacy map cell |
| DELETE `/api/Map/cells?farmId=...&gridX=...&gridY=...` | Delete legacy geometry |
| POST `/api/Farms/{id}/map-layout` | Save legacy layout JSON |
| POST `/api/Farms/{id}/geocode-address` | Change farm coordinates |
| POST `/Farmer/MapImage` | Upload/replace basemap |
| POST `/Farmer/GridPainter` | Save a legacy cell through the API |
| POST `/Farmer/Dashboard?handler=Basemap` | Upload/replace basemap |
| POST `/Farmer/Dashboard?handler=Status`, `Channel`, `Public` | Change live map definition availability/visibility |
| POST `/Farmer/Add?handler=Create`, `Edit`, `Delete` | Create/edit/delete farm definitions and branches used by maps |

No separate geometry delete route exists in UnityMap: draft replacement and publication delete old features internally. Those paths are covered by the entry-point checks. The disabled AddMapCell page has no write handler.

## B. Previous exposure

There was no configured login, authentication middleware, or ownership check at map write entry points. A caller could supply another farm ID and replace or publish its map. Registration also stored UTF-8 password bytes in PasswordHash, with an empty salt.

## C. Authentication protection added

The user approved adding minimal Member login after inspection found no existing authentication system.

- `/Farmer/Login` validates an existing active Member and active Farm, then creates an ASP.NET Core protected cookie. `/Farmer/Logout` signs out via POST.
- Cookie: `__Host-Agriloco.Member`, Secure, HttpOnly, SameSite Strict, eight-hour lifetime, no sliding renewal. Use HTTPS, including for local farmer login.
- Login/logout retain Razor antiforgery validation. Login is rate limited to ten requests per minute per remote IP (including the login GET).
- New registrations use ASP.NET Core PasswordHasher. A successful legacy login upgrades the old empty-salt plaintext representation to a version-marked password hash. Unknown salted legacy formats fail closed. No real account credentials were changed during testing.
- Map-write authorization runs before model binding/antiforgery failures: anonymous callers receive 401. Authenticated write requests require a matching Origin header; missing/foreign origins receive 403. Razor POSTs additionally require antiforgery tokens.
- Shared navigation includes login/sign-out links. Existing farmer GET pages were not globally restricted.

## D. Ownership

The protected cookie contains Member.Id, not a trusted browser-supplied FarmId. Every protected write queries the database to require an active Member, matching Member.FarmId, and active Farm. Wrong-farm or inactive ownership returns 403. Invalid farm IDs return 400 for authenticated same-origin callers.

There is no admin role or multi-farm permission model in this repository, so no bypass was invented. Draft saves additionally verify that every positive FarmDefinitionId and LabelSourceFarmDefinitionId belongs to the authorized farm before deleting any existing geometry. Existing legacy cell crop ownership checks remain intact. Nested incoming feature IDs do not select records to update: features are recreated under the server-selected map.

## E. Anonymous public access

GET `/api/UnityMap/farm?farmId=1`, GET `/api/UnityMap/published?farmId=1`, `/viewer/?farmId=1`, basemap assets, and existing public legacy map GETs remain anonymous. Public availability subscription behavior was not changed. This task did not add a draft-read privacy policy; GET draft remains as previously implemented.

## F. Unity Editor impact

No Unity files or public Viewer build files were modified. Same-origin browser WebGL requests inherit the login cookie and browser Origin header; no API key or hard-coded credential was introduced. GridPainter's server-side API call forwards the current request cookie only on that request to the configured application origin. Its HTTP handler disables cookie storage and redirects so pooled clients cannot retain another farmer's login or redirect credentials elsewhere.

The source `C:\Users\pete2\My project (9)\Assets\Scripts\MapPersistenceController.cs` still defaults to `https://localhost:44389/api/UnityMap`. It sends draft JSON and an empty publish POST without its own login mechanism. Before using that Editor in production, make its API root derive from the browser's HTTPS origin (as the public Viewer already does), host it on the authenticated website's origin, and select the farm from the launch URL, including `?farmId=1` for Farm 1. The server remains authoritative for ownership. Surface 401/403 as sign-in/access-denied messages. Standalone/native Unity does not inherit a browser cookie and cannot save/publish through this browser login workflow. Do not weaken authorization to accommodate it.

The actual Unity Editor browser save/publish workflow could not be validated because no same-origin production-ready Editor build is integrated here. Authenticated HTTP and the browser-equivalent Razor workflow were validated instead.

## G. Anonymous tests

Ran the real Release application over trusted local HTTPS. Tests used isolated consistent SQLite copies of the selected Agriloco-Deploy database; no real map/account data was mutated by the tests.

- Viewer URL, both public UnityMap GETs, and Dashboard GET: 200.
- Anonymous draft, publish, cells POST/DELETE, map-layout, geocode, Dashboard basemap, Add delete, MapImage, and GridPainter: 401 (ten write routes).
- Fresh anonymous WebGL browser load on an untouched data snapshot logged `Viewer basemap loaded | 921 x 568` and `Published viewer map rendered | Features: 58`.
- Existing Unity unsupported-shader messages were emitted, but the map and basemap loaded successfully.

## H. Authenticated and ownership tests

Synthetic fixture accounts were used only in the test copy.

- Existing-format login: 302 with member cookie; database confirmed password upgraded to a marked hash. Registration of a second farm/member: 201 with a marked password hash.
- Authorized draft save/publish: 200; published round trip confirmed one geometry feature with two points.
- Authorized cell create/delete and map-layout save: successful 2xx responses.
- All six API mutations rejected both an unauthorized nonexistent farm ID and the existing second test farm with 403.
- Draft referencing an existing definition belonging to the second farm: 403.
- Missing Origin and foreign Origin on authenticated publish: 403.
- Razor Add create for another farm with a valid antiforgery token: 403. Authorized Add create: 302.
- GridPainter POST: 302; GET cells confirmed the cell actually persisted through its authenticated internal HTTP call.

Successful image uploads and external geocoding were not exercised, to avoid replacing the shared basemap or calling external services. Their authorization entry points were tested anonymously and reviewed. The availability/email implementation was unchanged; live email was not sent during security testing. This is not a full inventory or email regression run.

## I. Release validation

- `dotnet restore`: passed.
- `dotnet build -c Release --no-restore -p:UseSharedCompilation=false`: passed, zero errors; three existing warnings (two nullable assignments in FarmControllers, one async-without-await inventory handler).
- `dotnet publish -c Release --no-build --no-restore -o .codex-build/map-security/publish`: passed. The solution invocation emitted NETSDK1194 about solution-level output; this solution has one application project.
- All 17 files under wwwroot/viewer matched the publish output by SHA-256.
- Publish excludes local appsettings.json, development settings, and SQLite test databases.
- SQL Server `has-pending-model-changes`: no changes. No schema was applied remotely; no model or migration files were changed.

Generated local tests/logs/databases/publish outputs reside in `.codex-build/map-security`, which is excluded from project content. Build also updates normal bin/obj artifacts. These are not source changes for review.

## J. Remaining launch blockers and limits

The identified anonymous map-write/ownership blocker is addressed. This does not establish whole-application security readiness:

1. Complete and test the same-origin Unity Editor launch integration described above if farmer map editing is required at launch.
2. Other existing anonymous routes remain outside this map repair: `/api/Alerts/debug/list` exposes subscription rows, `/api/Alerts/debug/enqueue` can enqueue email events, and other crop/inventory/member-contact endpoints need an access review before public exposure. No live debug email endpoint was invoked. These are concrete residual exposures, not covered by the new map filter.
3. Existing users' legacy passwords remain plaintext until their first successful login upgrades them. Arrange secure credential conversion/reset before moving those credentials to the production database; this pass did not alter real credentials or invent an account-recovery system.
4. Hosting must preserve the forwarded HTTPS scheme and durable protected ASP.NET Data Protection keys for cookies. Follow the existing deployment review's trusted-proxy settings; the map Origin check compares the effective request scheme/host. No hosting settings were applied here.

## Files changed in this security pass

- Program.cs
- Controllers/MembersController.cs
- Controllers/UnityMapController.cs
- Controllers/MapController.cs
- Controllers/FarmControllers.cs
- Controllers/FarmGeoController.cs
- Pages/Farmer/Dashboard.cshtml.cs
- Pages/Farmer/Add.cshtml.cs
- Pages/Farmer/MapImage.cshtml.cs
- Pages/Farmer/GridPainter.cshtml.cs
- Pages/Shared/_Layout.cshtml
- Security/MemberPasswords.cs (new)
- Security/FarmMapWriteAttribute.cs (new)
- Pages/Farmer/Login.cshtml (new)
- Pages/Farmer/Login.cshtml.cs (new)
- Pages/Farmer/Logout.cshtml (new)
- Pages/Farmer/Logout.cshtml.cs (new)
- MAP-WRITE-SECURITY-REVIEW.md (new)
