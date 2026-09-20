# Putting the tracker on your phone with GitHub Pages

> ## ✅ This is already done
>
> **Your site is live at <https://tennisagenda.com/>**
>
> Open that on your phone and add it to your home screen (step 4 below).
> The rest of this page is kept as a record of how it was set up, and step 5 is
> how you publish changes from now on.
>
> **Since UTR support (v1.3):** Pages no longer deploys straight from the branch. Step 3
> below is superseded — **Settings → Pages → Source** is now **GitHub Actions**, and
> `.github/workflows/pages.yml` builds the site on every push and every 3 hours (to
> refresh the UTR snapshot). Don't switch it back to "Deploy from a branch", or the UTR
> data file stops being published.

Your phone can't reach a file sitting on your PC, so the app needs a web address.
GitHub Pages gives you one for free, permanently, on `https://` — which matters here,
because **alerts only work on `https://`**.

The local repository is already created and committed. You're starting at step 1 below.

---

## 1. Create the repository

1. Go to **[github.com/new](https://github.com/new)**
2. **Repository name:** `tournament-tracker`
3. **Visibility:** **Public**
   > Pages only works on private repos with a paid plan. Public is fine here — the app
   > holds no passwords or personal data. Your ZIP code and saved tournaments are stored
   > on your own device, never in the code.
4. **Do not** tick "Add a README", ".gitignore", or "Choose a license".
   You already have those files locally, and an empty repo avoids a merge conflict.
5. Click **Create repository**

GitHub then shows you a "…or push an existing repository" box. That's step 2.

---

## 2. Push your code

Open a terminal in the project folder and run these two commands, replacing
`YOUR-USERNAME` with your actual GitHub username:

```bash
cd "C:/Users/User/OneDrive/Documents/Tournament Tracker v.1" && git remote add origin https://github.com/YOUR-USERNAME/tournament-tracker.git
```

```bash
cd "C:/Users/User/OneDrive/Documents/Tournament Tracker v.1" && git push -u origin main
```

**About the sign-in prompt.** GitHub stopped accepting account passwords for pushes years
ago. On this machine Git Credential Manager is installed, so the first push opens a browser
window asking you to authorise Git — click through it and you're done, permanently. If you
instead get a username/password prompt in the terminal, don't type your password: it will
fail. Create a token at **Settings → Developer settings → Personal access tokens → Tokens
(classic)**, tick the **repo** scope, and paste the token in place of the password.

Refresh the GitHub page. Your files should be there.

---

## 3. Turn on Pages

1. In your repository, click **Settings** (the tab along the top, not your account settings)
2. In the left sidebar, click **Pages**
3. Under **Build and deployment → Source**, choose **Deploy from a branch**
4. Under **Branch**, set the dropdown to **`main`** and the folder to **`/ (root)`**
5. Click **Save**

The first build takes about a minute. Reload the Pages settings screen and it'll show:

> Your site is live at `https://YOUR-USERNAME.github.io/tournament-tracker/`

That's your URL. It works because `index.html` sits at the top level of the repo — Pages
serves it automatically as the front page.

---

## 4. Add it to your phone

Open that URL on your phone, then:

- **iPhone (Safari)** — tap **Share** (the square with the arrow) → scroll down →
  **Add to Home Screen** → **Add**
- **Android (Chrome)** — tap **⋮** → **Add to Home screen** → **Install**

It gets your tennis-ball icon and opens full-screen with no browser bars, like a real app.

### What you gain on the phone

- **Alerts work.** It's a real `https://` address, so the browser will actually grant
  notification permission — unlike opening the file from disk.
- **Fully automatic Google Calendar becomes available.** Follow the client-ID steps in the
  README, and add `https://YOUR-USERNAME.github.io` to the
  **Authorised JavaScript origins** list.

Each device keeps its own settings, so set your ZIP and birth year once on the phone too.

---

## 5. Updating it later

Whenever you change `index.html`:

```bash
cd "C:/Users/User/OneDrive/Documents/Tournament Tracker v.1" && git add index.html && git commit -m "Update tracker" && git push
```

Pages rebuilds within a minute. Your phone picks up the new version on next open — you
never reinstall the home-screen icon, and your saved settings are untouched.

---

## Notes

**The .exe isn't in the repo.** It's 63 MB, and Pages doesn't need it — `.gitignore`
excludes `dist/`. If you want it downloadable, attach it to a **Release**: repository →
**Releases** → **Create a new release** → drag `dist/Tournament Tracker.exe` into the
binaries box. GitHub allows up to 2 GB per release file.

**Desktop and phone don't share settings.** They're separate browsers on separate devices,
so each keeps its own ZIP, filters and starred tournaments. There's no account system to
sync them.

**If the page 404s**, give it another minute — the first deploy is the slowest. After that,
check Settings → Pages still shows Source **GitHub Actions**, and that the latest run on
the repo's **Actions** tab succeeded.
