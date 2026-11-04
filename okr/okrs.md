# OKRs — Ben Mazzarol (FY2026-27)

Source: Workday "View Goal" exports (bot-impermeable — updated manually when Ben re-exports).
Last captured: 2026-11-04 (4 goals).

These OKRs are the reference for triage criteria 3–4 (platform-vision fit, OKR alignment)
when ranking incoming work without asking Ben each time.

## Role context (2026-11-04, walkthrough)

Ben is effectively the **product owner for DevX** in the platform team. Consequences for triage:

* Work that improves DevX → the DevX bucket (map #2 / INTPLAT-375), even if it arrives framed as a different concern.
* Other platform work (e.g. observability KAPs) is generally **owned by the wider platform team**; Ben's contribution is via DevX improvements and enabling work for his buckets. Not a direct delivery gap for him unless a specific item lands on his desk.
* **G4 (AppSec)** is a **DevSec business outcome**, reached via the **Valkyrie farm** — the in-house agent farm the org is investing in. Ben + Rivet *support* the farm (map #26 / company Agentic Workboard); AppSec-flavoured asks route there, no separate bucket.

---

## Goal 1 — Observability in prod & preprod

- **Title:** Observability is built into all production and preproduction environments
- **Category:** Priority 1 — Simpler and Better (Public)
- **Due:** 31/12/2026
- **Set by:** Eric Chapman, 24/08/2026
- **Status (Workday):** Not Started

Key action points:
1. **CMDB Completeness & Ownership** — end-to-end service mapping in the CMDB; 100% of CIs accurately logged with designated primary owners.
2. **User Experience & Observability** — design and deploy UX monitoring dashboards across all critical platforms/applications; track real-time system performance and user impact.
3. **Incident Recovery Optimization** — establish MTTR baseline for critical incidents; automated triage and recovery workflows; reduce critical-incident MTTR by 20% vs baseline.
4. **Automated Testing & Validation** — continuous automated testing/validations across critical application pipelines; use observability data to identify, track, and remediate systemic exceptions.

## Goal 2 — AI cost, velocity, world-class engineering capability

- **Title:** Using AI responsibly, identify and execute on a plan to reduce AI costs, increase velocity and build a world class engineering capability in Bunnings at scale
- **Category:** Priority 1 — Accelerated by Data and AI (Public)
- **Due:** 30/04/2027
- **Set by:** Eric Chapman, 24/08/2026
- **Status (Workday):** Not Started

Key action points:
1. **Cost Optimization & Velocity** — AI implementation plan that reduces operational costs while measurably increasing delivery velocity; governance frameworks so all AI tools/models are leveraged responsibly and securely at scale.
2. **Agentic Engineering Capability** — design and deploy an agentic engineering architecture automating complex software development and operational workflows; upskill and empower engineering teams to build, orchestrate, and maintain AI agents.
3. **24/7 Automated Engineering Workforce** — collaborate cross-functionally to implement a continuous 24/7 engineering pipeline using cost-effective AI models; monitoring, guardrails, and handoff protocols for continuous output and reliability.

## Goal 3 — Embedding AI in everyday work

- **Title:** Embedding AI in everyday work and demonstrating through key productivity metrics
- **Category:** Accelerated by Data and AI (Public)
- **Due:** 30/06/2027
- **Set by:** Richard Crane, 21/09/2026
- **Status (Workday):** (not shown in export)

Description:
- Including software development as well as non-coding related tasks
- Demonstrating reduction in unit effort for key tasks in the team

## Goal 4 — AI-driven vulnerability management & appsec posture

- **Title:** Leverage AI and agentic engineering capabilities to automate vulnerability management, accelerate patching velocity, and improve the overall application security posture across all engineering repositories
- **Category:** Priority 1 — Accelerated by Data and AI (Public)
- **Due:** 30/04/2027
- **Set by:** Eric Chapman, 24/08/2026
- **Status (Workday):** Not Started

Key action points:
1. **Critical Finding Remediation** — AI + automated agent workflows to triage, fix, and resolve critical AppSec findings within 30 days across all code repositories.
2. **Production Security Guardrails** — continuous CI/CD security gates driven by agentic capability; no code with unresolved medium-or-higher vulnerabilities deploys to production.
3. **Rapid Patch Automation** — automated detection and rapid-patching pipelines; critical vulnerabilities on critical assets remediated within 48 hours of discovery.
4. **Overall Risk Reduction** — measure, track, and reduce the organization's unified risk score by 15% through proactive, AI-driven threat mitigation and automated security remediation.

---

## Mapping to delivery streams (DRAFT — confirm with Ben in walkthrough)

| Goal | Likely-aligned work (from GH inventory 2026-11-04) |
|------|----------------------------------------------------|
| G1 Observability | Mostly platform-team owned (see role context). Ben's angle: INTPLAT-1770/1769, bunnings-agentic #60-63 (OTel → ELK) as enabling work |
| G2 AI capability | Digital 2IC (#26); Agentic Eng & Platform Tooling (#2); AOT copilot-am (#1754); ConsoleAppFramework migration (#1761); bunnings-agentic harness |
| G3 Everyday AI | Cross-cutting: Hermes usage, copilot skills, agent-assisted PRs across all repos |
| G4 AppSec | DevSec outcome via Valkyrie farm (see role context). Ben/Rivet support the farm; dotnet.common CodeQL sweep (#1734-1739) is one instance |

Categorisation complete (walkthrough 2026-11-04):
- item Cosmos migration (#116-119) → bucket #3 Search & Discovery (item = semantic-capability test bed)
- edp-dbt devx standard (#12282+) → bucket #2 DevX (incremental enablement; politics block wholesale change)
- loyalty pagination (#473-476) → handed to nleroux-bunnings (capability already in dotnet.common; Ben considers complete)
- powerpass portal E2E (#347-355) → handed to delvinnair (may close when happy)
- plsql.common bulk (53 open) → handed to Samccull-Bunnings (May 2026 import backlog)
