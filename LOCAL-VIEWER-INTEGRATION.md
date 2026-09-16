# Local Unity viewer integration

## A. Export copied

Source: `C:\Users\pete2\My project (9)\Builds\PublicViewer-20260909-113343`

Destination: `C:\Users\pete2\source\repos\Agriloco-Deploy\wwwroot\viewer`

The destination did not previously exist. Copied all 17 files directly beneath `viewer`, including `index.html`, `Build/`, and `TemplateData/`. No extra parent directory, Unity source, fixture files, or old export was included. This build has no StreamingAssets directory. SHA-256 checks matched every file against both the integrated and Release-published copies.

## B. Backend changes

No backend source or configuration changes were needed. Existing default-file and Unity MIME/Brotli handling worked. Auth, email, startup, database-provider configuration, and SQL Server migrations were not edited. Unity source was not modified. Added this report and the 17 web assets listed below.

The existing Development startup automatically created the previously missing, empty `ProductionIngredientSources` table and advanced the `AvailabilityChannels` SQLite identity counter from 140 to 145 through its existing INSERT OR IGNORE seeding. All existing business-table rows, including subscriptions, are unchanged. Thus `agriloco.db` has a normal startup metadata/schema change, not test farm/subscription data. A consistent pre-start backup is retained at `.codex-build/local-integration/database-before.sqlite`. No production database was contacted.

## C. Restore, build, publish, run

Restore passed. Release build passed with zero errors and three existing warnings: two CS8601 warnings in FarmControllers and one CS1998 warning in ProductionRunDetails. Release publish also passed, and all 17 viewer assets were verified in `.codex-build/local-integration/publish/wwwroot/viewer`.

The actual application ran in Development against the repository's existing `agriloco.db`. A free port was allocated and confirmed in Kestrel's startup log: `https://localhost:65217`. TLS certificate validation remained enabled; the existing localhost development certificate is trusted. Environment/URL overrides were process-local only. The temporary test application and browser tab were stopped after validation.

## D–E. Real API and WebGL results

Tested `https://localhost:65217/viewer/?farmId=1` in the actual WebGL browser player served by ASP.NET. No synthetic server/data was used.

The only real farm available was Farm 1, Wheelbarrow Orchards. The real farm endpoint returned HTTP 200 with 80 items and `/uploads/farms/1/basemap.png`. The real published endpoint returned HTTP 200 with 58 features. The browser displayed the farm name, actual orchard basemap, published rows and labels. Browser logging confirmed `Published viewer map rendered | Features: 58`. No second real farm was available.

## F. Requests and static assets

Server logs captured actual browser HTTP/2 requests to:

- `https://localhost:65217/api/UnityMap/farm?farmId=1`
- `https://localhost:65217/api/UnityMap/published?farmId=1`
- `https://localhost:65217/uploads/farms/1/basemap.png`

All returned HTTP 200. Refresh requests used the same origin. No old 44389/5227 connection, `/viewer/api/` mistake, mixed-content failure, or compression/MIME failure was observed.

Verified HTML, loader JavaScript, CSS, PNG, and native Brotli framework/WASM/data responses. Raw responses used `application/javascript`, `application/wasm`, and `application/octet-stream` respectively with `Content-Encoding: br`. Fonts are embedded in this Unity export; it includes no separate JSON or font files requiring extra hosting mappings. Existing standard static-file mappings remain in place.

The browser logged unsupported internal CoreCopy, StencilDitherMaskSeed, and HDRDebugView shader diagnostics, also seen during the earlier standalone smoke test. They did not prevent actual map rendering. This is not a claim of compatibility across all phone/GPU combinations.

## G. Farm ID validation

Farm 1 loaded real data. `/viewer/` and `/viewer/?farmId=0` both displayed the explicit positive-farm-ID error and logged it to the console. Server-log checks found zero subsequent farm API requests for either invalid launch. No arbitrary/default farm was loaded.

## H. Availability signup

The Viewer rendered its signup UI. Empty input showed the client validation message. A nonempty invalid address submitted through the actual Unity UI reached `/api/AvailabilitySubscriptions`; the real backend returned HTTP 400 and `Please enter a valid email address`, also displayed by the Viewer. A separate invalid-email HTTP check gave the same response. Existing subscription rows remained unchanged. No successful subscription, availability transition, or real email was sent/tested.

## I. GPS

The existing geolocation plugin initialized but browser location timed out (error 3). The map still rendered. GPS code was not changed. Real phone coordinates/permission behavior still require device testing. The known absence of a GPS-to-map marker implementation is unchanged and is not a basic deployment blocker.

## J. Before Azure

No blocker remains in this local viewer/static-host integration. The next deployment step still needs target production SQL connection/settings and schema/data readiness (including the farm's published map and uploaded basemap), public HTTPS configuration, and a hosted smoke test. Production SQL connectivity, successful SMTP delivery, and cellular-device behavior were not validated by this local SQLite test. No Azure resources or DNS changes were made.

No Git commands, commits, pushes, or `.git` modifications were performed.

## Files added under wwwroot/viewer
- `index.html`
- `Build/PublicViewer-20260909-113343.data.br`
- `Build/PublicViewer-20260909-113343.framework.js.br`
- `Build/PublicViewer-20260909-113343.loader.js`
- `Build/PublicViewer-20260909-113343.wasm.br`
- `TemplateData/favicon.ico`
- `TemplateData/fullscreen-button.png`
- `TemplateData/MemoryProfiler.png`
- `TemplateData/progress-bar-empty-dark.png`
- `TemplateData/progress-bar-empty-light.png`
- `TemplateData/progress-bar-full-dark.png`
- `TemplateData/progress-bar-full-light.png`
- `TemplateData/style.css`
- `TemplateData/unity-logo-dark.png`
- `TemplateData/unity-logo-light.png`
- `TemplateData/unity-logo-title-footer.png`
- `TemplateData/webmemd-icon.png`

Validation artifacts are under .codex-build/local-integration (backend log, API snapshots, file hashes, pre-start database backup, and publish output), outside production web content.
