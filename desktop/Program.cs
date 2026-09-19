using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace TournamentTracker;

/// <summary>
/// Desktop shell for the Junior Tournament Tracker.
///
/// Why this exists rather than just opening index.html in a browser:
///
///  1. Notifications. A page opened from disk (file://) is not a secure
///     context, so browsers silently refuse notification permission. Here the
///     page is served from a WebView2 virtual host, which IS a secure context,
///     and notifications are handed to Windows as real tray toasts.
///
///  2. Closing the window parks the app in the system tray instead of quitting,
///     so the deadline checks keep running and can still reach the user.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // "Tournament Tracker.exe --selftest" boots the page, reports whether the
        // browser environment can actually raise notifications, and quits. Useful
        // when alerts are misbehaving and you need to know why.
        if (args.Any(a => string.Equals(a, "--selftest", StringComparison.OrdinalIgnoreCase)))
        {
            Application.Run(new MainForm(selfTest: true));
            return;
        }

        // One instance only - a second launch just re-opens the existing window.
        using var mutex = new Mutex(true, @"Local\JuniorTournamentTracker", out bool isFirst);
        if (!isFirst)
        {
            NativeSingleInstance.PokeExistingInstance();
            return;
        }

        Application.Run(new MainForm());
    }
}

internal static class NativeSingleInstance
{
    public const int WM_SHOWME = 0x0400 + 42;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? cls, string? title);

    public static void PokeExistingInstance()
    {
        IntPtr h = FindWindow(null, MainForm.WindowTitle);
        if (h != IntPtr.Zero) PostMessage(h, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
    }
}

internal sealed class MainForm : Form
{
    public const string WindowTitle = "Junior Tournament Tracker";

    // WebView2 maps this name to a folder we control. It must NOT be a ".local"
    // name - that suffix is reserved for mDNS, so the runtime tries to resolve it
    // on the network and the navigation fails. ".example" is reserved by IANA and
    // never resolves anywhere, which is exactly what we want.
    private const string VirtualHost = "appassets.example";

    private readonly WebView2 _web = new();
    private readonly NotifyIcon _tray = new();
    private readonly System.Windows.Forms.Timer _recheck = new();
    private readonly string _html;

    private bool _reallyExit;
    private string? _lastNotificationUrl;
    private readonly bool _selfTest;
    private bool _selfTestDone;
    private bool _navOk;
    private string _navError = "";

    public MainForm(bool selfTest = false)
    {
        _selfTest = selfTest;
        Text = WindowTitle;
        MinimumSize = new Size(900, 620);
        Size = new Size(1280, 860);
        StartPosition = FormStartPosition.CenterScreen;
        // Matches the app's dark canvas so there is no white flash while WebView2 boots.
        BackColor = Color.FromArgb(0, 0, 0);

        try { Icon = LoadAppIcon(); } catch { /* falls back to the default icon */ }

        _html = LoadWebApp();

        _web.Dock = DockStyle.Fill;
        _web.DefaultBackgroundColor = Color.Black;
        Controls.Add(_web);

        SetUpTray();

        // The page runs its own 30-minute check. This nudge covers the case where
        // the machine was asleep and timers drifted.
        _recheck.Interval = 15 * 60 * 1000;
        _recheck.Tick += (_, _) => PostToPage("{\"type\":\"recheck\"}");
        _recheck.Start();

        if (!selfTest) RestoreGeometry();

        Load += async (_, _) => await InitialiseWebViewAsync();
        FormClosing += OnFormClosing;
    }

    // ---------------------------------------------------------------- content

