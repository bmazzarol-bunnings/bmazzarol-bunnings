# Assistants Registry

Discipline assistants on this machine: git-synced folders under `/home/bmazzarol/work/` that pi opens as specialized experts. Create new ones with the `creating-assistants` skill, then **add a row here**.

## Registry

| Assistant | Path | Discipline | Mode | Call on them when |
|---|---|---|---|---|
| frontend-design-assistant | `work/frontend-design-assistant` | Frontend design craft | Self-contained | Designing/prototyping frontend work: component design, design systems, UI decisions, design reviews — briefs go in its `briefs/` |
| sre-assistant | `work/sre-assistant` | Reliability engineering (Grafana, OTel, Elastic, PagerDuty) | Self-contained | SLOs, alert/dashboard design, incident investigation, postmortems, OTel instrumentation — briefs go in its `briefs/` |
| testing-assistant | (not created yet) | The nature of testing | Target-repo | Planning test strategy, reviewing test quality, triaging failures — create with the `creating-assistants` skill when needed |

## Notes

- A row lands here when the assistant folder exists and is pushed; drop the row (or mark RETIRED) when it goes away.
- Assistants are the captain's direct lanes — this registry is for *discovery and routing* ("when should I call on which one?"), not ownership.
- Keep "call on them when" concrete: the situations/phrase triggers, not a scope essay.
