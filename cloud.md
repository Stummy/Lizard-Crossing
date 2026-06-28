# cloud.md — cloud + Unity-cloud playbook for Lizard Crossing

> **Why this file (owner ask, 2026-06-27):** so the main session AND the `cloud-engineer` agent both
> share one researched, decision-ready knowledge base for anything cloud/build/backend/release — and
> don't wing expensive infra decisions. **Guiding principle for THIS project: lean, free-tier-first,
> solo-dev. Add a cloud service only when a concrete need appears — never "just in case."** Lizard
> Crossing is a single-player portrait mobile arcade runner (Crossy-Road-style); it does **not** need a
> multiplayer backend, and a custom backend would be overkill.

## What we already use (our current "cloud")
- **Source + large files:** GitHub (`origin`, branch `feat/realistic-city-crossing`) + **Git LFS** for
  GLB/textures/MP4s. Fine for a solo dev; no need for Unity Version Control yet (see Part B).
- **Cloud AI generation (asset pipeline):** **Meshy** (text/image→3D, `Tools/meshy.sh`), **Higgsfield**
  (textures), and the **Gemini** QA reviewer (`Tools/gemini_review.py`). These ARE cloud services we
  depend on. Keys live OUTSIDE the repo (`~/.lizard_secrets/*`), gitignored — keep it that way.
- **Builds:** local Unity batchmode today (no CI). First obvious upgrade = automated builds (below).
- **Backend / analytics / remote config:** **none yet** (correct for pre-beta). Staged plan below.

## The decision framework (use this before adopting ANYTHING)
1. **Is there a concrete, present need?** (a real player problem, not a hypothetical.) No → don't adopt.
2. **Free tier first.** Start on the free tier of a *managed* service; never self-host infra for a solo
   dev unless cost forces it.
3. **Prefer managed BaaS over custom backend.** Custom backends "make sense only when infrastructure
   demands exceed service boundaries" — not our case.
4. **Single-player ⇒ no realtime/multiplayer backend.** Skip Nakama/Photon/PlayFab-multiplayer entirely.
5. **Watch the meter:** storage + **egress** + build-minutes are the usual surprise bills. Re-check
   pricing before scaling (Unity has changed pricing before — see Risk watch-list).
6. **Keep secrets out of the repo.** Service keys/tokens → secret store / CI secrets, never committed.

---

## PART A — Cloud for game developers (the general landscape)

### A1. CI/CD — automated build + test (the first cloud upgrade worth doing)
- **GameCI + GitHub Actions** — *free, community, open-source*; Docker images with Unity pre-installed;
  runs tests + builds on push. Best fit for us because **we're already on GitHub** and it's free for
  private repos within the GitHub Actions free-minutes allotment. Needs a Unity license activated as a
  CI secret. → **Recommended path** when we want hands-off builds/tests.
- **Unity Build Automation** (UGS DevOps, formerly Cloud Build) — managed cloud builds, no Docker/YAML to
  maintain, signs iOS/Android/UWP. Easiest "it just builds" option, but mobile build-minutes are limited
  on the free tier (2026: +100 *Mac* build minutes free) and it bills beyond that. → Pick this only if we
  want zero-config managed builds and will pay for minutes.
- **Verdict for us:** stay local until builds become a chore, then **GameCI/GitHub Actions** (free, fits
  our stack). Build Automation is the paid convenience alternative.

### A2. Backend-as-a-Service (BaaS) — only when we need persistence/economy/liveops
- **Firebase** (Google) — mobile-first, indie-favourite. Free/low-cost tiers for **Analytics**,
  **Crashlytics** (crash reporting), **Remote Config** (live-tune without an app update), Cloud
  Functions, and a realtime DB for **cloud save**. Not a game-multiplayer server — but we don't need one.
  → **Our default backend when we add one.**
- **GameAnalytics** — free analytics *built for games* (funnels, progression, retention, DAU). Lighter
  than Firebase for pure analytics. → Good free analytics alternative/companion.
- **PlayFab** (Microsoft/Azure) — all-in-one live-service backend (accounts, economy, leaderboards,
  analytics, liveops) with a free tier. → Consider only if we go full live-service with an economy.
- **LootLocker** — indie-friendly progression/inventory/leaderboards via simple APIs. → Nice if we want
  leaderboards + player progression fast without standing up Firebase.
