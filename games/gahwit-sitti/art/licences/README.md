# Licences

Every font, piece of music, sound, asset-store pack and AI tool used for shipped content, with its licence (COMPLETE_SPEC.md section 14). Keep each licence file beside this one, or say where it lives.

| What | Used for | Licence | Where | Checked |
|---|---|---|---|---|
| Noto Naskh Arabic | All in-game text | SIL Open Font License 1.1 | `unity/Assets/Content/Resources/Fonts/NotoNaskhArabic-OFL.txt` | Yes: free for commercial use and embedding |
| RTLTMPro (hk1ll3r) | Arabic text shaping | MIT | Its package's `LICENSE` | Yes |
| ink and ink-unity-integration (inkle) | Dialogue | MIT | inkle's repositories | Check the package's licence file before shipping |
| Unity packages (URP, Localization, Addressables, Input System, Mobile Notifications, IAP) | Engine | Unity Companion License / Unity terms | Unity | Covered by the Unity licence |
| ElevenLabs | Draft music, effects and Teta's draft voice note (`tools/audio_drafts.py`, `tools/voice_drafts.py`) | ElevenLabs terms of service | elevenlabs.io | **Check:** the plan used must allow commercial use; drafts are placeholders until final audio (missing/14) |
| Google Flow (Nano Banana Pro and Nano Banana 2.1) | The 23 characters' portraits, the 49 item icons and the app icon (`art/flow/`, prompts and links in `art/flow/manifest.json`), made 2026-10-31 and 2026-11-01 on the owner's Google account | Google's terms for generated content | Google | **Check** before shipping: commercial use on the plan used. `PLAN.md` section 6 says characters and key art are made by people, so these are drafts for an artist to redo or approve (missing/13) |
| Sentry SDK for Unity (getsentry) | Crash reports, sent to our own GlitchTip | MIT | Its package's `LICENSE` | Yes |
| Apple Vision (on this Mac) | Cutting the Flow art out of its backdrop (`tools/art/lift.swift`); nothing ships with it | Part of macOS | Apple | Yes: a local tool, no content of its own |

When something is added, add a row, and keep the licence or a link to it here.
