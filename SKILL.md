---
name: mayor
description: "Operate Cities: Skylines 1 through the Skylines Agent Bridge mod and localhost API. Use when Codex needs to build, inspect, repair, resume, save, or continue a CS1 city using focused API calls rather than screenshot recognition, including road connectivity, service infrastructure, zoning, facilities, problem icons, and save verification."
---

# Cities: Skylines 1 Agent Skill

Use this skill to control a running Cities: Skylines 1 city through the local Skylines Agent Bridge API at `http://127.0.0.1:32123`.

## Core Rules

- Prefer API state over image recognition.
- Resume and repair existing saves by default. Start fresh only when explicitly requested.
- Use small, separated commands: inspect, bulldoze, build, place, move, zone, simulate, save.
- Save after meaningful city changes and verify the `.crp` file exists.
- Commit repository changes after each coherent code/docs task when working inside this repository.

## API Reference

This file covers the common inspect/repair/save loop below. The mod exposes
32 routes total (state reads, prefab lookups, and commands), including some
not used here: `/state/demand`, `/state/growables`,
`/state/external-connections`, `/state/saves`, `/prefabs/roads`,
`/prefabs/networks`, `/prefabs/buildings`, `/commands/build-road`,
`/commands/move-building`, `/commands/set-building-active`,
`/commands/disable-blocked-assets`, `/commands/repair-zones-to-growables`,
`/commands/repair-zone-clusters`, `/commands/batch`. See
[docs/api.md](docs/api.md) for the complete reference with full request and
response shapes.

## Inspection Loop

Use these before acting:

```bash
curl -s http://127.0.0.1:32123/health
curl -s http://127.0.0.1:32123/state/summary
curl -s "http://127.0.0.1:32123/state/chirps?limit=50"
curl -s http://127.0.0.1:32123/state/zones
curl -s "http://127.0.0.1:32123/state/problems?limit=200"
curl -s "http://127.0.0.1:32123/state/economy"
curl -s "http://127.0.0.1:32123/state/road-anomalies?limit=500&nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=false"
curl -s "http://127.0.0.1:32123/state/building-anomalies?limit=200"
curl -s "http://127.0.0.1:32123/state/zone-anomalies?limit=200&includeUnzonedHoles=true"
curl -s "http://127.0.0.1:32123/state/facilities?limit=500"
curl -s "http://127.0.0.1:32123/state/growables?limit=500"
curl -s "http://127.0.0.1:32123/state/networks?limit=1000&service=Road"
```

Use `includeMapObjects=true` on `/state/facilities` only when raw helper objects such as pipe junctions are needed.

## Command Pattern

Use separate commands so the repair remains auditable.

Delete a bad segment:

```bash
curl -s -X POST http://127.0.0.1:32123/commands/bulldoze \
  -H "Content-Type: application/json" \
  -d '{"entityType":"netSegment","id":19023,"keepNodes":false}'
```

Build a network segment:

```bash
curl -s -X POST http://127.0.0.1:32123/commands/build-network \
  -H "Content-Type: application/json" \
  -d '{
    "roadPrefab": "Basic Road",
    "start": {"x": 400, "z": 300},
    "end": {"x": 423.614, "z": 554.945},
    "name": "Agent Highway Link"
  }'
```

Place or move a building:

```bash
curl -s -X POST http://127.0.0.1:32123/commands/place-building \
  -H "Content-Type: application/json" \
  -d '{"buildingPrefab":"Water Tower","position":{"x":120,"z":-220},"angleDegrees":0}'
```

Paint zones:

```bash
curl -s -X POST http://127.0.0.1:32123/commands/set-zone \
  -H "Content-Type: application/json" \
  -d '{"zone":"ResidentialLow","preserveOccupied":true,"center":{"x":240,"z":-40},"radius":70}'
```

Run simulation:

```bash
curl -s -X POST http://127.0.0.1:32123/commands/set-simulation-speed \
  -H "Content-Type: application/json" \
  -d '{"paused":false,"speed":3}'
```

Lower taxes when `/state/problems` reports `TaxesTooHigh`:

```bash
curl -s -X POST http://127.0.0.1:32123/commands/set-tax-rate \
  -H "Content-Type: application/json" \
  -d '{"service":"Commercial","rate":9}'
```

Save and verify:

```bash
curl -s -X POST http://127.0.0.1:32123/commands/save \
  -H "Content-Type: application/json" \
  -d '{"name":"AgentAutoSave-clean"}'
curl -s http://127.0.0.1:32123/state/saves
```

## Known Gotchas

- CS1 network crossings are not intersections unless a real node is created. If a pipe or road visually crosses another segment but does not connect, bulldoze and rebuild split segments with a shared endpoint.
- Heating service buildings may create an actual connection helper offset from the building center. Query `/state/facilities?service=Water&includeMapObjects=true` when diagnosing heating pipe issues.
- Roads that look connected to highways can still have separate nodes. Use `/state/road-anomalies` and rebuild with endpoints close enough to reuse the existing road nodes.
- Do not treat all dead ends as errors. Use bounded checks or `includeDeadEnds=false` unless the user asks to remove cul-de-sacs/stubs.
- Use `/state/zone-anomalies` when zone colors look mottled or circular paint left residential/commercial/industrial/office cells mixed in the same block.
- `/commands/set-zone` defaults to `preserveOccupied=true`; check `/state/growables` first and keep that flag enabled unless the user explicitly wants to repaint developed blocks.
- If screenshots show blue/green/yellow mottling across a whole city block, use `/state/zone-anomalies` and repair with `/commands/repair-zone-clusters` using `preferGrowableZone=true`.
