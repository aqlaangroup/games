# Aqlaan Platform

One .NET application that serves every game in `games/`: players, saves, payments, game settings, live events, analytics and one admin panel.

**Stack:** .NET 10, ASP.NET Core, PostgreSQL, EF Core. The admin web app comes later, in the same solution.

## How money flows

```
Player buys in the game (Unity IAP)
  → Apple / Google charge the player and send the game a receipt
  → the game sends the receipt to   POST /games/{slug}/purchases/verify
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

## Data model

Every table has a `GameId`.

| Entity | What it holds |
|---|---|
| Game | slug, names, Apple bundle id, Google package name, store credentials (in a secret store, not the database) |
| Player | one account per game: Apple or Google sign-in id, created date, country |
| PlayerSave | the player's latest save (JSON) and its version, for cloud save and conflict checks |
| Wallet | gems and other paid currencies, changed only on the server |
| Product | sku, type (consumable, non-consumable, subscription), what it grants |
| Purchase | store, store transaction id (unique), sku, price, currency, status (verified, refunded), raw receipt |
| ConfigVersion | a published copy of the game's `balance.json`, with version and date; the game loads the active one |
| LiveEvent | template, start and end, settings; lets the team run events without app updates |
| AnalyticsEvent | forwarded game events, or a pointer to the analytics tool |
| AdminUser | staff accounts with a role per game (owner, producer, support, viewer) |

## API (first version)

| Method and route | Job |
|---|---|
| `GET /health` | Health check |
| `POST /games/{slug}/players/sign-in` | Sign in with an Apple or Google identity token; create the player if new |
| `GET /games/{slug}/players/me/save` · `PUT …/save` | Cloud save, with a version number to stop older saves overwriting newer ones |
| `GET /games/{slug}/time` | Server time, so energy can't be cheated by changing the phone clock |
| `GET /games/{slug}/config` | The active balance settings |
| `GET /games/{slug}/events` | Live events running now |
| `POST /games/{slug}/purchases/verify` | Verify a receipt and grant items |
| `POST /webhooks/apple` · `POST /webhooks/google` | Store notifications: refunds, renewals, cancellations |

## Admin panel (to come)

One web app for all games:
- **Games:** add a game from its `game.json`.
- **Players:** look up a player and fix their wallet.
- **Purchases and refunds:** see every purchase and refund.
- **Config:** publish a new `balance.json`.
- **Live events:** schedule events.
- **Revenue dashboards:** store and ad revenue per game and per country.

## Status

The projects in `src/` are a starting skeleton. They have not been compiled yet, because the .NET SDK can't be installed in the workspace where this was written. The store verification is a stub that refuses every receipt until the real Apple and Google calls are added.
