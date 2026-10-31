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

## Deploy

The platform runs on the team's Docker host (s1) behind Cloudflare and Traefik, at `https://games.aqlaan.com`. It follows the servers' deploy standard (the `aqlaan-deploy` skill in `~/Desktop/projects/servers`).

- **Image:** `ghcr.io/aqlaangroup/games-platform:X.Y.Z`, linux/amd64, from `Dockerfile`. The build context is the repo root. `Dockerfile.dockerignore` lets in only `platform/src` and the game files the platform reads: each `game.json`, its `design/balance.json` and its `legal/*.html`. The Unity projects never enter it.
- **Container:** non-root (uid 1654), plain HTTP on 8080, read-only with `/tmp` as tmpfs. No ports; Traefik routes to it.
- **Health:** `GET /healthz` returns `{ "version": "X.Y.Z" }`, the version the image was built with (`APP_VERSION`). `GET /health` stays.
- **Client IP:** the auth rate limit counts per `CF-Connecting-IP`, or the connection's address without it (`Auth/ClientIp.cs`). Use that helper for anything that logs or limits by IP.
- **Two database logins** on the shared PostgreSQL, database `games`:
  - `games_app` runs the app (`ConnectionStrings:Platform`). It may only read and write rows. Pool size 10.
  - `games_owner` owns the tables and runs the migrations at startup (`ConnectionStrings:PlatformMigrations`, with `Database:MigrateOnStartup=true`). Its default privileges give `games_app` access to new tables, so migrations need no GRANT.
  - Without `PlatformMigrations` (Development, tests), the `Platform` login migrates.
- **Settings:** `docker-compose.yml` builds the settings from `.env` (git-ignored, `chmod 600`; names in `.env.example`): `GAMES_VERSION`, `GAMES_DB_APP_PASSWORD`, `GAMES_DB_OWNER_PASSWORD`, `GAMES_SIGNING_KEY`. `databaseip` comes from the servers' `.env`. Onboarding writes the two database passwords, so leave them empty until then.
- **Data protection:** nothing uses it, so its keys live in memory only (`UseEphemeralDataProtectionProvider`). A feature that needs it must keep its keys in the database.

**How a release works.** Run the servers' release script from `platform/`:

```sh
~/Desktop/projects/servers/tools/release.sh --dry-run --yes   # tests and a local image build; nothing leaves the Mac
~/Desktop/projects/servers/tools/release.sh --plan            # what a release would do
~/Desktop/projects/servers/tools/release.sh --yes             # the next patch version
```

A release reads `deploy.conf`. It needs the `SOURCES` there committed on `BRANCH`. It runs `deploy/checks.sh` (`dotnet test`), builds and pushes the image, tags git `games-vX.Y.Z`, deploys the Portainer stack `games` and waits until `/healthz` reports the new version. Roll back with `release.sh --deploy <previous X.Y.Z>`.

