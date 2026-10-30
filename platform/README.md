# Aqlaan Platform

One .NET application that serves every game in `games/`: players, saves, payments, game settings, live events, analytics and one admin panel.

**Stack:** .NET 10, ASP.NET Core minimal APIs, PostgreSQL, EF Core 10 (Npgsql). The admin web app comes later, in the same solution.

## Run it

You need the .NET 10 SDK and PostgreSQL.

```sh
cd platform
dotnet tool restore                       # dotnet-ef, pinned in dotnet-tools.json
dotnet run --project src/Platform.Api     # http://localhost:5080
```

- **Development** uses `appsettings.Development.json`: database `aqlaan_platform` on `localhost` as user `postgres`. The database is created and migrated at startup.
- **From a phone on the same Wi-Fi:** `dotnet run --project src/Platform.Api -- --urls http://0.0.0.0:5080`, then use the Mac's address, for example `http://192.168.1.20:5080`.
- **Games** register themselves at startup from `games/*/game.json` (`Games:Path`). Values written `[set after ...]` are ignored until they are filled in.
- **The first owner:** `dotnet run --project src/Platform.Api -- create-owner you@aqlaan.com`. It makes that email an owner of every registered game and prints a temporary password once. Sign in and change it; owners then add everyone else.
- **Outside Development**, set `ConnectionStrings:Platform` and `Auth:SigningKey` (32+ bytes) from a secret store, and `Database:MigrateOnStartup=true` or run `dotnet ef database update`. The app refuses to start without the signing key.
- **In Development without a signing key**, each run makes a random one. Access tokens die on restart; the game's refresh token gets a new one. To keep one, use `dotnet user-secrets set Auth:SigningKey ...` in `src/Platform.Api`.

## Tests

```sh
dotnet test platform/Platform.slnx
```

Each test run creates a throwaway database (`platform_test_<guid>`) on `Host=localhost;Username=postgres` and drops it at the end. Set `PLATFORM_TEST_DB` to use another server. Apple and Google keys and the store checks are replaced with test versions.

## How money flows

```
Player buys in the game (Unity IAP)
  → Apple / Google charge the player and send the game a receipt
  → the game sends the receipt to   POST /games/{slug}/purchases/verify   (signed in)
  → the platform checks it directly with Apple (App Store Server API)
    or Google (Play Developer API), using that game's own app ids
  → if valid: record the purchase once (store transaction id is unique),
    grant the items from that game's product list, return the new balances
  → Apple / Google later call   POST /webhooks/apple   or   /webhooks/google
    for refunds and pass renewals; the platform takes back or extends items
Apple and Google pay the company bank account monthly (minus 15–30%).
Ad networks pay monthly too; their reports are imported for the admin panel.
```

The phone never decides it has been paid. Only the platform does.

## Player accounts

- **Guest first.** The game creates a guest on first launch. The guest keeps playing on that phone with its refresh token.
- **Link Apple or Google** to keep progress across phones. Signing in with that identity on a new phone returns the same player. One identity per provider per player.
- **Already taken.** If the Apple or Google id already has its own player, linking answers `409 identity_taken` with that player's save summary. The game asks which progress to keep, then signs in or stays.
- **Tokens.** Access tokens are platform-signed JWTs that last 1 hour and work on one game only. Refresh tokens are 32 random bytes that last 180 days. The platform stores only their SHA-256 hash. Each refresh token works once. If a used one comes back, every session it led to ends.
- **Identity tokens** are checked against Apple's and Google's published keys. Apple: issuer `https://appleid.apple.com`, audience = the game's bundle id. Google: issuer `accounts.google.com`, audience in `stores.google.signInClientIds`. With a `nonce`, the token's `nonce` claim must equal SHA-256 (hex) of it. The game sends the hash to Apple or Google and the plain nonce to the platform.
- **Age band.** The game asks for the birth year only. The platform counts the younger possible age: `age = current UTC year − birthYear − 1`. Under 13 is `under13`, 13 to 15 is `13to15`, 16 and over is `16plus`, no birth year is `unknown`. Birth years from 1900 to the current year are accepted. Under-13 rules: no personalised ads and no social features until a parent agrees (COMPLETE_SPEC.md section 14).
- **Deleting an account** (required by both stores) is immediate for the player: every session ends and the Apple or Google id is released, so it can start a new account. The data is purged within `Accounts:DeletionGraceDays` (30, never more). The purge removes the save, wallet, ledger, sessions, support messages and the player. **Purchases stay** for accounting and tax, with the player link removed.
- **Cloud save.** Each upload names the version it was based on. If another phone uploaded since, the platform answers `409 conflict` with that save's level, chapter, time and device. The game asks "keep this phone or the other one?" and sends `force: true` to keep this one.
- **Sanity check** (COMPLETE_SPEC.md section 11). Uploads that gain levels, chapters or coins faster than `Saves:Sanity` allows are flagged for staff. They are never rejected. The limits are starting values to tune from real play data.
- **Rate limits.** Guest creation, sign-in, refresh and staff sign-in are limited per IP (`RateLimits:AuthPerMinute`, 30).