    /// <summary>
    /// The web app is read straight out of the exe and served from memory.
    ///
    /// It used to be unpacked to a folder and handed to WebView2 via
    /// SetVirtualHostNameToFolderMapping, but WebView2's renderer is sandboxed:
    /// unless that folder carries an "ALL APPLICATION PACKAGES" ACE it is denied,
    /// and every navigation fails with ERR_ACCESS_DENIED. Serving the bytes
    /// ourselves sidesteps file permissions completely - and guarantees the page
    /// always matches this build instead of a stale copy left on disk.
    /// </summary>
    private static string LoadWebApp()
    {
        using Stream? s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.index.html");
        if (s is null) return "<h1>Web app missing from this build.</h1>";
        using var reader = new StreamReader(s, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    // ---------------------------------------------------------------- UTR

    private static readonly HttpClient Http = CreateHttp();

    private static HttpClient CreateHttp()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("JuniorTournamentTracker/1.3 (personal use)");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return c;
    }

    private static async Task<CoreWebView2WebResourceResponse> ProxyUtrAsync(
        CoreWebView2Environment env, string query)
    {
        // Only the event search is reachable, and only for tennis - the page cannot
        // use this to reach anything else on UTR, let alone the wider internet.
        string q = query.TrimStart('?');
        q = System.Text.RegularExpressions.Regex.Replace(q, @"(^|&)show(Tennis|Pickleball)Content=[^&]*", "");
        q = "showTennisContent=true&showPickleballContent=false&" + q.TrimStart('&');

        int status = 502;
        string reason = "Bad Gateway";
        string json;
        try
        {
            using HttpResponseMessage r = await Http.GetAsync("https://api.utrsports.net/v2/search/events?" + q);
            status = (int)r.StatusCode;
            reason = r.ReasonPhrase ?? "";
            json = await r.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            json = JsonSerializer.Serialize(new { error = "UTR unreachable", message = ex.Message });
        }

        return env.CreateWebResourceResponse(
            new MemoryStream(Encoding.UTF8.GetBytes(json)), status, reason,
            "Content-Type: application/json; charset=utf-8\r\nCache-Control: no-store");
    }

    private static Icon LoadAppIcon()
    {
        string? path = Environment.ProcessPath;
        if (path is not null)
        {
            Icon? extracted = Icon.ExtractAssociatedIcon(path);
            if (extracted is not null) return extracted;
        }
        return SystemIcons.Application;
    }

    // ---------------------------------------------------------------- webview

    private async Task InitialiseWebViewAsync()
    {
        string profile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JuniorTournamentTracker", "profile");
        Directory.CreateDirectory(profile);

        CoreWebView2Environment env;
        try
        {
            env = await CoreWebView2Environment.CreateAsync(null, profile);
            await _web.EnsureCoreWebView2Async(env);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "This app needs the Microsoft Edge WebView2 runtime, which it could not start.\n\n" +
                "Install the free 'Evergreen WebView2 Runtime' from Microsoft, then run this again.\n\n" +
                ex.Message,
                WindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            _reallyExit = true;
            Close();
            return;
        }

        CoreWebView2 core = _web.CoreWebView2;

        // Answer every request to our own host from memory. The https scheme is
        // what makes this a secure context, which is what lets notifications work.
        core.AddWebResourceRequestedFilter($"https://{VirtualHost}/*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += async (_, e) =>
        {
            var uri = new Uri(e.Request.Uri);

            // /utr/search?... is forwarded to UTR's event search. UTR only lets its
            // own website call that API from a browser, but this host is not a
            // browser, so the desktop app gets live UTR data with no middleman.
            if (uri.AbsolutePath.StartsWith("/utr/search", StringComparison.OrdinalIgnoreCase))
            {
                CoreWebView2Deferral deferral = e.GetDeferral();
                try
                {
                    e.Response = await ProxyUtrAsync(env, uri.Query);
                }
                finally
                {
                    deferral.Complete();
                }
                return;
            }

            var body = new MemoryStream(Encoding.UTF8.GetBytes(_html));
            e.Response = env.CreateWebResourceResponse(
                body, 200, "OK",
                "Content-Type: text/html; charset=utf-8\r\nCache-Control: no-cache");
        };

        CoreWebView2Settings st = core.Settings;
        st.AreDefaultContextMenusEnabled = false;
        st.IsStatusBarEnabled = false;
        st.AreBrowserAcceleratorKeysEnabled = false;
        st.IsSwipeNavigationEnabled = false;

        // The page asks the shell to raise notifications; nothing else is accepted.
        core.WebMessageReceived += OnWebMessage;

        // Registering, the USTA site and Google Calendar all open in the real browser.
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            OpenInBrowser(e.Uri);
        };

        // Keep the app itself on its own origin; outside links go to the browser.
        core.NavigationStarting += (_, e) =>
        {
            if (e.Uri.StartsWith($"https://{VirtualHost}/", StringComparison.OrdinalIgnoreCase)) return;
            if (e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return;
            e.Cancel = true;
            OpenInBrowser(e.Uri);
        };

        // Hand the saved setup to the page before any of its own code runs, so it
        // can fall back to this when localStorage is empty (fresh install, new exe,
        // cleared browser data).
        await core.AddScriptToExecuteOnDocumentCreatedAsync(
            $"window.__restored = {ReadRestoreBlob()};");

        if (_selfTest)
        {
            // A cold WebView2 profile fires NavigationCompleted for about:blank
            // before our own page loads, so wait for the real one.
            core.NavigationCompleted += async (_, ev) =>
            {
                if (_selfTestDone) return;
                if (!core.Source.Contains(VirtualHost, StringComparison.OrdinalIgnoreCase)) return;
                _selfTestDone = true;
                _navOk = ev.IsSuccess;
                _navError = ev.WebErrorStatus.ToString();
                await RunSelfTestAsync(core);
            };
        }

        core.Navigate($"https://{VirtualHost}/index.html");
    }

    /// <summary>
    /// Reports the three things that decide whether alerts can work at all,
    /// then pushes one real notification through the full path.
    /// </summary>
    private async Task RunSelfTestAsync(CoreWebView2 core)
    {
        // NavigationCompleted can arrive for an earlier navigation in the same
        // WebView, so do not trust it alone - wait until the document is really ours.
        for (int i = 0; i < 40; i++)
        {
            string ready = await core.ExecuteScriptAsync(
                "(document.readyState==='complete' && typeof S!=='undefined')?'1':'0'");
            if (ready.Contains('1')) break;
            await Task.Delay(250);
        }

        // Rewriting the current settings is a no-op when nothing has changed,
        // so this is safe to run at any time - but it does prove the save path.
        await core.ExecuteScriptAsync("try{ saveS(); saveW(); }catch(e){}");
        await Task.Delay(300);

        string probe = await core.ExecuteScriptAsync(
            "JSON.stringify({origin:location.origin," +
            "isSecureContext:window.isSecureContext," +
            "hasHostBridge:!!(window.chrome&&window.chrome.webview)," +
            "notificationApi:('Notification' in window),docTitle:document.title," +
            "pageText:(document.body?document.body.innerText.split('\\n').filter(Boolean).join(' | ').slice(0,300):'')," +
            "restoredSeen:!!window.__restored," +
            "restoredZip:(window.__restored&&window.__restored['jtt.settings']&&window.__restored['jtt.settings'].zip)||null," +
            "liveZip:(typeof S!=='undefined'?S.zip:null)," +
            "savedCount:(typeof watch!=='undefined'?watch.size:null)})");

        // ExecuteScriptAsync hands back a JSON-encoded string; unwrap one level.
        string inner = JsonSerializer.Deserialize<string>(probe) ?? probe;

        // Exercise the live UTR route through the same fetch the app uses.
        await core.ExecuteScriptAsync(
            "window.__utrProbe=null;fetch('/utr/search?top=1&distance=45mi&pin=40.74,-74.38')" +
            ".then(r=>r.json()).then(j=>window.__utrProbe='total='+j.total+' hits='+(j.hits||[]).length)" +
            ".catch(e=>window.__utrProbe='ERR '+e.message)");
        string utrProbe = "timeout";
        for (int i = 0; i < 60; i++)
        {
            string v = await core.ExecuteScriptAsync("window.__utrProbe");
            if (v != "null") { utrProbe = JsonSerializer.Deserialize<string>(v) ?? v; break; }
            await Task.Delay(250);
        }

        // Run the app's own UTR pipeline end to end - fetch, normalise, junior
        // detection - against a fixed location, restoring the page's settings after.
        await core.ExecuteScriptAsync(
            "window.__utrFlow=null;(async()=>{const o={lat:S.lat,lon:S.lon,r:S.radius};" +
            "try{S.lat=40.7409;S.lon=-74.3834;S.radius=45;const {all,near}=await fetchUtr();" +
            "const j=near.filter(t=>t.junior),s=j[0]||near[0];" +
            "window.__utrFlow='near='+near.length+' junior='+j.length+(s?' | e.g. '+s.name.slice(0,34)+' / UTR '+utrRangeText(s)+' / '+s.miles+'mi / reg '+s.registered:'');}" +
            "catch(e){window.__utrFlow='ERR '+e.message}finally{S.lat=o.lat;S.lon=o.lon;S.radius=o.r;}})()");
        string utrFlow = "timeout";
        for (int i = 0; i < 120; i++)
        {
            string v = await core.ExecuteScriptAsync("window.__utrFlow");
            if (v != "null") { utrFlow = JsonSerializer.Deserialize<string>(v) ?? v; break; }
            await Task.Delay(250);
        }

        // Drive fullscreen exactly the way the user does: let the page see an F11
        // keypress and watch it come back through the bridge.
        await core.ExecuteScriptAsync(
            "document.dispatchEvent(new KeyboardEvent('keydown',{key:'F11',bubbles:true}))");
        await Task.Delay(500);
        bool fsOn = _fullscreen && FormBorderStyle == FormBorderStyle.None
                                && WindowState == FormWindowState.Maximized;

        await core.ExecuteScriptAsync(
            "document.dispatchEvent(new KeyboardEvent('keydown',{key:'F11',bubbles:true}))");
        await Task.Delay(500);
        bool fsOff = !_fullscreen && FormBorderStyle != FormBorderStyle.None;

        string report = inner.TrimEnd('}')
            + $",\"utrLive\":\"{utrProbe}\""
            + $",\"utrFlow\":{JsonSerializer.Serialize(utrFlow)}"
            + $",\"fullscreenOn\":{(fsOn ? "true" : "false")}"
            + $",\"fullscreenRestored\":{(fsOff ? "true" : "false")}"
            + $",\"navOk\":{(_navOk ? "true" : "false")}"
            + $",\"navError\":\"{_navError}\""
            + $",\"source\":\"{core.Source}\""
            + $",\"settingsFile\":\"{(File.Exists(RestorePath) ? "present" : "absent")}\""
            + "}";

        string outPath = Path.Combine(Path.GetTempPath(), "tracker-selftest.json");
        File.WriteAllText(outPath, report, new UTF8Encoding(false));

        // Exercise the real bridge: page -> shell -> Windows toast.
        await core.ExecuteScriptAsync(
            "window.chrome.webview.postMessage(JSON.stringify(" +
            "{type:'notify',title:'Self-test',body:'Notification path is working.'}))");

        await Task.Delay(1500);
        _reallyExit = true;
        Close();
    }

    private static void OpenInBrowser(string uri)
    {
        if (!uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri)
            {
                UseShellExecute = true
            });
        }
        catch { /* no default browser configured */ }
    }

