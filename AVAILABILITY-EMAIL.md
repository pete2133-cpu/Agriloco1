# Availability email notifications

## Test in Visual Studio
1. Stop the previous debugging session, rebuild Agriloco1.sln and launch IIS Express normally.
2. Open the public farm page. Click **Email me when available** for a crop or variety and enter the address that should receive notifications.
3. In the farmer dashboard, change that item from an unavailable status (such as Coming Soon or Unavailable) to **Available**. Matching subscribers will be emailed using the existing SMTP configuration.
4. Check the dashboard banner: sent count, failure count, or no matching subscribers. Check the recipient's inbox/spam folder to verify delivery.
5. Changing a row to Available also triggers the notification if this newly makes its parent variety available. The email describes the crop/variety, rather than the row.

## What changed
- The public farm page now sends dashboard crop/variety subscriptions to the existing AvailabilitySubscriptions endpoint using FarmDefinitionId. Legacy-only listings keep their legacy crop endpoint. These IDs are never interchangeable.
- Dashboard notifications handle both modern subscriptions and matching pre-existing legacy crop subscriptions. Legacy matching uses category/variety names within the same farm.
- Modern subscriptions remain active for future availability transitions. Legacy one-time subscriptions are fulfilled only after a successful send. Public subscription messages distinguish these behaviors.
- One email per address per status action, even when the address follows the variety, its parent product, the whole farm and a matching legacy crop. A newly activated variety supplies the specific name instead of sending an additional generic parent-product email.
- Private/inactive branches and other farms' subscriptions are excluded.
- Saving an already-available status does not resend. Existing available states are Available, Peak Season, Limited Availability and Late Season; moving between these states does not trigger another email. Switching off and later becoming available is a new event for recurring subscribers.
- SMTP failures do not undo the availability change. They are logged and shown in the dashboard; failed recipients are not marked notified. Other recipients are still attempted. SMTP attempts have a 20-second cancellation timeout.

## Configuration and limits
SMTP uses Email:Host, Email:Port, Email:User, Email:AppPassword and Email:From. A stale AppPassword override in appsettings.Development.json caused Gmail authentication error 535 when launched from Visual Studio. That override has been removed, so development inherits the main configuration password. The main credential successfully authenticated to Gmail over TLS (235); no email was submitted during diagnosis. Inbox delivery still needs an actual availability transition after restarting the app.

Notifications are sent during the status request. This change does not add a durable mail outbox or automatic retries; a failed attempt is reported for troubleshooting. Already-available items do not generate retrospective emails when a new subscription is created. This change concerns dashboard availability status, not sales-channel checkboxes.

## Verification
Build passed with zero errors and the two pre-existing nullable warnings in FarmControllers. Nineteen tests on a copied SQLite database used a capturing/failing IEmailSender: direct transitions, parent activation, deduplication, legacy fulfillment, recurring subscriptions, private/inactive/other-farm exclusions, failure reporting, successful-recipient timestamps, and public subscription API through to the dashboard sender.

No real emails were sent and no credentials were printed. The obsolete development password override was removed; the working main credential was not changed. The working database was excluded from the checkpoint.

## Files
- Services/DashboardAvailabilityNotifications.cs — matching, deduplication, sending and subscription bookkeeping.
- Pages/Farmer/Dashboard.cshtml.cs — detect all newly available crop/variety nodes after hierarchy updates.
- Pages/Farmer/Dashboard.cshtml — notification result banner.
- Pages/Public/Farm.cshtml — correct subscription endpoint/ID and matching subscription messages.
- Services/SmtpEmailSender.cs — bounded SMTP send timeout.
- AVAILABILITY-EMAIL.md — behavior and test steps.
