# Tennis Agenda

Finds tennis tournaments near you from **USTA** or **UTR**, shows the entry deadline,
dates and fee, tells you when new ones are posted, nags you before registration closes,
and drops any tournament straight into Google Calendar. **Matchmaking** posts that you are
free to hit, and other players nearby ask you for a time at the court you picked.

Two ways to run it:

| | |
|---|---|
| **the web page** | What everyone else uses: [tennisagenda.com](https://tennisagenda.com/). Works on a phone home screen. |
| **`dist/Tennis Agenda.exe`** | Your own desktop copy. Window + system tray, keeps watching deadlines while closed. |

---

## The desktop app

Double-click **`dist/Tennis Agenda.exe`**. Nothing to install — .NET and the browser
engine are baked in, so it's one 63 MB file you can copy anywhere. A **Tennis Agenda**
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
origin: https://appassets.example   isSecureContext: true   hostBridge: true
```

Turn alerts on with the **◎ Alerts** button. You get a Windows notification 7, 3 and 1 days
before entries close, and on deadline day — clicking one opens that tournament. Saved (★)
tournaments get the full 7-day warning; everything else starts at 3 days so you aren't
buried.

If alerts ever misbehave, run this and it'll tell you what the page environment looks like:

```bash
"C:\Users\User\OneDrive\Documents\Tennis Agenda v.1\dist\Tennis Agenda.exe" --selftest
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
| Rating filter | Level 1–7 (1 hardest) + *Other* | UTR 1–16, then 16.5 |
| Age | Division 8U–18U or **18 & over** | **Juniors only** toggle (off by default) |
| Players already entered | Not published — **Who's in** opens the list | Shown on the card |
| Calendar title | `L6 (Tournament Name)` | `UTR 1–5 (Tournament Name)` |

**18 & over** switches USTA from junior to adult tournaments (USTA's *O18* and *Open*
divisions). Everything else defaults to **18 & under**, singles only, open for entry.

A UTR button means that whole band — **5** is 5.00–5.99, **16** is 16.00–16.49, and
**16.5** is the top of the scale — and an event matches when its
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

## Matchmaking

The **Tournaments | Matchmaking** switch at the top of the dashboard opens it.

1. **Post "Want to play"** — display name, **skill** (Unranked, Beginner, Intermediate,
   Advanced, Tournament, or UTR 1–16.5), singles/doubles, **when** (today, tomorrow, this
   weekend, weekday evenings, flexible, or a specific date and time), an optional note, and
   **the court you'll play at**. You can have **up to five posts live at once** — one per
   time you could play.
2. **Choose the court.** *📍 Choose a court* opens the in-app map with the public courts from
   OpenStreetMap as blue dots — tap one, or tap anywhere to drop your own pin, and give it a
   name. The poster always decides where; the app looks up the address and shows the other
   player the name, how many courts, lights, surface and address.
3. **Someone invites you.** They see your court on your card, can tap **Map** to look at it
   in the app, and tap **Invite to play here** — the only thing they choose is the time
   (pre-filled from your post). A player can send more than one invite for different times.
4. **Accept or Decline.** Confirmed matches show the time, court, **Map**, and Add to calendar.
5. **Cancel** a confirmed match, or **Withdraw** an invite you sent — two taps, so a stray tap
   can't do it. The other player is told straight away.

**Map** anywhere in Matchmaking — on a card, an invite, a match, or your own post — opens the
court on the in-app map, with your area marked for scale and a **Directions** button that
routes you there on OpenStreetMap. There's no Google Maps in Matchmaking any more.

### When the board looks empty

The line under *Players near you* always says why posts aren't showing — for example
*"No one within 25 mi · 2 more beyond your 25-mi max drive (nearest 31 mi)"*, or
*"1 from players you hid"* with an **Unhide players** button — and when it last updated.

A phone that sent the app to the background in the middle of a refresh used to leave that
request hanging forever, and the board then silently never refreshed again. Every relay
request now gives up after 15 seconds, so the next refresh always goes through.

### Notifications

With **Alerts** on you're notified when someone **invites you**, **accepts or declines**,
or **cancels / withdraws** — as a Windows notification in the desktop app, or a system
notification on the web version.

**Phones need one extra step to be told while the app is closed.** A phone pauses web pages
in the background, so no web app can wake itself up to alert you. The free **ntfy** app can:
open *Get invites on your phone even when this app is closed* at the bottom of your
Matchmaking panel, tap *Open your alert channel*, and subscribe in ntfy. Every invite,
answer and cancellation then arrives as a normal phone notification. Keep your channel name
to yourself — anyone who has it can read your alerts.

### What gets shared — and what never does

A post carries your **display name, skill, format, availability, note, chosen court, and
your ZIP code's centre point** (not your location — everyone in 07928 shares the same point).
It never carries your address, age, birth year, phone or email. Phone numbers, emails,
links and social handles typed into a note are replaced with *[removed]* before it's
sent, and again on the receiving side. The time is agreed inside the app, so no one needs
to swap contact details.

Before first use there's a short notice; if your birth year says you're under 18 it
asks you to play only with a parent or guardian's OK and to bring them.

**✕** on a player's card hides them from you; *Unhide players* brings everyone back.

### How it works (and its limits)

There's no server of our own. Posts and invites travel through **ntfy.sh**, a free public
message relay with no account needed. That means:

- **Posts are public by design.** Anyone who knows where to look on the relay can read
  them — which is why they contain nothing sensitive.
- **The relay keeps messages 12 hours.** While your app is running it re-posts every
  6 hours to stay visible; if it's closed for over 12 hours your posts drop off. An invite
  not picked up within 12 hours shows as *no answer* and you can invite again.
- **The app checks for invites every 2 minutes** while it's open — and from the system
  tray in the desktop app.
- **Your desktop and phone are separate players.** Each device has its own posts and inbox;
  an invite to a post you made on your PC arrives on your PC.
- **You only see posts inside your *Max drive* radius**, measured between ZIP areas. Two
  people who both want to see each other each need the other within their own radius.
- **The blue court dots come from OpenStreetMap's free court finder**, which is sometimes
  busy; the picker says so, and you can still drop a pin anywhere. Results are cached for a
  month per area. Map tiles are OpenStreetMap's, which asks for light, personal use.
- **Only people using this app** can see posts. There's no moderation beyond hiding a
  player, so it suits a group you share the link with better than the open internet.

## Filters

- **Rating** — USTA level or UTR band, per the source
- **Division** (USTA) or **Juniors only** (UTR)
- **Gender**, **singles only**, **max drive** (25–70 mi), **how far ahead** to look
- **Open for entry** — on by default, hides anything you can no longer enter

Sort by deadline, tournament date, distance or entry fee. Tap **☆** to save a tournament;
**★ Saved** filters to just those.

---

## Getting it on your phone

It's live at **<https://tennisagenda.com/>**.

The site is deployed by the GitHub Action in `.github/workflows/pages.yml`, which runs on
every push to `main` and every 3 hours (to refresh the UTR snapshot). To publish a change:

```bash
cd "C:/Users/User/OneDrive/Documents/Tennis Agenda v.1" && git add index.html && git commit -m "Update tracker" && git push
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
%LOCALAPPDATA%\TournamentTracker\settings.json
```

That file sits **outside** the app and outside the browser storage the page normally uses,
so you can replace `Tennis Agenda.exe` with a newer build, or clear browser data, and
the app picks straight up where it left off. Verified by wiping all browser storage and
relaunching: the ZIP code and all starred tournaments came back.

The window's size and position live beside it in `window.json`.

If you ever want a genuinely clean start, delete that folder.

---

## Rebuilding the .exe

The web app is embedded in the executable, so after editing `index.html`:

```bash
cd "C:/Users/User/OneDrive/Documents/Tennis Agenda v.1/desktop" && dotnet publish -c Release -o ../dist
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
