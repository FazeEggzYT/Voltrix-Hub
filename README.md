# VOLTRIX Hub

A lightweight, custom Windows app for Logitech keyboards and mice. A clean alternative to Logitech G HUB, built from scratch.

Made for the Logitech PRO keyboard (UK layout) and the G502 HERO mouse. Status: **pre-release** (v0.9.0). Hardware features are being tested on real devices.

<!-- Add screenshots here once taken: screenshot-devices.png, screenshot-lighting.png, screenshot-profiles.png -->
<img width="550" height="335" alt="image" src="https://github.com/user-attachments/assets/1a5736ed-c716-4920-9765-f1c106a4c092" />
<img width="567" height="974" alt="image" src="https://github.com/user-attachments/assets/36ac76b0-e198-4cc6-a390-49dcd67ab0f1" />
<img width="593" height="977" alt="image" src="https://github.com/user-attachments/assets/fea5aeed-9dea-4225-9688-a0572426b410" />
<img width="816" height="952" alt="image" src="https://github.com/user-attachments/assets/07902259-376f-4435-bc46-180231760f70" />
<img width="612" height="978" alt="image" src="https://github.com/user-attachments/assets/b15d5e8b-e83e-4882-a28f-38defa9718a7" />
<img width="65" height="32" alt="image" src="https://github.com/user-attachments/assets/e1d0646e-5bca-4521-bb04-3850d583aa68" />
<img width="54" height="37" alt="image" src="https://github.com/user-attachments/assets/bf833fbf-b3b1-43f8-9400-a4d5629b743c" />
<img width="813" height="984" alt="image" src="https://github.com/user-attachments/assets/f79a8955-bb9c-4097-8392-f737f7b8f754" />
<img width="652" height="968" alt="image" src="https://github.com/user-attachments/assets/6b391c8e-3eab-4c56-a7bc-78b6828efae2" />







## Features
- Device scanner (read-only) with a copyable report
- Keyboard lighting (one colour, saved between runs)
- Key assignments: remap keys, media and system actions, open programs, type text, disable keys
- Mouse buttons (middle and side) use the same assignments
- Game Mode: block the Windows keys, Alt+Tab, Alt+F4 and Menu, by hand or automatically when a game runs
- Per-game colour or profile
- G502 HERO: DPI slider, quick DPI, report rate
- Profiles: save, apply, export and import
- Overlay: a HUD with a 3x3 placement grid (FPS, CPU, RAM, ping, clock and more), templates, and a customisable crosshair
- Themes, accent colours and a start-up screen
- Auto-updater using GitHub releases, with the changelog inside the app
- Tray icon with an emergency "Pause key assignments"

## Install
1. Download `VOLTRIX-Hub.exe` from the latest release.
2. Close Logitech G HUB first (tray icon > Quit).
3. Run the EXE. Windows may show SmartScreen because the app is not signed: click **More info**, then **Run anyway**.
4. Optional: open **Settings** and press **Install VOLTRIX Hub** to add a Start Menu shortcut and an Installed apps entry. No admin needed.

To remove it: Settings > Uninstall, or Windows Settings > Installed apps.

## Good to know
- Key assignments and Game Mode only work while VOLTRIX Hub is running (use Settings > Start with Windows).
- Lighting, DPI and report rate commands are experimental. If a device refuses one, the app shows the message and the raw reply. Press **Copy report** on the Devices page and share it in an issue.
- Close G HUB before using device features. Two programs talking to one device can clash.
- Emergency switch: tray icon > Pause key assignments.

## Build it yourself
Run `build.bat` on Windows. No extra installs needed (it uses the C# compiler that ships with Windows). It makes `VOLTRIX-Hub.exe`.

## FPS counter
Windows does not let one program read another program's FPS. VOLTRIX Hub uses PresentMon (free, open source) for it. Overlay > FPS explains the set up. It needs administrator rights.

## Troubleshooting
- "Logitech software is running": close G HUB and end `lghub_agent` in Task Manager > Details.
- A key stays remapped after closing: it should not. Reopen the app and press Pause, or restart Windows.
- Scan finds nothing: check the cable, try another USB port, then scan again.

Not affiliated with Logitech. Logitech and G HUB are trademarks of Logitech.
