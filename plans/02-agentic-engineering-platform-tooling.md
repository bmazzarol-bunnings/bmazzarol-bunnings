# Agentic Engineering & Platform Tooling

Map: [#2](https://github.com/bmazzarol-bunnings/bmazzarol-bunnings/issues/2) · Jira: INTPLAT-375 (DevEx Uplift, parent), INTPLAT-1205 (Platform Auth CLI), INTPLAT-1206 (Post-Clone workflow), INTPLAT-406 (Solace MCP in DevX), INTPLAT-555 (MCP metrics libraries)

## Vision

A single entry point for agent-assisted development. **bagc** (agentic API
CLI, dotnet.common#1747) is the vehicle: platform auth + API access in one
tool, a post-clone agentic workflow (intake prompt → requirements breakdown →
guided implementation), and MCP tooling (Solace MCP in DevX, common MCP
metrics libraries). Everything documented in approved plans and broken into
implementation issues.

Prefer .NET tooling to match the platform ecosystem unless a better reason
emerges.

## Scope

In:
- Platform Auth CLI (hybrid auth: OAuth where vendors support it, validated
  wallet entries where they don't; .NET global tool; GH/Atlassian/Octopus/
  Grafana + FIDP SSO anchor in v1)
- Post-clone agentic workflow — on-demand, no new trigger; extends the
  existing init-dev-environment.ps1 convention; `starting-work` front-door
  skill delegates to delivering-projects + supporting skills
- Solace MCP in DevX (leading candidate: "MCP-ify empx")
- MCP metrics (likely config-only on IOP MCP server; SDK already ships
  OTel-convention metrics)

Out:
- Token brokering in FIDP (deferred follow-up)
- Command proxying in the auth CLI (env-export surface only)

## Current state

- Auth landscape researched (#4), training delivered (#5), CLI design
  approved (#6), post-clone workflow design approved (#7).
- Open: what "DevX" means (agent tooling vs DevHub portal) + which ops in
  v1 for INTPLAT-406; packaging/distribution of the auth CLI.
- Frontier: implementation issues per approved plans.

## Evolution log

- 2026-11-04 — Plan doc created from map #2 (bucket standardisation, issue #34).
