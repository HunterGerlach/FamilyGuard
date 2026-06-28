# Configuration

All configuration is accessed through the DAD tray icon: right-click and select **Settings**. Settings are protected by a parent PIN.

## Parent PIN

On first run, DAD prompts the parent to create a PIN (minimum 4 characters). This PIN is required to:

- View or change settings
- Add or remove covered users
- Modify notification preferences

PIN security:
- Stored as PBKDF2-HMAC-SHA256 with random salt (210,000 iterations)
- Rate-limited: 5 attempts before 15-minute lockout
- Lockout logged as a security event

## Presence Timeout

**Setting:** "Consider computer unattended after: N seconds"

- Default: 90 seconds
- Range: 30–300 seconds
- Applies equally to keyboard, mouse, and controller inactivity

The presence state machine:

| State | Meaning | Triggers at |
|---|---|---|
| Present | Recent input detected | Any keyboard/mouse/controller activity |
| Likely Away | Inactivity nearing threshold | 75% of timeout |
| Away | No input for full threshold | 100% of timeout |

The mic auto-mute policy fires only when presence state is **Away** AND the default communications microphone is **unmuted**.

## Covered Users

Add Windows usernames whose sessions should be monitored. If no users are specified, all sessions are monitored.

Users are matched case-insensitively and deduplicated.

## Notifications

| Setting | Default | Effect |
|---|---|---|
| Show toast on auto-mute | On | Balloon notification when mic is muted by policy |
| Show tray warning when likely away | On | Tray icon turns yellow when mic is open and user may be leaving |

## Policy Rules

DAD ships with one built-in rule:

```
Rule: mute_unattended_microphone
  When: microphone is unmuted AND presence is away
  Then: mute microphone, notify child, log event
```

Policy rules are stored in SQLite and can be extended in future versions. The policy engine reads `presence.state` — it never duplicates timeout logic.

## Data Location

All data is stored in `C:\ProgramData\FamilyGuard\familyguard.db`. This includes:

- Event log (structured events with timestamps, user, policy, details)
- Settings (presence timeout, covered users, notification preferences)
- Policy rules
- PIN hash
- Schema version
