# Data inventory

What Gahwit Sitti and the platform collect, from the code on 2026-10-31. Use it for the privacy policy, Apple's privacy label, Google's Data safety form and the iOS privacy manifest. Update it whenever an SDK or a field is added.

## What is collected

| Data | Where it comes from | Where it is kept | Why | Linked to the player |
|---|---|---|---|---|
| Player id (random UUID) | Created by the platform at first launch | Platform database; phone (Keychain or app storage) | Account, cloud save | Yes |
| Refresh secret | Platform | Phone Keychain; platform stores only a hash | Staying signed in | Yes |
| Apple or Google user id | Sign in with Apple or Google, when the player links | Platform (`PlayerIdentities`) | Account recovery on a new phone | Yes |
| Birth year (year only), parent consent flag | Age gate | Save; platform (`Players.BirthYear`) | Age rules (ads, social) | Yes |
| Language, country (if sent) | Settings, phone region | Platform (`Players`) | Text and support | Yes |
| Device model, game version | Phone | Platform: player, events, support tickets | Support, error reports | Yes |
| Game progress (the save) | Play | Phone; platform cloud copy | Cloud save, reward checks | Yes |
| Purchases: store transaction id, product, price, currency, date | Apple or Google | Platform (`Purchases`); kept for accounting after deletion, unlinked | Delivering and restoring purchases, refunds | Yes (until deletion) |
| Wallet ledger (gem changes and why) | Platform | Platform (`WalletEntries`) | Paid currency, support | Yes |
| Analytics events (name, fields, time, session id) | Play | Platform (`AnalyticsEvents`) | Improving the game | Yes (player id) |
| Error reports (message, stack trace) | Unity errors on the phone | Platform, as analytics events | Fixing crashes | Yes |
| Performance sample (fps, memory) | Phone | Platform, as an analytics event | Performance on low-end phones | Yes |
| Support message and optional email | Help form | Platform (`SupportTickets`) | Answering the player | Yes |
| IP address | Network | Memory only, for rate limits | Abuse prevention | No (not stored) |

**Not collected:** location, contacts, photos, microphone, camera, advertising id (IDFA or Google's), push tokens (notifications are local), payment details.

**Third parties:** Apple and Google (purchases, sign-in). No analytics, crash or ad SDKs are in the game today. LevelPlay (ads) will add the device's advertising id and ad events; update this file, the policy, the label and the manifest when it's added.

## Apple privacy label (draft answers)

- **Tracking:** No. The game doesn't link its data with other companies' data for ads, and has no ad SDK.
- **Data linked to the user:**

| Apple category | Type | Purposes |
|---|---|---|
| Contact Info | Email Address (only if given in Help) | App Functionality (customer support) |
| User Content | Customer Support | App Functionality |
| Identifiers | User ID | App Functionality, Analytics |
| Purchases | Purchase History | App Functionality |
| Usage Data | Product Interaction | Analytics, Product Personalization: no |
| Diagnostics | Crash Data, Performance Data, Other Diagnostic Data | App Functionality, Analytics |
| Other Data | Birth year (age gate) | App Functionality |

## Google Data safety (draft answers)

- **Data collected:** Personal info (User IDs; email address, optional), Financial info (Purchase history), App activity (App interactions), App info and performance (Crash logs, Diagnostics), Other (birth year).
- **Shared with third parties:** No (Apple and Google as processors only).
- **Encrypted in transit:** Yes (HTTPS).
- **Deletion:** Yes, in the app (Settings, Delete my account); data deleted within 30 days.
- **Collection is required** for: user id, purchases, app activity, diagnostics. **Optional:** email.

## iOS privacy manifest

`unity/Assets/Plugins/iOS/PrivacyInfo.xcprivacy` declares the types above, no tracking, and one API with a required reason:
- **System boot time** (`GahwitUptime.mm` reads the uptime clock): reason `35F9.1`, to measure time between events in the app (trusted time for energy and events).

Unity's engine and packages ship their own manifests for what they use (for example user defaults for PlayerPrefs).
