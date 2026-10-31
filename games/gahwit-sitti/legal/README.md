# Legal pages and privacy forms

**Status (2026-10-31): drafts.** Written from what the game and platform actually collect (`DATA_INVENTORY.md`). They need a lawyer's review before they go live, above all for children's consent under the UAE Child Digital Safety Law (from 1 January 2027) and Egypt's rules for under-15s.

## Files

| File | What it is |
|---|---|
| `privacy.ar.html`, `privacy.en.html` | The privacy policy |
| `terms.ar.html`, `terms.en.html` | The terms of use |
| `build_pages.py` | The texts; run `python3 legal/build_pages.py` after changing them |
| `DATA_INVENTORY.md` | Every piece of data, where it lives, and draft answers for Apple's privacy label and Google's Data safety form |

The platform serves the pages at `/games/gahwit-sitti/legal/privacy?lang=ar` and `/games/gahwit-sitti/legal/terms?lang=en`. The game's Settings open them in the player's language (`platform.json` `privacyUrl`, `termsUrl`).

## Fill in before publishing

Every `[bracket]` in `build_pages.py`:
- the company's legal name and address
- the support email
- the hosting provider and the region the servers are in
- the jurisdiction for the terms
- the publication date

## Also before the store builds

- Re-check `DATA_INVENTORY.md` when any SDK is added (LevelPlay for ads, crash reporting), and update the policy's "Ads" section to name the ad partner.
- The iOS privacy manifest is `unity/Assets/Plugins/iOS/PrivacyInfo.xcprivacy`. Keep it matching the inventory.
