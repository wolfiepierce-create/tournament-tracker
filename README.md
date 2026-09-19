# Junior Tournament Tracker

Finds **junior** tennis tournaments near you from **USTA** or **UTR**, shows the entry
deadline, dates and fee, tells you when new ones are posted, nags you before registration
closes, and drops any tournament straight into Google Calendar.

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

## USTA or UTR

The small **USTA ▾** pill beside the title switches the whole app between the two.
Each keeps its own filters, so flipping back and forth never loses your setup.

| | USTA | UTR |
|---|---|---|
| Rating filter | Level 1–7 (1 hardest) + *Other* | UTR 1–12 |
| Age | Division 8U–18U, auto from birth year | **Juniors only** toggle |
| Players already entered | Not published — **Who's in** opens the list | Shown on the card |
| Calendar title | `L6 (Tournament Name)` | `UTR 1–5 (Tournament Name)` |

A UTR button means that whole band — **5** is 5.00–5.99 — and an event matches when its
allowed range overlaps it. Many UTR events are open to a very wide range like 1–16, which
is why those appear under almost every band: they genuinely accept that player.

**How "Juniors only" works, honestly.** UTR's public search has no age field. The app
recognises junior events from what the director wrote in the event and division names —
*Junior*, *12U*, *Boys 14s*, *red/orange/green ball*, *high school* and similar. Events
open to "All Ages" count too, since juniors can enter them. A junior event with a name
that gives no hint will be missed; turn the toggle off to see everything.

## New tournament alerts

With **Alerts** on, you're notified when a tournament is **added** to whichever source is
selected — but only if it matches your filters. Up to three arrive individually; more than
that becomes one summary. New ones also get a **New** tag for three days.

The app checks every hour while it's open (the desktop app keeps checking from the tray).
It remembers every tournament it has already seen, so the first run, widening your radius,
or the calendar rolling forward never set off false "new" alerts — only tournaments that
actually appear on the site do.

## Filters

- **Rating** — USTA level or UTR band, per the source
- **Division** (USTA) or **Juniors only** (UTR)
- **Gender**, **singles only**, **max drive** (25–70 mi), **how far ahead** to look
- **Open for entry** — on by default, hides anything you can no longer enter

Sort by deadline, tournament date, distance or entry fee. Tap **☆** to save a tournament;
**★ Saved** filters to just those.

---

## Getting it on your phone

It's live at **<https://wolfiepierce-create.github.io/tournament-tracker/>**.

The site is deployed by the GitHub Action in `.github/workflows/pages.yml`, which runs on
every push to `main` and every 3 hours (to refresh the UTR snapshot). To publish a change:

```bash
cd "C:/Users/User/OneDrive/Documents/Tournament Tracker v.1" && git add index.html && git commit -m "Update tracker" && git push
```

On your phone open that URL and:
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

**UTR** comes from the public event search behind
[app.utrsports.net](https://app.utrsports.net/search?type=events) — tennis only, no login.
UTR only allows its own website to call that search from a browser, so:

- **The desktop app** asks UTR directly through its built-in host (it isn't a browser), so
  its UTR data is live.
- **The phone/web version** reads a snapshot of every upcoming US event, rebuilt every
  3 hours by the GitHub Action in `.github/workflows/pages.yml` and published with the site.
  It covers the whole country on purpose: your location is never in the public file —
  distance is worked out on your device. The status line shows how old the snapshot is.

If UTR is ever unreachable when the Action runs, it re-publishes the last good snapshot
rather than blanking the data.

> GitHub pauses scheduled Actions on a repository with no activity for 60 days, and emails
> you when it does. If UTR data on the phone stops updating, re-enable it under the repo's
> **Actions** tab, or push any change.

### What it can't show you

**Live entry counts for USTA.** How many players are already in your division isn't in
USTA's public feed — it's rendered on the tournament's own Players page, which the app
can't read across domains. USTA cards have a **Who's in** button that opens that page in
one tap. (UTR does publish the count, so UTR cards show it directly.)

Drive time is estimated from straight-line distance (~45 mph), so treat it as a rough sort.

Fees and deadlines come straight from the feed, but directors do change them.
**Confirm on the USTA page before you pay.**
