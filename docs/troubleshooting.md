# Troubleshooting

## Service Won't Start

**Check service status:**
```powershell
sc query FamilyGuard
```

**Check Windows Event Log:**
```powershell
Get-EventLog -LogName Application -Source FamilyGuard -Newest 10
```

**Common causes:**
- .NET 10 Runtime not installed
- Service binary missing from `C:\Program Files\FamilyGuard\`
- Port conflict or permissions issue on named pipes

**Fix:** Reinstall via MSI or manually re-register:
```powershell
sc delete FamilyGuard
sc create FamilyGuard binPath= "C:\Program Files\FamilyGuard\FamilyGuard.Service.exe" start= auto
sc start FamilyGuard
```

## Agent Not Launching

The service automatically launches one agent per interactive user session. If the agent isn't running:

1. Verify the service is running: `sc query FamilyGuard`
2. Check if the user session is active: `query session`
3. Look for agent crash events in the SQLite database:
   ```powershell
   sqlite3 "C:\ProgramData\FamilyGuard\familyguard.db" "SELECT * FROM events WHERE event_type='AgentStopped' ORDER BY timestamp_utc DESC LIMIT 5;"
   ```

## Tray Icon Not Showing

- Open **DAD - Digital Activity Defender** from the Start Menu; the MSI installs this shortcut for launching the interactive tray app on demand
- The tray icon appears only for interactive sessions with a desktop
- RDP disconnected sessions don't show a tray icon
- Try right-clicking the notification area and selecting "Show hidden icons"
- Confirm `FamilyGuard.UI.exe` exists in `C:\Program Files\FamilyGuard\`; if only `FamilyGuard.Service.exe` and `FamilyGuard.Agent.exe` are present, reinstall from the latest MSI

## Microphone Not Being Muted

1. **Check presence timeout:** Settings > Presence Timeout. Default is 90 seconds.
2. **Check covered users:** If users are specified, only those sessions are monitored.
3. **Controller input:** If a controller is active, the session is considered "present" even without keyboard/mouse input. This is by design for gaming.
4. **Check mic device:** DAD controls the default communications microphone. Some apps use custom audio devices.
5. **View event log:** Right-click tray icon > Event Log. Filter by "Mic Auto-Muted" to see if actions are firing.

## PIN Lockout

After 5 failed PIN attempts, settings are locked for 15 minutes. Wait for the lockout to expire, then try again.

If you've forgotten the PIN entirely, an administrator can reset it by deleting the PIN hash from the database:
```powershell
sqlite3 "C:\ProgramData\FamilyGuard\familyguard.db" "DELETE FROM settings WHERE key='pin_hash';"
```
The next settings access will prompt for a new PIN.

## Database Location

All data is in `C:\ProgramData\FamilyGuard\familyguard.db`. You can query it with any SQLite tool:

```powershell
# Recent events
sqlite3 "C:\ProgramData\FamilyGuard\familyguard.db" "SELECT * FROM events ORDER BY timestamp_utc DESC LIMIT 20;"

# Current settings
sqlite3 "C:\ProgramData\FamilyGuard\familyguard.db" "SELECT * FROM settings;"

# Policy rules
sqlite3 "C:\ProgramData\FamilyGuard\familyguard.db" "SELECT * FROM policy_rules;"

# Schema version
sqlite3 "C:\ProgramData\FamilyGuard\familyguard.db" "SELECT * FROM schema_version;"
```

## Logs

FamilyGuard writes structured events to SQLite (not text log files). To export:

```powershell
sqlite3 "C:\ProgramData\FamilyGuard\familyguard.db" ".mode json" "SELECT * FROM events ORDER BY timestamp_utc DESC;" > events.json
```
