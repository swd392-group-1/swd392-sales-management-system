# AI assistance record

Date: 2026-11-04. Tool: Codex. User authorized implementing the Tech Lead flow, reviewing/merging the two open PRs and preparing a template-based SRS review copy. Existing local diagrams were read, not edited.

Work: reviewed PR #13 and #14; resolved shared Book/DbContext/Program conflicts; restored .NET 8 SDK configuration; removed machine-specific database settings from current configuration; implemented inventory report query/service/MVC/CSV and in-memory price queue/worker/SignalR integration; added regression tests and matching PlantUML design diagrams. Copied the existing review-schema artifacts into the integration checkout without applying schema changes to the existing DB.

Validation: Release test suite 44/44; actual local SQL report reads/filter/export; two WebSocket clients received BookPriceChanged after saving the unchanged B01 fixture price. SQL baseline 12 Books / 183 units retained. Test app stopped after verification. No CI result, production auth, durable event delivery or real VnPay integration is claimed.

Document: source v1 and official template preserved, SRS output saved separately; section 3.4 now contains normalized relational design and declared-FD limitations. Original embedded diagram media retained. Word COM was used to update fields/export PDF after packaged renderer reported no LibreOffice executable; PDF pages were rasterized with bundled pypdfium2 and inspected. The report remains a review copy requiring the team's manual diagram corrections and resolution of marked requirements.

Human acceptance of requirements/design remains with the team/instructor. The review does not assert all implementation branches are merged: the access and inventory branches have not been submitted as the two reviewed PRs.
