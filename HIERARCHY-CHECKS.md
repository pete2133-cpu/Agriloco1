# Dashboard parent/child availability

Applies to Availability dropdown statuses and each sales-method checkbox independently.

- Enable a child: enable that entry and every ancestor. Siblings and descendants remain unchanged.
- Disable a parent: disable that entry and all descendants. Its ancestors remain unchanged.
- Enable a parent: descendants remain unchanged.
- Disable the last child: ancestors remain unchanged until explicitly disabled.
- Dropdown statuses Available, Peak Season, Limited Availability and Late Season count as enabled, matching the existing dashboard/search definition. An unavailable ancestor becomes Available; an already-available ancestor keeps its specific status.
- Selecting another status (for example Coming Soon or Unavailable) applies that exact status to the selected entry and its descendants.
- Sales methods are independent of each other and of the dropdown. Visibility is not cascaded.
- Changes use the existing hierarchy and tables. One SaveChanges operation persists the affected branch atomically. Traversal is farm-scoped and cycle-safe.
- Existing notification behavior for the explicitly selected entry is retained. Automatically updated ancestors do not generate additional notifications. Email implementation, Unity and APIs were not changed.

## Changed files

- Pages/Farmer/Dashboard.cshtml: explain the automatic hierarchy rules.
- Pages/Farmer/Dashboard.cshtml.cs: apply the rules in the existing status and sales-method save handlers.
- Services/FarmHierarchyChanges.cs: shared branch traversal and status updates.
- HIERARCHY-CHECKS.md: this record.

## Verification

Build succeeded with zero errors and two existing nullable warnings in FarmControllers.cs.

30 regression checks passed using the real save handlers against a disposable database copy. They covered saved ancestor activation, descendant deactivation, siblings unchanged, parent-on leaves children unchanged, child-off leaves ancestors unchanged, existing seasonal status preservation, unavailable status propagation, three independent sales methods, no cross-farm updates, invalid IDs/statuses, independent dropdown/channel state, and cycle termination.

All fixture updates were rolled back. A no-op email sender was used in tests; no emails were sent. The working database was not modified and its existing changes remain uncommitted.

## Test in Visual Studio

1. Stop debugging, rebuild Agriloco1.sln in the ag3 folder, then press F5.
2. In Dashboard, enable Pick Your Own on an apple row. The row, its variety and Apple should be checked after reload; other rows remain unchanged.
3. Disable Pick Your Own on the variety. All rows beneath it should be unchecked; Apple remains checked.
4. Enable the variety again: its rows remain unchecked.
5. Repeat using the Availability dropdown. A row set to Available activates unavailable ancestors; a variety set to Unavailable deactivates all rows beneath it.
6. Confirm Wholesale/Farm Store and sibling varieties retain their independent settings.

These actions intentionally update your data. Existing manually triggered availability notification behavior still applies when testing with your real database.