- **Nakama** (Heroic Labs) — open-source, self-hostable, powerful, but **demands real DevOps** (Go/TS/Lua
  server logic, infra). → **Skip** for a solo single-player game.
- **Verdict for us:** when persistence/liveops is needed, **Firebase** (Crashlytics + Analytics + Remote
  Config first; Cloud Save only if cross-device progress is wanted). Leaderboards → LootLocker or Firebase.

### A3. Analytics + crash + Remote Config (highest ROI cloud for a small game)
- These three are the *first* cloud services worth adding at **beta** — they're cheap/free and directly
  improve the game: **Crashlytics** catches device crashes you'll never reproduce locally; **Analytics**
  shows where players quit (tune difficulty/onboarding); **Remote Config** lets you re-tune balance
  (speeds, spawn rates, hearts) **live without shipping an app update or new code** — huge for an arcade
  runner.
- Cheapest stack: **Firebase Crashlytics + GA4/Firebase Analytics + Firebase Remote Config**, or
  **GameAnalytics** for the analytics half. All free at our scale.

### A4. Distribution (cloud, but storefront-side)
- iOS → **TestFlight** (beta) → App Store. Android → **Play Console internal/closed testing** → Play
  Store. Both are "cloud" in that builds upload to Apple/Google; CI (A1) can auto-upload (Fastlane).
- We **cannot** create store accounts or pay fees — the **owner** does Apple/Google enrollment, signing
  certs, and store submission. Claude can prep build configs + Fastlane scripts only.

### A5. Storage / asset egress (the silent cost)
- Our GLB/MP4 assets are big and on **Git LFS** — fine, but LFS has bandwidth/storage quotas on GitHub;
  watch them. Generated source assets (Megascans, Meshy outputs) that don't ship can stay out of the repo
  (we already moved the dormant rigged GLB out of `Resources/` to avoid build bloat).
- Don't ship unused assets in `Resources/` (force-included in the build) — a recurring cost lever.

---

## PART B — Unity's cloud stack (UGS / Unity DevOps / Unity Cloud)

**Unity Gaming Services (UGS)** is Unity's pay-as-you-go, modular cloud platform — activate only the
modules you need from the Unity Dashboard + SDK. Develop/launch free; pay only past free tiers.

### B1. Unity DevOps (two modules)
- **Build Automation** — cloud multiplatform CI (see A1). Free tier is small for mobile; bills per
  build-minute beyond it.
- **Unity Version Control (UVCS, formerly Plastic SCM)** — VCS optimized for large binaries + art/code
  workflows; supports Git/SVN/Hg/Perforce interop. **2026 free tier: unlimited seats in public cloud +
  25 GB cloud storage.** → Better than Git+LFS for *binary-heavy teams*; for our **solo Git+LFS** setup
  it's not worth switching unless LFS quotas/merge pain bite.

### B2. UGS live modules (adopt à la carte, only on need)
- **Cloud Save** — player data/progress to the cloud, cross-device. (Alt: Firebase.)
- **Remote Config** — launch features / tune values without an app update. (Alt: Firebase Remote Config.)
- **Cloud Code** — serverless game logic (has its own metered cost). Only if we need authoritative logic.
- **Economy / Authentication / Analytics / Leaderboards** — standard live-service building blocks.
- **Multiplayer suite (Relay / Lobby / Matchmaker / Vivox)** — **N/A** (single-player).

### B3. 2026 free-tier facts (verify before relying)
- Pay-as-you-go free tier expanded: **25 GB storage** (up from 5), **+100 Mac build minutes**, **100 GB
  free egress**; **UVCS unlimited free seats** in public cloud. Paid *Editor* subscriptions rose in 2025
  but free subs were extended. UGS itself is develop-free, pay-on-scale.

### B4. UGS vs roll-your-own / Firebase for us
- UGS is "ideal for mobile/mid-market live titles that prioritize speed + scalability." For a **solo,
  single-player** arcade runner, **Firebase covers our realistic needs (crash/analytics/remote-config/
  cloud-save) more simply**, and we're not in the Unity multiplayer ecosystem. Use UGS modules only if we
  later want everything inside the Unity Dashboard, or need a Unity-native feature (e.g. Cloud Diagnostics
  tied to the editor). Keep both on the table; default to whichever has the simpler free path for the
  specific need.

---

