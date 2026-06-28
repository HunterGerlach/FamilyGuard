# Platform Roadmap

## Vision

FamilyGuard is a modular family computer guidance platform. Each "family member" tool addresses a distinct aspect of safe, productive computer use. All tools share the same service infrastructure and work together seamlessly.

## Architecture for Multi-Tool Support

```
FamilyGuard.Service          Shared Windows service (hosts all tools)
FamilyGuard.Infrastructure   Shared persistence, IPC, platform adapters
FamilyGuard.Application      Shared policy engine, event bus, use cases
FamilyGuard.Domain           Shared entities, events, value objects

DAD.Policies                 DAD-specific policy rules and actions
MOM.Policies                 MOM-specific policy rules and actions (future)

FamilyGuard.Agent            Per-user agent (loads all active tool policies)
FamilyGuard.UI               Shared tray icon + UI shell (tabs per tool)
```

## Current Tools

### DAD — Digital Activity Defender

**Focus:** Endpoint protection — detecting unattended sessions and taking protective actions.

**V1 Policy:** Auto-mute microphone when user appears to have walked away.

**Future DAD Policies:**
- Camera safety (alert or disable camera when idle)
- Session awareness (detect abandoned game sessions)
- Device management (respond to mic/camera plug/unplug)

### MOM — (Name TBD)

**Focus:** Guidance and education — helping children develop healthy computer habits.

**Potential Policies:**
- Game time limits (per app, per day, per schedule)
- App rules (allow/block per user, time, day)
- Bedtime enforcement (school night vs weekend schedules)
- Extra time approval (parent remote confirmation)
- Family modes (school night, weekend, vacation, guest)

### Future Tools

Additional family member tools may address:
- Install control (parent approval for new apps/mods/launchers)
- Web/network rules (DNS/router/filtering integration)
- Remote parent controls (lock computer, approve requests)
- Audit and reporting (weekly summaries, trends)

## Shared Infrastructure

All tools benefit from:
- **Policy engine** — general-purpose rule evaluator with extensible conditions and actions
- **Presence state machine** — shared understanding of user activity
- **Event store** — unified structured event log across all tools
- **Settings repository** — centralized configuration with PIN protection
- **Migration framework** — schema versioning across updates
- **Update system** — signed manifests, service-managed updates
- **IPC** — named pipe communication between service and agents

## Adding a New Tool

1. Define policies in a new `ToolName.Policies` namespace
2. Register policy conditions and actions with the shared `PolicyEngine`
3. Add UI tab in the shared `FamilyGuard.UI` shell
4. Add migrations for any new database tables
5. All existing infrastructure (events, settings, PIN, tray icon) is reused