The first deploy needs onboarding first (roles, database and `.env` passwords). The servers team does it with `app_onboard.py games --pool 10 --domain games.aqlaan.com`.

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
  → the platform checks it directly with Apple (App Store Server API, by transaction id)
    or Google (Play Developer API, by purchase token, then acknowledges it), using that game's own app ids
  → if valid: record the purchase once (store transaction id is unique), add its gems to the wallet,
    and return its other items (energy, a generator copy, a decor set, an outfit, the pass) with a purchase id;
    the game applies those to its save once per purchase id, then finishes the store transaction
  → Apple / Google later call   POST /games/{slug}/stores/apple/notifications   or   .../stores/google/notifications
    for refunds; the platform marks the purchase refunded and takes its gems back
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
- **Rate limits.** Guest creation, sign-in, refresh and staff sign-in are limited per IP (`RateLimits:AuthPerMinute`, 30). Behind Cloudflare the IP is `CF-Connecting-IP`.

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
| AnalyticsEvent | one game event: name, fields, the game's time, session, version, device. Deleted with the account |
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
| `GET /healthz` | `{ version }`: the release version, for the release script |
| `GET /games/{slug}/time` | `{ now, nowMs }`: server time, so energy can't be cheated by changing the phone clock |
| `GET /games/{slug}/config` | The active balance settings, with its version in the `X-Config-Version` header. A game's `balance.json` (game.json `balanceConfig`) is published as version 1 when none exists; in Development a changed file is published again. Staff publish new versions (below) |
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
| `POST /games/{slug}/players/me/rewards` | `{ id: "levelUp:7" \| "chapter:2" \| "daily:2026-10-31" \| "task:2026-10-31:serve_10" \| "event:<id>:3" }` → `{ granted: { gems }, wallet }`, or `{ alreadyClaimed, wallet }`. Each id grants once. Level-ups and chapters: the latest cloud save must show it, `409 not_earned_yet` until the game uploads it. Daily gifts, tasks and event milestones can't be seen in a save, so they're limited instead: the date within a day of today (`400 wrong_day`), `dailyTasks.perDay` tasks a day (`400 daily_limit`), a running mini board (`400 event_over`); the gift's day in its 7-day cycle is how many gifts were claimed before. `400 unknown_reward` |
| `POST /games/{slug}/players/me/spend` | `{ item, requestId }` → `{ item, gems, grant, wallet }`. Items: `energyRefill` (each refill today costs more), `storageSlot:N` (once), `bubble:L` or `bubble:L:chain` (the chain comes back in the grant), `memoryRetry:<memory>`. A repeated `requestId` returns `{ alreadySpent }` without spending again. `409 not_enough_gems`, `409 already_owned`, `400 unknown_item`. The game applies `grant` only after this reply |
| `POST /games/{slug}/players/me/events` | `{ events: [{ name, fields, timeMs, sessionId }], gameVersion, device }` (up to 200) → `202 { accepted }`. Bad events are dropped, not the batch |
| `POST /games/{slug}/players/me/support` | `{ message, email?, gameVersion, device, language }` → `201 { ticketId }` |
| `POST /games/{slug}/purchases/verify` | `{ store, sku, receipt }` (Apple: the transaction id; Google: the purchase token) → `{ granted, purchaseId, items, wallet }`. Sent again it returns `{ alreadyGranted, purchaseId, items, wallet }` without granting gems twice; a non-consumable restored to another account brings its items only. `400 not_confirmed`, `400 refunded`, `400 unknown_product` |
| `GET /games/{slug}/players/me/purchases` | `[{ purchaseId, sku, status, purchasedAt, items }]`, for restoring items after a reinstall and hiding one-off offers |

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
| `GET /games/{slug}/admin/metrics?days=30` | viewer | Tester gate numbers from game events: players, sessions, players per tutorial step, reached the first memory, found the letter, day 1 return, average session length, error-free sessions |
| `GET /games/{slug}/admin/config` | viewer | Published balance.json versions |
| `POST /games/{slug}/admin/config` | producer | Body: a whole balance.json → `{ version }`, now active. Refused if it isn't JSON or lacks a core section (`energy`, `orders`, `board`, `chains`, `formulas`, `chapters`) |
| `POST /games/{slug}/admin/config/{version}/activate` | producer | Go back (or forward) to a published version |
| `GET /games/{slug}/admin/events` | viewer | Scheduled events, newest first |
| `POST /games/{slug}/admin/events` | producer | `{ template, startsAt, endsAt, settings }`: `miniBoard`, `frenzy`, `pass`, `album` or `hospitality`, up to 60 days. `settings` lay over the template's numbers in balance.json (a mini board's `chain`, `milestoneTokens`, `milestoneRewards`, `name`, `nameEn`) |
| `DELETE /games/{slug}/admin/events/{id}` | producer | Cancel an event |

**Social** (`Authorization: Bearer <player accessToken>`; games/gahwit-sitti/missing/20). Players under 13 or of unknown age get `403 parent_needed` until a parent agrees (`Players.ParentConsent`). Numbers come from the published balance.json (`social`, `levelB.hosting`, `levelB.sittiWall`).

| Method and route | Job |
|---|---|
| `GET /games/{slug}/players/me/social` | `{ friendCode, friends: [{ playerId, code, points, since, giftSentToday }], giftsWaiting, giftsLeftToday }`. The code is made on first use |
| `POST /games/{slug}/players/me/friends` | `{ code }`: both become friends at once; there's no chat. `404 unknown_code`, `400 own_code`, `already_friends`, `too_many_friends` |
| `DELETE /games/{slug}/players/me/friends/{id}` | Unfriend, both ways |
| `POST /games/{slug}/players/me/gifts` | `{ to }`: one energy gift per friend a day, `social.giftsPerDay` a day. `400 not_friends`, `daily_limit`, `already_sent_today` |
| `POST /games/{slug}/players/me/gifts/claim` | `{ gifts, energy }`: every gift waiting; the game adds the energy |
| `GET /games/{slug}/players/me/friends/{id}/cafe` | A friend's café from their latest cloud save, read-only: placed decor, styles, cosiness, chapter and task, Salma's outfit. Nothing else from the save |
| `GET /games/{slug}/players/me/hosting` | `{ mine, helping, canStart }`: the player's newest hosting order and open orders they help. Each: `{ id, hostId, startedAt, endsAt, needed, progress, bots, status, helpers, messages }` |
| `POST /games/{slug}/players/me/hosting` | Start one (`levelB.hosting.perWeek` a week, one at a time) |
| `POST /games/{slug}/players/me/hosting/{id}/invite` | `{ friendId }`, up to `maxHelpers` |
| `POST /games/{slug}/players/me/hosting/{id}/give` | `{ count }` items from the giver's board. When the order fills, the platform pays once: the host `hostGems` (× `soloBotRewardShare` if nobody helped), each helper who gave `helperGems` and `helperFriendship` points both ways. Alone, neighbour bots fill `botShareOfOrder` of it over its time |
| `POST /games/{slug}/players/me/hosting/{id}/message` | `{ preset }`: one of the game's preset messages |
| `GET /games/{slug}/players/me/wall` | `{ posts: [{ id, text, likes, liked, mine }], mine }`: approved posts without names, minus ones the player reported or by players they blocked |
| `POST /games/{slug}/players/me/wall` | `{ text }` up to `maxChars`, `postsPerDay`. The automatic check refuses links, emails, phone numbers (Arabic digits too) and listed words (`400 refused_<reason>`); everything else waits for a person |
| `POST /games/{slug}/players/me/wall/{id}/like` / `report` / `block` | Like toggles. A report hides the post from the reporter, and `reportsToReview` reports send it back to a person. Block hides everything by that writer |
| `GET /games/{slug}/admin/wall?status=pending` | support: the review queue |
| `POST /games/{slug}/admin/wall/{id}/approve` / `reject` | support, audited |

