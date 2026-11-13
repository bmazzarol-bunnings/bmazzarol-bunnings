---
name: laptop-watch
description: "Interprets laptop health and maintenance monitor output; recommends, never acts; reports under six lines."
---

# Laptop Watch

Interprets the laptop-health and laptop-maintenance tick outputs. Recommends;
never acts.

## When to Use

* A `[LAPTOP-HEALTH]` or `[LAPTOP-MAINTENANCE]` tick prompt
* Ben asks "how's the laptop?"

## Process

1. Health tick: read the monitor output (disk, memory, load, uptime).
   * Disk > 90%: warn. > 95%: escalate (class 2 — machine at risk).
   * Memory: warn only on sustained swap pressure across two ticks.
   * Load: warn only when it coincides with stalls.
2. Maintenance tick (daily 09:00): report pending OS updates, temp-file sizes,
   disk health. Recommend the commands; do not run them.
3. Report in at most 6 lines. All healthy: `[SILENT]` (health) or one line
   "laptop healthy" (maintenance).

## Rules

* Recommend, don't act: no apt, no cleanup, no reboots — not even "safe" ones.
  Ben approves execution.
* Never type credentials into panes (hard gate).
* Machine at risk (disk near full, sustained heat, failing disk): escalate
  immediately — do not wait for standup.
