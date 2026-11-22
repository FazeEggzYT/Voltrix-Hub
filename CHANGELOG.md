# Changelog

## v0.8.0 pre-release: Sharing
- Install VOLTRIX Hub from Settings: copies the app to your user folder, adds a Start Menu shortcut (and an optional desktop shortcut) and shows it in Windows Installed apps. No admin needed.
- Uninstall from Settings or from Windows Installed apps. You choose whether to keep your settings and profiles.
- Updates keep working for the installed copy.
- New README with a feature list, install steps and troubleshooting.
- Profile import and export (added in v0.7.0) is the way to share profiles.

## v0.7.0 pre-release: Profiles + Updates
- Profiles page: save your current lighting, key assignments, Game Mode keys and mouse DPI as a named profile. Apply, delete, export and import profiles (.vprofile files).
- Your active profile is applied again each time the app starts.
- Games can switch to a profile when they run, and your own settings come back when the game closes.
- Updates page: check GitHub for a newer version, read what is new, download and install it in one click. The app restarts into the new version.
- Changelog shown inside the app.
- Settings page: start with Windows, start hidden in the tray, check for updates on start, include pre-releases.
- Sidebar shows when an update is available.

## v0.6.0 pre-release: Mouse
- G502 HERO is found by the scanner and its card on the Devices page opens.
- Performance tab: DPI slider (100 to 25600), five quick DPI buttons, report rate (125, 250, 500, 1000 Hz), Apply to mouse.
- Buttons tab: change what the middle button and the two side buttons do (same actions as keys).
- Mouse buttons can also be chosen on the keyboard Assignments tab.
- EXPERIMENTAL: DPI and report rate commands use standard Logitech features. The message and raw reply on screen show if the mouse refuses.

## v0.5.0 pre-release: Game
- Game Mode tab with switches: block the Windows keys, Alt+Tab, Alt+F4 and the Menu key.
- Game Mode on now switch, or let it turn on by itself when a game from your list is running.
- Your games list: add a program (pick from running programs) and a keyboard colour for it.
- When a game starts the keyboard colour switches to the game's colour. When it closes, your normal colour comes back.
- Settings are saved between runs.

## v0.4.0 pre-release: Keys
- Assignments tab: change what any key does.
- Choose: press another key, a media or system action (play/pause, next, previous, mute, volume, calculator, lock PC), open a program or file, type some text, or disable the key.
- Add and remove assignments. They are saved.
- Works only while VOLTRIX Hub is running.
- Emergency switch: tray icon > Pause key assignments (also a switch on the tab).

## v0.3.0 pre-release: Lighting
- Lighting tab works: pick one of 10 colours or a custom colour, then press Apply to keyboard.
- Your last colour is saved and remembered next time (in %APPDATA%\VOLTRIX Hub).
- Restore button explains how to get Logitech lighting back. VOLTRIX never changes the keyboard's saved settings.
- EXPERIMENTAL: the lighting command is based on the open-source keyboard tools. If the keyboard rejects it, the message and raw reply on screen tell us what to fix.

## v0.2.0 pre-release: Detect
- Scan for devices on the Devices page (also runs on start). Read-only: it asks questions and changes nothing.
- Shows the keyboard name, HID++ version, firmware and its feature list in a report.
- Copy report button to send the result to Claude.
- Warns if G HUB or other Logitech software is running.

## v0.1.0 pre-release: First light
- App window with the VOLTRIX look and a sidebar (Devices, Profiles, Updates, Settings).
- Devices page with a PRO keyboard card and a greyed G502 HERO card.
- Device page shell with Lighting, Assignments and Game Mode tabs.
- Tray icon with Open and Exit. Minimise sends the app to the tray.
- Only one copy of the app can run at a time.
