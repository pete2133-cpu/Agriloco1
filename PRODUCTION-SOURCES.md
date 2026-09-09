# Receiving QR labels and optional production sources

## Test with Visual Studio
1. Open Agriloco1.sln in this ag3 folder. Stop the previous debugging session, rebuild and launch IIS Express normally. Startup creates the new ProductionIngredientSources table if it does not exist. No manual database command is needed.
2. Go to Inventory & production > Receiving. Choose **QR label** on a saved receipt, or open Details > **Show / print receiving QR label**.
3. Check the receiver, supplier, receipt number/date, item, received quantity/unit, total usable quantity and condition. If a harvest was used, its farm, lot, crop, variety, row, date and original quantity are included. Use **Print label / Save PDF**, show the QR on screen, or expand **Copy transfer text instead**.
4. Open Production > create a run from a saved recipe, or open an untouched draft's Details. Use **Load a saved recipe** and **Load recipe ingredients** to load the list. A recipe must have ingredients. Loading replaces the untouched draft ingredient list; it is disabled once quantities, sources or work have been recorded. Start another run if you need a different recipe after that point.
5. Expand **Apples — Source not recorded** under Ingredient sources. Choose **Saved receiving lot**, select an apple receipt, review the receiving/harvest preview, and press **Save source for Apples**. Confirm that Golden Delicious appears under Crop variety, separately from the recipe package and usage unit.
6. Expand Salt and leave **Not recorded** selected. Source selection is optional and never required to use an ingredient.
7. To test scanning, choose **Scan receiving QR** for an ingredient. Upload a receipt QR image, scan a screen/paper label, or paste its transfer text. Review the details, confirm it is the receipt for this ingredient, and save. Reopen the ingredient to check its saved receipt and harvest information.
8. Enter actual ingredient quantities using the existing controls. The selected source does not automatically copy the receipt's entire quantity into production or alter the scale.

Camera use needs permission on HTTPS or localhost. A phone's localhost is the phone itself; it needs an accessible HTTPS application address to use the computer-hosted website. QR image upload and transfer-text paste work without camera access.

## Behavior and boundaries
- One optional receiving source per production ingredient, either an owned receipt link plus snapshot, or a scanned snapshot. Multiple lots for one ingredient are not implemented in this pass.
- Local receipt choices are scoped to the farm and matched to the ingredient inventory item. For ingredients without an inventory ID, an exact item-name match is used. The saved harvest variety helps distinguish receipts for different apple varieties.
- QR labels contain a self-contained receipt snapshot rather than localhost links or internal database IDs. Imports never mistake another business's numeric IDs for local records.
- The QR format is `AGRILOCO-RECEIPT:1:` followed by JSON. Fields: Receiver, Address, Lot, Date, Supplier, Item, Quantity, Unit, TotalQuantity, BaseUnit, Condition, and optional Harvest. Harvest uses the existing harvest transfer fields. The combined limit is 2,200 UTF-8 bytes; overly long/incomplete records display an error rather than dropping harvest information.
- The source snapshot is retained if the original receiving/harvest record later changes or is deleted. Printed labels likewise retain the information at printing time. Sources can be changed/cleared before completion; completed/cancelled runs are locked.
- Receipt details are business-supplied, not cryptographically verified. A scanned receipt requires the user to confirm that it belongs to the selected ingredient. Scanning does not create stock or a new receiving lot.
- Existing recipe quantity/scaling, stock consumption and output calculations are unchanged. This feature records source information; it does not allocate/deplete individual receiving lots or convert receipt quantities into production usage.
- No Unity, authentication, GPS, email or existing API contract changes. Nothing is deployed or pushed.

## Storage
The additive ProductionIngredientSources table stores farm/run/ingredient IDs, an optional local ReceivingLotId, receipt JSON and recording time. A unique index permits one source per farm/ingredient. It does not alter existing columns. Existing-database setup is idempotent and follows the application's SQLite startup setup. New databases include the table through EF EnsureCreated. Draft-run deletion cleans up its source entries.

## Verification
- Build: zero errors, with the two existing nullable warnings in FarmControllers.
- 28 checks on a consistent SQLite backup covered receipt/nested harvest round trips, missing/invalid/oversized/versioned inputs, farm scoping, recipe loading, ingredient matching, optional salt source, preserving recorded activity, snapshot survival after receipt edits, QR confirmation, completed-source locking, and no inventory changes from source selection.
- Browser: loaded recipe ingredients; previewed and saved a local receiving lot with Golden Delicious/Row 39; kept Salt unrecorded; decoded the generated receiving QR image and saved the scanned snapshot; confirmed it persisted after reload.
- Receiving label content and screen layout inspected. Physical camera scanning, paper printing and real mobile devices still need user testing.
- Test data and new-table setup were exercised only in an ignored copied database under bin/FixChecks. The working database is excluded from the Git checkpoint.

## Files changed
- Data/AgrilocoContext.cs — register source table.
- Models/Inventory/ProductionIngredientSource.cs — source snapshot record.
- Services/ProductionSourceSchema.cs and Program.cs — additive startup table/index setup.
- Services/ReceivingTransfer.cs — portable receipt plus optional harvest format, lookup and validation.
- Pages/Farmer/Inventory/ReceivingLabel.cshtml and .cshtml.cs — screen/print receiving QR.
- Pages/Farmer/Inventory/ReceivingLots.cshtml and ReceivingLotDetails.cshtml — receipt QR links.
- Pages/Farmer/Inventory/ProductionRunDetails.cshtml and .cshtml.cs — saved recipe loading, optional source controls, previews, validation and persistence.
- Pages/Farmer/Inventory/ProductionRuns.cshtml.cs — cleanup source entries when a draft run is deleted.
- wwwroot/js/production-receipt-sources.js — camera/image decoding, receipt preview and source controls; reuses the existing local jsQR dependency.
- PRODUCTION-SOURCES.md — workflow, format and verification notes.
