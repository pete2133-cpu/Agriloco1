# Harvest sources in receiving

## Test in Visual Studio
1. Open Agriloco1.sln from this ag3 folder. Stop the previous debugging session, restore NuGet packages and build. Start IIS Express as usual.
2. Go to Farmer dashboard > Inventory & production > Harvests.
3. On a harvest with crop/variety/row saved, choose **QR label**. Check the farm name/address, harvest lot number, crop, variety, row, harvest date and original quantity/unit.
4. Use **Print label / Save PDF**, or leave the QR visible on screen. A previously printed label does not update when the harvest is edited.
5. Choose **Receive** beside a harvest, or choose **One of my harvests** in Receiving. Confirm the selected harvest. Confirm the item and crop variety, choose a receiving unit, and enter the actual amount received, and record delivery. A supplier is optional here; blank uses the harvest farm name.
6. Open the new receiving lot's **Details**. Original harvest source must show the farm, lot, crop, variety, row, harvest date and original quantity. The receipt quantity is separate.
7. For the external workflow, choose **Scan another farm's harvest** in Receiving. Scan a printed/on-screen label with the camera, upload a clear QR image, or paste the label's transfer text. Review the decoded source, then choose the receiving business's own inventory item/package and quantity.
8. A regular supplier delivery still works without any harvest source. Invalid QR data must show an error and create no receipt.

Camera scanning needs permission on HTTPS or localhost. On a phone, localhost refers to that phone; an accessible HTTPS address is needed to use the laptop-hosted application. Uploading an image or pasting transfer text works without camera access.

## Scope and storage
- No database schema migration, Unity change, existing API contract change, authentication change or email.
- The existing SourceHarvestLotId links receipts from this farm's own saved harvest.
- Both local and scanned receipts store a source snapshot in the existing ReceivingLotCustomFields table under the reserved field name `Agriloco harvest source v1`. Details render it separately and ordinary custom-field deletion cannot remove it.
- External QR data contains no local database IDs or localhost URL; no remote requests are made when parsing it. Imported records never become local harvest links through coincidentally matching IDs.
- The portable format is `AGRILOCO-HARVEST:1:` followed by JSON with Farm, Address, Lot, Crop, Variety, Row, Date (YYYY-MM-DD), Quantity and Unit. Maximum 2,000 UTF-8 bytes. Later Unity clients can read this same format.
- Worker names, notes and contact details are not included. The source farm identifies the harvester.
- QR information is farm-supplied, not cryptographically verified. The receiver reviews it and maps it to their own inventory item.
- The original harvest quantity is not a shipment quantity. Multiple receipts may reference one harvest; existing package/unit calculations remain unchanged and receiving does not subtract from the harvest.
- This carries harvest information into receiving; production transformations and onward chain-of-custody are outside this change.

## Verification
- Application built with zero errors. Two existing nullable warnings in FarmControllers remain. A subsequent sandbox build also reported NuGet audit connectivity warning NU1900; the initial online restore/audit succeeded.
- 22 checks against a SQLite backup: valid/invalid/versioned/oversized/Unicode payloads, own and external receiving, farm-scoped lookup, snapshot preservation after source edits, source field protection, and manual/base/package receiving behavior.
- Browser QR image decode and actual form submission succeeded against the test database, including accented characters and emoji.
- Label page rendered and QR preview inspected. Physical camera scanning, paper printing and real mobile hardware still need user testing.
- All test receipts were created only in an ignored copied database under bin/FixChecks. The working database was excluded from the commit.

## Dependencies
- [QRCoder 1.8.0](https://www.nuget.org/packages/QRCoder/1.8.0) generates PNG QR images locally.
- [jsQR](https://github.com/cozmo/jsQR) 1.4.0 is vendored in wwwroot/lib/jsqr with its license. Camera and uploaded-image decoding run locally; there is no runtime CDN dependency.

## Changed files
- Agriloco1.csproj — pinned QR generator package.
- Services/HarvestTransfer.cs — portable source format, validation and snapshots.
- Pages/Farmer/Inventory/HarvestLabel.cshtml and .cshtml.cs — screen/print QR label.
- Pages/Farmer/Inventory/HarvestLots.cshtml — QR label and Receive links.
- Pages/Farmer/Inventory/HarvestLotDetails.cshtml — QR label link.
- Pages/Farmer/Inventory/ReceivingLots.cshtml and .cshtml.cs — source choices, farm lookup/import, atomic receipt and snapshot save.
- Pages/Farmer/Inventory/ReceivingLotDetails.cshtml and .cshtml.cs — retained source and protection from generic custom-field removal.
- wwwroot/js/receiving-harvest.js — camera/image scanner, review and source controls.
- wwwroot/lib/jsqr/jsQR.js and LICENSE — local decoder.
- wwwroot/css/v1.css — source card and print label styling.
- HARVEST-RECEIVING.md — workflow, format and test notes.

## Crop variety and receiving unit
Selecting a saved own harvest fills the supplier and matching inventory item. Crop variety (for example Golden Delicious) appears directly below the item, from the retained harvest snapshot. Scanned harvests also show their crop variety.
Receiving unit is a separate required selection: the item's base unit (for example kg) or one of its saved packages (for example Bushel or Bin). It is not preselected from the harvest unit. A harvest recorded in bushels can be received in kg; the receiver enters the actual received quantity. Package options use the existing saved quantity per package; no conversion of the original harvest quantity occurs.
The receiving list and details now label crop variety and receiving unit separately. Existing database property names remain unchanged for compatibility; the crop variety comes from the harvest snapshot rather than the legacy package VariationName property.
Changing harvest/item clears the unit choice. Validation redisplays preserve the receiver's selection. Missing or ambiguous item matches still need a manual choice.
Verified: zero build errors (two existing nullable warnings); 30 service/handler checks. Browser test selected Golden Delicious Row 39 from a 7-bushel harvest, chose kg and saved 15 kg. The saved row showed Apples / Golden Delicious / kg and retained the original source quantity of 7 bushels. Test writes used only a copied database.
Files for this correction: Services/HarvestReceivingDefaults.cs; ReceivingLots.cshtml/.cshtml.cs; ReceivingLotDetails.cshtml; wwwroot/js/receiving-harvest.js; this document.
