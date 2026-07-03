---
name: app-screenshot
description: Regenerate docs/app-screenshot.png — launch the app, populate it with realistic demo data via Avalonia devtools, and capture it including the macOS title bar. Use whenever asked to update/refresh the app screenshot or showcase the GUI.
---

# App screenshot

Regenerates `docs/app-screenshot.png` for the README. Keep the macOS title
bar in the shot — do not crop it out.

## 1. Build and launch (Debug, so devtools attaches)

```sh
cd src/cloud-connector-gui
dotnet build -c Debug
nohup dotnet bin/Debug/net10.0/cloud-connector-gui.dll > /tmp/cc-gui.log 2>&1 &
sleep 4
```

## 2. Attach devtools

```
mcp__avalonia_devtools__attach-to-app   (pick the newest processId from the list)
mcp__avalonia_devtools__tree            (get fresh node IDs — they reset per process)
```

Logical tree node IDs are stable across runs as long as nothing else changed
in `MainWindow.axaml`:

| Field | nodeId | Binding |
|---|---|---|
| Address TextBox | 1065 | `Address` |
| Token TextBox | 1070 | `Token` |
| Proxy TextBox | 1074 | `Proxy` |
| Add endpoint Button | 1091 | — |
| DataGrid | 1088 | `Endpoints` |
| Status TextBlock | 1105 | `StatusText` |
| Logs TextBox | 1113 | `LogText` |

Endpoint grid cell node IDs only exist after clicking "Add endpoint" (once
per row) and must be read from the **Visual** tree (`treeKind: Visual`) under
node 1088 — the Logical tree shows the DataGrid as childless. Each row's
three text cells are the `CellTextBlock`-named nodes in column order
(Local port, Remote host, Remote port).

## 3. Set the demo data (hardcoded — don't re-derive these)

These come from the real `outsystemscc` CLI README usage examples
(`tony4outsystems/cloud-connector`), so they read as authentic:

```
Address: https://acme.outsystems.app/sg_6c23a5b4-b718-4634-a503-f22aed17d4e7
Token:   N2YwMDIxZTEtNGUzNS1jNzgzLTRkYjAtYjE2YzRkZGVmNjcy

Endpoint 1 — Local port: 8081, Remote host: 192.168.0.3,             Remote port: 8393
Endpoint 2 — Local port: 8083, Remote host: db.internal.example.com, Remote port: 5432

Status (nodeId 1105): Running

Logs (nodeId 1113):
2026/07/03 15:49:41 client: Connecting to wss://acme.outsystems.app/sg_6c23a5b4-b718-4634-a503-f22aed17d4e7
2026/07/03 15:49:42 client: Connected (Latency 733.439µs)
2026/07/03 15:49:42 tunnel: R:8081:192.168.0.3:8393 established, available at secure-gateway:8081
2026/07/03 15:49:42 tunnel: R:8083:db.internal.example.com:5432 established, available at secure-gateway:8083
```

Apply each with `mcp__avalonia_devtools__set-prop` (`propertyName: "Text"`).
Click "Add endpoint" (nodeId 1091) twice before setting the grid cells, once
per row.

## 4. Screenshot with the OS title bar (do NOT use the devtools screenshot tool for this — it only captures the content, no chrome)

```sh
osascript -e 'tell application "System Events" to tell (first process whose unix id is <PID>) to set frontmost to true'
sleep 1
osascript -e 'tell application "System Events" to get {position, size} of front window of (first process whose unix id is <PID>)'
# use the returned position/size verbatim:
screencapture -R<x>,<y>,<w>,<h> -x docs/app-screenshot.png
```

Verify the frontmost process is actually the app (not the editor/terminal)
before capturing — `osascript -e 'tell application "System Events" to name of first application process whose frontmost is true'` should print `dotnet`.

## 5. Clean up

```sh
kill <PID>
```