## Staff

- Staff sign in with email and password (ASP.NET Core `PasswordHasher`). Staff tokens last 8 hours. A disabled account stops working at once.
- New staff get a temporary password. They must set their own (12+ characters) before doing anything else.
- **Roles per game:** owner > producer > support > viewer. Each role can do everything the roles below it can.

| Role | Can |
|---|---|
| viewer | Look up players: account, masked Apple/Google ids, save summary and flags, wallet and ledger, purchases, support messages, deletion status |
| support | Grant compensation or fix a wallet (never below zero, always with a reason); read and close support messages |
| producer | Same as support for now; config and events come next |
| owner | Add, change and remove staff (never the last owner); read the audit log |

Every staff write on a game goes into its audit log: who, what, when and why.

## Data model

Every table has a `GameId`, except `Games` (it is the game) and `AdminUsers` (staff work across games; their access per game is in `AdminRoles`).

| Entity | What it holds |
|---|---|
| Game | slug, names, Apple bundle id, Google package name, Google sign-in client ids |
| Player | one account per game: birth year, country, language, created and last seen, deletion request |
| PlayerIdentity | a linked Apple or Google id (provider + subject) |
| PlayerSession | a refresh token's hash, its family, expiry and revocation |
| PlayerSave | the latest save (jsonb), its version, level, chapter, coins and device |
| SaveFlag | an upload that looked impossible |
| WalletBalance | one paid currency of one player, never below zero, changed only on the server |
| WalletEntry | every wallet change: amount, source (purchase or admin), reason, who |
| RewardClaim | a reward the game earned (`levelUp:7`, `chapter:2`) and its gems; one per player and id |
| GemSpend | gems spent on an item, with what the game gets; one per player and request id |
| Product | sku, type (consumable, non-consumable, subscription, non-renewing for a fixed-length pass), what it grants |
| Purchase | store, store transaction id (unique), sku, price, currency, status (verified, refunded) |
| SupportTicket | a Help form message, with game version, device and language |
| ConfigVersion | a published copy of the game's `balance.json`, with version and date |
| LiveEvent | template, start and end, settings; lets the team run events without app updates |
| AdminUser | a staff account: email, password hash, disabled |
| AdminRole | a staff member's role on one game |
| AuditEntry | one staff write on a game |

Migrations live in `src/Platform.Api/Data/Migrations`. Add one with `dotnet ef migrations add <Name> --project src/Platform.Api --output-dir Data/Migrations`.

## API

All JSON is camelCase. Errors look like `{ "error": "code" }`. Every game route starts with `/games/{slug}`; an unknown slug is `404 unknown_game`.

**Public**

| Method and route | Job |
|---|---|
| `GET /health` | Health check |
| `GET /games/{slug}/time` | `{ now, nowMs }`: server time, so energy can't be cheated by changing the phone clock |
| `GET /games/{slug}/config` | The active balance settings. A game's `balance.json` (game.json `balanceConfig`) is published as version 1 when none exists; in Development a changed file is published again |
| `GET /games/{slug}/version` | `{ minVersion }` from game.json `minGameVersion`; older games show "update required" |
| `GET /games/{slug}/events` | Live events running now |
| `POST /games/{slug}/players/guest` | `{ device, gameVersion }` → a new guest's `AuthResponse` |
| `POST /games/{slug}/players/sign-in` | `{ provider: "apple" \| "google", idToken, nonce? }` → `AuthResponse` for that identity's player (new or existing). `401 invalid_token`, `400 provider_not_configured`, `400 unknown_provider` |
| `POST /games/{slug}/players/refresh` | `{ refreshToken }` → `AuthResponse` with a new refresh token. `401 invalid_refresh` |

`AuthResponse` is `{ playerId, accessToken, accessExpiresAt, refreshToken, account }`.
`Account` is `{ playerId, providers, birthYear, ageBand, country, language, createdAt, deletionRequestedAt }`.

**Signed in as a player** (`Authorization: Bearer <accessToken>`)

