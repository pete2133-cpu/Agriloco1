# Reusable definition images

## Behavior

`DefinitionImages.ResolveAsync` walks each definition's parent chain within the current farm. The nearest item with `MarketDetails.Image` wins: self, parent, then higher ancestors. No image bytes are copied into child records. Missing/cyclic/cross-farm ancestor chains fail closed. Public resolution also requires every item in the chain to be active and public; farmer resolution allows private items after the existing Member ownership check.

The dashboard uses the same existing avatar dimensions, lazy-loads inherited thumbnails and retains its leaf icon on missing/failed images. The public farm API adds only an `imageUrl` to each item. The selected-feature card uses that URL for the selected feature's actual definition, not its label source. Existing status, labels, selection and geometry are unchanged.

Images are customer-facing independently of FarmStore membership. This does not change the Market list's FarmStore inclusion rule or its existing image endpoint.

## Delivery and caching

- Public thumbnail: `/api/DefinitionImages/public?farmId={farmId}&itemId={definitionId}`.
- Farmer thumbnail: `/api/DefinitionImages/farmer?farmId={farmId}&itemId={definitionId}`, requiring existing authentication/ownership checks.
- The endpoints accept a definition and resolve its effective image. Generated URLs point to the resolved ancestor so siblings share one URL instead of downloading copies.
- SkiaSharp generates WebP thumbnails with a maximum edge of 160 px, respecting EXIF orientation. Original image data stays in MarketDetails. A bounded 32 MB memory cache holds only derived thumbnails, keyed by source-content hash, with 20-minute sliding expiry. Decoding concurrency and image dimensions are bounded.
- Responses use ETags and `private, no-cache`: browsers may retain the small thumbnail but revalidate it. Current access checks run before conditional 304 responses. Replacing an image changes the ETag. Already-open cards refresh their source on page reload; no original image or base64 blob is embedded in item payloads.
- The original Market Details editor, image upload and database schema are untouched.

Thumbnail dependencies: [SkiaSharp 4.152.0](https://www.nuget.org/packages/SkiaSharp/4.152.0) and its matching Linux NoDependencies native-assets package. Windows assets are supplied transitively. No system image-processing executable is required.

## Changed project files

- `Agriloco1.csproj`
- `Program.cs`
- `Services/DefinitionImages.cs` (new)
- `Services/DefinitionThumbnails.cs` (new)
- `Controllers/DefinitionImagesController.cs` (new)
- `Controllers/UnityMapController.cs`
- `Pages/Farmer/Dashboard.cshtml`
- `Pages/Farmer/Dashboard.cshtml.cs`
- `wwwroot/css/farmer-dashboard.css`
- `wwwroot/js/farmer-dashboard.js`
- `wwwroot/viewer/index.html`
- `wwwroot/viewer/viewer.css`
- `wwwroot/viewer/viewer.js`
- `DEFINITION-IMAGES.md`

Validation artifacts are isolated under `.codex-build/definition-images/`; these are not application source or production data.

## Validation

- Honeycrisp image and Row 31 inheritance, child override, fallback through Apple, and no-image fallback.
- Cross-farm/anonymous farmer endpoint rejection, hidden-parent public exclusion, and visibility checks before cached/conditional responses.
- Image access independent of FarmStore selection; Market inclusion remains unchanged.
- Replacement invalidation, corrupted-image fallback, WebP output and ETag/304 behavior.
- A 1,052,655-byte fixture image became a 5,946-byte thumbnail (160 × 99 px).
- Dashboard image retained the 42 × 44 px variety footprint and the existing child-row footprint. Original dashboard form blocks are unchanged.
- Selected Row 31 card displayed the inherited image and stayed 104 px high in desktop and 390 px mobile tests, without horizontal page overflow.
- Market navigation and Crop/Variety preservation checked. GPS source block is byte-for-byte unchanged; physical GPS was not retested for this image-only integration.
- Release build/publish and JavaScript syntax checks passed. EF reports no model changes since the existing Market Details migration. Windows and Linux Skia native assets are present in publish output; Linux execution was not tested locally.

## Manual deployment

1. Review these changes in Visual Studio. No database migration, data backfill or additional image upload is needed. The already-applied Market Details migration remains sufficient; do not rerun it for this change.
2. Restore NuGet packages and build Release. The new thumbnail dependencies must be restored.
3. Publish the **complete ASP.NET application** using your existing Azure publish profile. Include `SkiaSharp.dll`, the updated dependency manifest and the appropriate native runtime assets from normal publish output. Do not copy just the application DLL or viewer files.
4. Reload the dashboard and public viewer. Confirm the uploaded image appears beside the parent and on an inherited child selection. Verify a definition without an image still shows the original fallback.
5. No Unity/WebGL rebuild, map republish, DNS change or POS setup is required.

No Azure deployment or production data changes were performed.