The wall's word list starts short and is extended in configuration (`Wall:BlockedWords`).

**Store notifications** (anonymous; always `200` once read, so the stores stop retrying):
- `POST /games/{slug}/stores/apple/notifications`: App Store Server Notifications v2. The platform reads the transaction id from the notification, asks Apple for that transaction with its own key, and refunds it when Apple says it's revoked.
- `POST /games/{slug}/stores/google/notifications?token=<Stores:Google:PushToken>`: Google Play real-time developer notifications through a Pub/Sub push subscription. A voided purchase is refunded by its order id. A wrong token gets `401`.
- **Refunds:** the purchase is marked refunded and its gems are taken back. A balance never goes below zero: if some gems are already spent, what's left is taken and the ledger notes the shortfall.

**Store keys** (configuration or a secret store, never the repo):
- `Stores:Apple:IssuerId`, `KeyId`, `PrivateKey`: an App Store Connect in-app purchase key (the `.p8` file's text).
- `Stores:Google:ServiceAccountEmail`, `PrivateKey`, `PushToken`: a service account with access to the app in the Play Console, and the secret in the Pub/Sub push URL.
- A store without keys refuses every receipt. On a desk only, `Stores:AllowDevReceipts` (in `appsettings.Development.json`) accepts the Editor's `dev:` receipts so the whole flow can be tried without a store.

## Still needs you

- **Signing key:** a 32+ byte `Auth:SigningKey` in a secret store for every non-Development server.
- **Google sign-in:** create OAuth client ids in Google Cloud (Android, plus a web client id if the game asks for an id token with it). Put them in `game.json` under `stores.google.signInClientIds`.
- **Apple sign-in:** turn on the Sign in with Apple capability for the bundle id. To revoke Apple tokens when an account is deleted (Apple asks for this), the platform needs the Sign in with Apple key (`.p8`, key id, team id) and the Apple refresh token from the first sign-in. Until then `IAppleTokenRevoker` only logs.
- **Store keys:** the App Store in-app purchase key and a Google Play service account (above), the Pub/Sub topic and push subscription for Google's notifications, and the App Store Server Notifications URL in App Store Connect. Until then each store refuses every receipt, so nothing is granted by mistake.

## Staff panel

`/staff/` on the platform: plain HTML and JavaScript over the staff API above (`src/Platform.Api/wwwroot/staff`), served with a strict content security policy. Staff sign in with their email and password, and with a code from an authenticator app once they turn on two-factor sign-in (My account).

Pages, shown by role:
- **Players** (viewer): search, everything about a player, and **compensation** (support), which goes into the audit log.
- **Support** (support): open and closed messages, the player behind each, and closing with a note.
- **Purchases** (viewer): by transaction id, player or product, with refunds.
- **Revenue** (viewer): store sales by day, currency and product. Google's records carry no price; ad revenue arrives with the ad network.
- **Config** (viewer; producer publishes): published versions, comparing a new `balance.json` with the active one, publishing, and going back.
- **Events** (viewer; producer schedules): scheduled events, scheduling and cancelling.
- **Sitti wall** (support): the review queue; approve or reject, and see the writer.
- **Deletions** (support): accounts waiting for the purge.
- **Metrics** (viewer), **Game** (viewer: what game.json registered), **Staff** and **Audit** (owner), **My account** (password, two-factor).

Routes the panel added:

| Method and route | Role | Job |
|---|---|---|
| `POST /admin/me/two-factor` | any | A new authenticator secret `{ secret, uri }`; sign-in asks for codes only after it's turned on |
| `POST /admin/me/two-factor/enable` / `disable` | any | `{ code }` from the authenticator app |
| `POST /admin/sign-in` | | Now also `{ code }`: `401 code_required` or `invalid_code` when two-factor is on. Each 30-second code works once |
| `GET /games/{slug}/admin/game` | viewer | The registered game and its products |
| `GET /games/{slug}/admin/purchases?query=&status=` | viewer | Newest 100 purchases matching a transaction id, player id or sku |
| `GET /games/{slug}/admin/revenue?days=30` | viewer | Sales by day and currency, and by product |
| `GET /games/{slug}/admin/deletions` | support | Accounts waiting for the purge |
| `GET /games/{slug}/admin/config/{version}` | viewer | A published config's text |

Still to come: imported ad revenue, and requiring two-factor for every staff member.
