# Junior Tournament Tracker

Finds USTA **junior** tournaments near you, shows the entry deadline, dates and fee,
nags you before registration closes, and drops any tournament straight into Google Calendar.

Two ways to run it:

| | |
|---|---|
| **`dist/Tournament Tracker.exe`** | The real app. Desktop window + system tray. **Alerts only work here.** |
| **`index.html`** | The same app as a web page — for your phone, or any other computer. |

---

## The desktop app

Double-click **`dist/Tournament Tracker.exe`**. Nothing to install — .NET and the browser
engine are baked in, so it's one 63 MB file you can copy anywhere. A **Tournament Tracker**
shortcut is already in your Start menu.

**Closing the window doesn't quit it.** It parks in the system tray (bottom-right, near the
clock) and keeps watching deadlines. Right-click the tray icon for *Open*,
*Check deadlines now*, or *Quit*.

**Press F11 for fullscreen**, and F11 again to come back. The window also remembers its
size, position and whether it was maximised, so it reopens exactly how you left it. On wide
or fullscreen displays the grid spreads out instead of stranding the extra width in the
margins. F11 works in the browser version too.

First launch asks for two things:

- **Home ZIP code** — the centre of your search radius
- **Birth year** — drives the age division; on Jan 1 it moves you up on its own, so you
  never edit your age

### Why alerts didn't work before

You were opening `index.html` straight from disk. A page loaded from a file isn't a
"secure context", and **every browser silently refuses notification permission there** —
so the button could never have worked, no matter how many times you clicked it. The old
code just reported a vague "blocked" message, which made it look broken rather than
impossible.

The desktop app fixes this properly. It serves the page from a real internal address, and
hands notifications to Windows itself. Verified on your machine:

```
origin: https://tournaments.local   isSecureContext: true   hostBridge: true
```

Turn alerts on with the **◎ Alerts** button. You get a Windows notification 7, 3 and 1 days
before entries close, and on deadline day — clicking one opens that tournament. Saved (★)
tournaments get the full 7-day warning; everything else starts at 3 days so you aren't
buried.

If alerts ever misbehave, run this and it'll tell you what the page environment looks like:

```bash
"C:\Users\User\OneDrive\Documents\Tournament Tracker v.1\dist\Tournament Tracker.exe" --selftest
```

---

## Google Calendar

**Add to calendar** on any card sends it to your real Google Calendar, formatted exactly as:

```
L6 (RWTT L 6 BOYS 12'S, 14'S, 16'S 18'S *top down by ranking)
```

The description carries the full date and time automatically, plus everything else worth
having:

```
Starts: Friday, September 25, 2026 at 12:00 AM
Ends:   Sunday, September 27, 2026 at 11:59 PM

Level: USTA Level 6
Division: 12 & under
Entry fee: $60
Entries close: Saturday, September 19, 2026 at 5:59 PM
Distance: 41.5 mi from home
Tournament ID: 26-62068

https://playtennis.usta.com/...
```

The event spans the real tournament days and carries a reminder the day before.

### One tap, or zero

Out of the box this opens Google Calendar with every field already filled in and you press
**Save**. That needs no setup and works on every device.

To remove that last tap, give it a Google OAuth client ID and it writes to your calendar
directly:

1. Go to **console.cloud.google.com** → create a project (any name)
2. **APIs & Services → Library** → enable **Google Calendar API**
3. **APIs & Services → Credentials** → *Create credentials* → **OAuth client ID** →
   *Web application*
4. Under **Authorised JavaScript origins**, add the address you open the app from
   (for example `https://yourname.github.io`)
5. Copy the client ID into **Settings → Google Calendar** in the app

> This upgrade only works on a real web address. Google won't authorise a page served from
> disk or from the desktop app's internal address, so inside the .exe the one-tap Save flow
> is the one you get.

---

## Filters

- **USTA level** 1–7 (1 hardest, 7 easiest) plus *Other* for Junior Circuit and unsanctioned
- **Division** — 8U / 10U / 12U / 14U / 16U / 18U, or *Auto* from your birth year
- **Gender**, **singles only**, **max drive** (25–70 mi), **how far ahead** to look
- **Open for entry** — on by default, hides anything you can no longer enter

Sort by deadline, tournament date, distance or entry fee. Tap **☆** to save a tournament;
**★ Saved** filters to just those.

---

## Getting it on your phone

Your phone can't reach a file on your PC, so `index.html` needs a web address first.

**GitHub Pages** (free, permanent; `git` is already on this machine) — create an empty
GitHub repo, then:

```bash
cd "C:/Users/User/OneDrive/Documents/Tournament Tracker v.1" && git init && git add index.html README.md && git commit -m "Junior tournament tracker" && git branch -M main
```

Push it, then **Settings → Pages** → source `main` / root.

**Netlify Drop** (no account needed) — drag the folder onto `app.netlify.com/drop`.

Then on your phone open that URL and:
- **iPhone (Safari)** — Share → **Add to Home Screen**
- **Android (Chrome)** — ⋮ → **Add to Home screen**

It opens full-screen like an app. On a real `https://` address the Alerts button works on
the phone too, and that's also where the fully-automatic Google Calendar option becomes
available.

---

## Your settings survive updates

Everything you set up — ZIP code, birth year, gender, division, level filters, sort order,
theme, alert preference and your starred tournaments — is written to:

```
%LOCALAPPDATA%\JuniorTournamentTracker\settings.json
```

That file sits **outside** the app and outside the browser storage the page normally uses,
so you can replace `Tournament Tracker.exe` with a newer build, or clear browser data, and
the app picks straight up where it left off. Verified by wiping all browser storage and
relaunching: the ZIP code and all starred tournaments came back.

The window's size and position live beside it in `window.json`.

If you ever want a genuinely clean start, delete that folder.

---

## Rebuilding the .exe

The web app is embedded in the executable, so after editing `index.html`:

```bash
cd "C:/Users/User/OneDrive/Documents/Tournament Tracker v.1/desktop" && dotnet publish -c Release -o ../dist
```

The page is served from memory inside the exe, so a rebuild always ships the current HTML —
there's no unpacked copy on disk that can go stale.

---

## Where the data comes from

The public USTA / Serve Tennis tournament search
(`prd-usta-kube.clubspark.pro/unified-search-api`) — the same feed behind
[playtennis.usta.com/tournaments](https://playtennis.usta.com/tournaments). It needs no
login, so **there's no USTA account to connect and no password stored anywhere**.
ZIP codes are resolved by [zippopotam.us](https://api.zippopotam.us).

### What it can't show you

**Live entry counts.** How many players are already in your division isn't in the public
feed — it's rendered on the tournament's own Players page, which the app can't read across
domains. Every card has a **Who's in** button that opens that page in one tap.

Drive time is estimated from straight-line distance (~45 mph), so treat it as a rough sort.

Fees and deadlines come straight from the feed, but directors do change them.
**Confirm on the USTA page before you pay.**