    private void PostToPage(string json)
    {
        if (_web.CoreWebView2 is null) return;
        try { _web.CoreWebView2.PostWebMessageAsString(json); } catch { }
    }

    // ---------------------------------------------------------------- messages

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string raw;
        try { raw = e.TryGetWebMessageAsString(); }
        catch { return; }

        string type;
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(raw);
            type = doc.RootElement.TryGetProperty("type", out JsonElement t) ? t.GetString() ?? "" : "";
        }
        catch { return; }

        using (doc)
        {
            switch (type)
            {
                case "notify":
                    ShowToast(doc.RootElement);
                    break;

                // The page mirrors its settings out so they outlive this exe.
                case "persist":
                    if (doc.RootElement.TryGetProperty("data", out JsonElement data))
                        SaveRestoreBlob(data.GetRawText());
                    break;

                case "fullscreen":
                    ToggleFullscreen();
                    break;
            }
        }
    }

    private void ShowToast(JsonElement root)
    {
        string? Get(string name) =>
            root.TryGetProperty(name, out JsonElement v) ? v.GetString() : null;

        _lastNotificationUrl = Get("url");

        // Balloon text is single-line in practice; flatten so nothing is lost.
        string body = (Get("body") ?? string.Empty).Replace("\n", "  Â·  ");

        _tray.BalloonTipTitle = Trim(Get("title") ?? WindowTitle, 63);
        _tray.BalloonTipText = Trim(body, 255);
        _tray.BalloonTipIcon = ToolTipIcon.Info;
        _tray.ShowBalloonTip(10_000);
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "â€¦";

    // ---------------------------------------------------------------- settings

    /// <summary>
    /// Where the user's setup lives. Deliberately outside both the exe and the
    /// WebView2 profile, so replacing the app - or clearing browser data - keeps
    /// the ZIP code, birth year, filters and saved tournaments intact.
    /// </summary>
    private static string RestorePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JuniorTournamentTracker", "settings.json");

    private void SaveRestoreBlob(string json)
    {
        try
        {
            string path = RestorePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Write beside the target first, then swap it in, so a crash midway
            // can never leave a half-written settings file behind.
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));
            File.Move(tmp, path, overwrite: true);
        }
        catch { /* a failed mirror must never interrupt the app */ }
    }

    private static string ReadRestoreBlob()
    {
        try
        {
            if (File.Exists(RestorePath))
            {
                string json = File.ReadAllText(RestorePath, Encoding.UTF8).Trim();
                // Only hand back something that actually parses as an object.
                if (json.StartsWith('{'))
                {
                    using JsonDocument _ = JsonDocument.Parse(json);
                    return json;
                }
            }
        }
        catch { }
        return "null";
    }

    // ---------------------------------------------------------------- fullscreen

    private bool _fullscreen;
    private FormWindowState _preFullscreenState = FormWindowState.Normal;
    private FormBorderStyle _preFullscreenBorder = FormBorderStyle.Sizable;

    private void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            _preFullscreenState = WindowState;
            _preFullscreenBorder = FormBorderStyle;

            FormBorderStyle = FormBorderStyle.None;
            // Drop out of Maximized first, or the border removal is not re-applied
            // and the taskbar stays on top of the window.
            WindowState = FormWindowState.Normal;
            WindowState = FormWindowState.Maximized;
            _fullscreen = true;
        }
        else
        {
            FormBorderStyle = _preFullscreenBorder;
            WindowState = _preFullscreenState;
            _fullscreen = false;
        }
    }

    // ---------------------------------------------------------------- geometry

    private static string GeometryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JuniorTournamentTracker", "window.json");

    private sealed class Geometry
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public bool Maximized { get; set; }
    }

    private void SaveGeometry()
    {
        try
        {
            // Never record the fullscreen or minimised shape as the normal one.
            if (_fullscreen || WindowState == FormWindowState.Minimized) return;

            Rectangle b = WindowState == FormWindowState.Maximized ? RestoreBounds : Bounds;
            var g = new Geometry
            {
                X = b.X, Y = b.Y, W = b.Width, H = b.Height,
                Maximized = WindowState == FormWindowState.Maximized,
            };
            Directory.CreateDirectory(Path.GetDirectoryName(GeometryPath)!);
            File.WriteAllText(GeometryPath, JsonSerializer.Serialize(g), new UTF8Encoding(false));
        }
        catch { }
    }

    private void RestoreGeometry()
    {
        try
        {
            if (!File.Exists(GeometryPath)) return;
            Geometry? g = JsonSerializer.Deserialize<Geometry>(File.ReadAllText(GeometryPath, Encoding.UTF8));
            if (g is null || g.W < MinimumSize.Width || g.H < MinimumSize.Height) return;

            // A monitor may have been unplugged since last time; only accept a
            // position that still lands on a screen that exists.
            var want = new Rectangle(g.X, g.Y, g.W, g.H);
            if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(want))) return;

            StartPosition = FormStartPosition.Manual;
            Bounds = want;
            if (g.Maximized) WindowState = FormWindowState.Maximized;
        }
        catch { }
    }

    // ---------------------------------------------------------------- tray

    private void SetUpTray()
    {
        _tray.Icon = Icon ?? SystemIcons.Application;
        _tray.Text = WindowTitle;
        _tray.Visible = true;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowWindow());
        menu.Items.Add("Check deadlines now", null, (_, _) =>
        {
            PostToPage("{\"type\":\"recheck\"}");
            _tray.BalloonTipTitle = WindowTitle;
            _tray.BalloonTipText = "Checked. You'll be told when a deadline gets close.";
            _tray.BalloonTipIcon = ToolTipIcon.Info;
            _tray.ShowBalloonTip(4000);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => { _reallyExit = true; Close(); });
        _tray.ContextMenuStrip = menu;

        _tray.DoubleClick += (_, _) => ShowWindow();

        // Clicking the toast opens the tournament it was about.
        _tray.BalloonTipClicked += (_, _) =>
        {
            if (!string.IsNullOrEmpty(_lastNotificationUrl)) OpenInBrowser(_lastNotificationUrl!);
            else ShowWindow();
        };
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    // ---------------------------------------------------------------- lifetime

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_selfTest) SaveGeometry();

        // Closing the window keeps the deadline checks alive in the tray.
        // Quit from the tray menu (or a Windows shutdown) actually exits.
        if (_reallyExit || e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _recheck.Stop();
            return;
        }

        e.Cancel = true;
        Hide();

        if (!_toldAboutTray)
        {
            _toldAboutTray = true;
            _tray.BalloonTipTitle = "Still watching";
            _tray.BalloonTipText = "Tournament Tracker keeps checking deadlines from here. Right-click to quit.";
            _tray.BalloonTipIcon = ToolTipIcon.Info;
            _tray.ShowBalloonTip(6000);
        }
    }

    private bool _toldAboutTray;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeSingleInstance.WM_SHOWME) ShowWindow();
        base.WndProc(ref m);
    }
}