| Method and route | Job |
|---|---|
| `GET /games/{slug}/players/me` | `Account` |
| `PATCH /games/{slug}/players/me` | `{ birthYear?, language?, country? }` → `Account`. `400 invalid_birth_year`, `invalid_language`, `invalid_country` |
| `DELETE /games/{slug}/players/me` | `202 { deleteBy }` |
| `POST /games/{slug}/players/me/link` | Same body as sign-in → `{ account }`. `409 identity_taken { other: { playerId, save } }`, `409 provider_already_linked` |
| `POST /games/{slug}/players/sign-out` | `{ refreshToken }` → `204` |
| `GET /games/{slug}/players/me/save` | `{ version, save, summary: { level, chapter }, updatedAt, device }`, or `204` with no save |
| `PUT /games/{slug}/players/me/save` | `{ baseVersion, save, device, force? }` → `{ version }`. `409 conflict { server: { version, summary, updatedAt, device } }` |
| `GET /games/{slug}/players/me/wallet` | `{ wallet: { gems } }` |
| `POST /games/{slug}/players/me/rewards` | `{ id: "levelUp:7" \| "chapter:2" }` → `{ granted: { gems }, wallet }`, or `{ alreadyClaimed, wallet }`. Each id grants once. The latest cloud save must show it: `409 not_earned_yet` until the game uploads it. `400 unknown_reward` |
| `POST /games/{slug}/players/me/spend` | `{ item, requestId }` → `{ item, gems, grant, wallet }`. Items: `energyRefill` (each refill today costs more), `storageSlot:N` (once), `bubble:L`, `memoryRetry:<memory>`. A repeated `requestId` returns `{ alreadySpent }` without spending again. `409 not_enough_gems`, `409 already_owned`, `400 unknown_item`. The game applies `grant` only after this reply |
| `POST /games/{slug}/players/me/support` | `{ message, email?, gameVersion, device, language }` → `201 { ticketId }` |
| `POST /games/{slug}/purchases/verify` | `{ store, sku, receipt }` → `{ granted, wallet }` or `{ alreadyGranted, wallet }` |

**Staff** (`Authorization: Bearer <staff accessToken>`)

| Method and route | Role | Job |
|---|---|---|
| `POST /admin/sign-in` | | `{ email, password }` → `{ accessToken, expiresAt, admin: { id, email, mustChangePassword, roles: { slug: role } } }` |
| `GET /admin/me` | any | The signed-in staff member |
| `POST /admin/me/password` | any | `{ currentPassword, newPassword }` → `204` |
| `GET /games/{slug}/admin/players?query=` | viewer | Find by player id or Apple/Google subject; no query lists the newest 50 |
| `GET /games/{slug}/admin/players/{id}` | viewer | Everything about one player |
| `POST /games/{slug}/admin/players/{id}/grants` | support | `{ currency, amount, reason }` → `{ wallet }`. Negative takes back. `400 insufficient_balance` |
| `GET /games/{slug}/admin/support?status=open` | support | Support messages (`open` or `closed`) |
| `POST /games/{slug}/admin/support/{id}/close` | support | `{ note }` |
| `GET /games/{slug}/admin/staff` | owner | Staff on this game |
| `PUT /games/{slug}/admin/staff` | owner | `{ email, role }`. A new email gets an account; the reply has its `temporaryPassword`, shown once |
| `DELETE /games/{slug}/admin/staff/{adminId}` | owner | Remove from this game. `400 last_owner` |
| `GET /games/{slug}/admin/audit` | owner | The newest 200 staff writes |

**Store notifications:** `POST /webhooks/apple` and `POST /webhooks/google` (to do: refunds, renewals, cancellations).

## Still needs you

- **Signing key:** a 32+ byte `Auth:SigningKey` in a secret store for every non-Development server.
- **Google sign-in:** create OAuth client ids in Google Cloud (Android, plus a web client id if the game asks for an id token with it). Put them in `game.json` under `stores.google.signInClientIds`.
- **Apple sign-in:** turn on the Sign in with Apple capability for the bundle id. To revoke Apple tokens when an account is deleted (Apple asks for this), the platform needs the Sign in with Apple key (`.p8`, key id, team id) and the Apple refresh token from the first sign-in. Until then `IAppleTokenRevoker` only logs.
- **Store verification:** the Apple and Google receipt checks are still stubs that refuse every receipt, so nothing is granted by mistake. They need the App Store Server API key and a Google Play service account.

## Admin panel (to come)

One web app for all games, on top of the staff API above:
- **Games:** add a game from its `game.json`.
- **Players:** look up a player, see their purchases and fix their wallet.
- **Purchases and refunds:** see every purchase and refund.
- **Config:** publish a new `balance.json`.
- **Live events:** schedule events.
- **Revenue dashboards:** store and ad revenue per game and per country.
