# Retro — #77 Copilot feedback rounds on dotnet.common#1785 (NativeAOT release assets)

Date: 2026-11-17 · Outcome: SHIPPED (PR dotnet.common#1785 merged 04:37 UTC 11-17)

## Timeline
- 18-16 (yesterday): Ben: "worker to look at copilot comments" → worker-3 round 1 (3 comments: GH_REPO, smoke-gate removal, Version/PackageVersion)
- 19:07: new Copilot comment (shell continuations) lands AFTER round-1 handback + pane close → round 2 re-dispatch
- Round 2 also cleared the two merge blockers: Issue Gate (tracking issue #1808 + Fixes keyword) and CodeQL high (untrusted checkout in privileged workflow_run)
- Ben: base64'd the Apple signing assets (p12 + notary JSON via rcodesign 0.29.0 `encode-app-store-connect-api-key`, password raw) → 3 secrets set via CLI into `release-signing` env; temp files wiped
- 11-17 12:37: Ben merged

## What worked
- Copilot caught a genuinely broken workflow (multi-line `run: |` without continuations = options silently dropped; assets would never have been produced)
- Worker obeyed the no-label rule (raised #1808, left the stamp to Ben)
- Secrets handling: values never in chat/log; rcodesign-generated JSON = byte-exact for the pinned binary

## What didn't
- Round-1 handback raced the new comment (pane closed before comment landed) → extra re-dispatch. Acceptable cost; no fix needed (comments can always arrive late)
- Issue Gate + CodeQL were only in the worker's scope after I read the round-1 handback note — cards for "make mergeable" should pre-declare the full CI surface

## Improvements
1. DONE — card #77 closed, state cleaned, worktree/branches deleted
2. OPEN (rivet) — for org-repo PRs, add "check ALL failing checks, not just the flagged ones" to feedback-round task bodies (lesson: "pre-existing" failures on the PR's own commits are in scope)
3. OPEN (ben) — runner fleet: `generic`/`script` labels + AOT toolchain/RID coverage (win-x64 + osx-* can't cross-build from Linux-only) — first real release is the proof

## Follow-ups
- First `release`-event CD Build will be the live test of the whole AOT pipeline (secrets, signing, notarization, attach)
- If notarization fails: rcodesign docs say use Apple's macOS tooling first as existence proof