## Staged plan for Lizard Crossing (what to adopt, in order)
1. **Now (pre-beta):** keep GitHub + LFS + the AI-gen cloud tools. **No backend.** **CI pipeline is
   built** (`.github/workflows/ci.yml`, GameCI) — runs EditMode+PlayMode tests incl. the **Invariant
   spatial gate** (`InvariantTest`) + the bot playthrough on push/PR to `main` +
   `feat/realistic-city-crossing`; Linux runner (1× minutes), LFS + Library cached. Android build job
   stubbed (owner keystore). Keep `Resources/` lean.
   - **⚠️ Unity CI licensing reality (verified 2026-06-28):** Unity **deprecated manual activation for
     Personal licenses**, AND Unity 6's new Licensing Client leaves **no reusable `Unity_lic.ulf`** on
     disk — so both the old `.alf`→`.ulf` manual flow and the "grab the local `.ulf`" trick are DEAD for
     a free Personal license. To run GameCI in the cloud on Personal you now need ONE of:
     (a) **`game-ci/unity-license-activate`** — logs in headlessly with `UNITY_EMAIL` + `UNITY_PASSWORD`
     secrets to fetch a license each run (works on Personal; breaks if the Unity account has 2FA without
     a TOTP secret; means your Unity password lives in a repo secret — acceptable only for your own
     private repo); (b) a **self-hosted runner** on a machine with Unity already activated (no license
     secret, but the machine must be on); (c) **Unity Pro/Plus serial** (`UNITY_SERIAL`+email+password —
     paid). Until one is set up, **CI is dormant and we rely on the local verify loop** (the same tests
     run in-editor on every change). Revisit at beta or when a collaborator joins.
2. **Beta:** add **Firebase Crashlytics + Analytics + Remote Config** (free). Crash visibility + a
   live-tunable difficulty/economy. Owner handles store enrollment + TestFlight/Play internal testing.
3. **Launch:** wire CI → Fastlane auto-upload to TestFlight / Play (owner provides signing). Remote Config
   becomes the LiveOps balance knob.
4. **Post-launch / LiveOps (only if the game warrants it):** Cloud Save (cross-device), leaderboards
   (LootLocker/Firebase), A/B tests via Remote Config. Revisit UGS vs Firebase per the simpler free path.

## Cost + risk watch-list
- **Re-check Unity pricing before scaling** — Unity has changed pricing/terms before (the 2023 Runtime
  Fee was later reversed; 2025 subscription price rises). Don't architect around a free tier that can move.
- **Egress + storage + build-minutes** are the usual surprise bills — monitor LFS bandwidth, cloud egress,
  CI minutes.
- **Secrets discipline:** service keys → CI secrets / secret store, never the repo (we already keep
  Meshy/Gemini keys in `~/.lizard_secrets/`). Rotate any key that's ever been pasted in chat/terminal.
- **Claude can't make purchases or create accounts** — shortlist + configure; the **owner** enrolls/pays.
- **Don't add complexity the game doesn't need** — every service is a dependency, a cost, and an attack
  surface. For a solo single-player runner, less is more.

## Sources (researched 2026-06-27)
- Unity Gaming Services / DevOps / pricing: [unity.com/products/unity-devops](https://unity.com/products/unity-devops),
  [UGS overview](https://docs.unity.com/ugs/en-us/manual/overview/manual/unity-gaming-services-home),
  [UGS pricing](https://unity.com/products/gaming-services/pricing),
  [2026 pricing changes](https://www.cgchannel.com/2025/11/price-of-paid-unity-subscriptions-to-rise-but-free-subs-extended/),
  [CI/CD solutions](https://unity.com/solutions/ci-cd), [LiveOps](https://unity.com/features/liveops)
- CI/CD for indies: [GameCI](https://game.ci/docs/github/getting-started/), [game-ci/unity-actions](https://github.com/game-ci/unity-actions),
  [CI/CD pipeline for Unity](https://www.simonh.dev/post/ci-cd-pipeline-for-unity)
- Backends / analytics: [choosing a mobile game backend](https://games.themindstudios.com/post/how-to-choose-mobile-game-backend/),
  [best backend providers 2026 (Metaplay)](https://www.metaplay.io/blog/best-game-backend-providers),
  [2025 mobile game tech market map](https://www.gamemakers.com/p/the-definitive-mobile-game-tech-market),
  [mobile game analytics tools 2025](https://thinkingdata.io/blog/7-best-mobile-game-analytics-tools-for-data-driven-growth-in-2025/)
