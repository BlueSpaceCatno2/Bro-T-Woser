using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Win32Interop.Structs;

namespace WebBrowserApp
{
    public partial class Form1 : Form
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
        //if you touch these, you will learn what a cheese slicer is.
        private TextBox textBox2;
        private Button Search;
        private Button Forward;
        private Button Backward;
        private Button Refresh;
        private Button button3;
        private Button HOME;
        private Button NewTabButton;
        private Button CloseTabButton;
        private WebView2 webView21;
        private TabControl tabControl;
        private MenuStrip menuStrip;
        private ToolStripMenuItem settingsMenuItem;
        private ToolStripMenuItem quickLinksMenuItem;
        private ToolStripMenuItem editQuickLinksMenuItem;
        private Panel topPanel;
        private Panel sidePanel;
        private FlowLayoutPanel sideFlow;

        private readonly Dictionary<TabPage, WebView2> tabWebViews = new();
        private readonly Dictionary<TabPage, string> tabUrls = new();
        private WebView2 currentWebView;

        static readonly string rootDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrjT");
        static readonly string userDataFolder = Path.Combine(rootDataFolder, "UserData");
        const string SettingsFileName = "settings.json";

        private AppSettings settings = new();

        private class AppSettings
        {
            public int CurrentSiteIndex { get; set; } = 0;
            public Dictionary<string, string> SideButtons { get; set; } = new()
                {
                    { "SOverflow", "https://stackoverflow.com" },
                    { "Outlook", "https://outlook.com" },
                    { "Github", "https://github.com" },
                    { "Wikipedia", "https://wikipedia.org" },
                    { "Spotify", "https://spotify.com" },
                    { "IntSpeed", "http://dl.google.com/googletalk/googletalk-setup.exe" },
                    { "Archive", "https://archive.org" }
                };
            public List<string> Suggestions { get; set; } = new() { "weather", "news", "stackoverflow", "github", "spotify" };
            // /\ real shortcuts, use these if your cool
            public List<string> OpenTabs { get; set; } = new List<string>();
        }

        static private int currentSiteIndex = 0;
        static private readonly string[] sites = { "https://www.google.com/xhtml/search", "https://search.yahoo.com", "https://bing.com", "https://duckduckgo.com" };
        static private readonly string[] siteNames = { "Google", "Yahoo", "Bing", "DuckDuckGo" };

        public Form1()
        {
            InitializeComponent();

            //VERY IMPORTANTO!
            textBox2.KeyDown += textBox2_KeyDown;
            this.Resize += (_, _) => LayoutControls();

            //scary stuff.
            this.KeyPreview = true;
            this.KeyDown += Form_KeyDown;

            try
            {
                if (!Directory.Exists(userDataFolder)) Directory.CreateDirectory(userDataFolder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("User data folder error: " + ex.Message);
            }

            LoadSettings();
            ConfigureSearchSuggestions();
            PopulateQuickLinksMenu();
            ApplySideButtons();
            CreateInitialTab();
        }

        //even scawyer!
        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T)
            {
                e.SuppressKeyPress = true;
                _ = CreateNewTab(sites[currentSiteIndex]);
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SearchIT();
            }
        } //thing that makes it do the internet thingy.

        private void LoadSettings()
        {
            try
            {
                var path = Path.Combine(userDataFolder, SettingsFileName);
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null) settings = loaded;
                    if (settings.CurrentSiteIndex >= 0 && settings.CurrentSiteIndex < sites.Length) currentSiteIndex = settings.CurrentSiteIndex;
                }
            }
            catch (Exception ex) { Debug.WriteLine("LoadSettings failed: " + ex.Message); }
        }

        private void SaveSettings()
        {
            try
            {   //ohh god who put this here, im rewriting this and if you redo it ill get the cheese slicer.
                settings.CurrentSiteIndex = currentSiteIndex;
                if (tabControl != null && tabControl.TabPages.Count > 0)
                {
                    var urls = tabControl.TabPages.Cast<TabPage>()
                                .Where(tp => tp.Text != "+")
                                .Select(tp => tabUrls.TryGetValue(tp, out var u) ? u : "")
                                .Where(u => !string.IsNullOrEmpty(u))
                                .ToList();
                    settings.OpenTabs = urls;
                }

                var path = Path.Combine(userDataFolder, SettingsFileName);
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex) { Debug.WriteLine("SaveSettings failed: " + ex.Message); }
        }

        private async void CreateInitialTab()
        {
            try
            {
                
                if (settings.OpenTabs != null && settings.OpenTabs.Count > 0)
                {
                    foreach (var u in settings.OpenTabs)
                    {
                        await CreateNewTab(u, persist: false);
                    }
                }
                else
                {
                    await CreateNewTab(sites[currentSiteIndex], persist: false);
                }

                EnsurePlusTab();
                if (tabControl.TabPages.Count > 0) tabControl.SelectedIndex = 0;
            }
            catch (Exception)
            {
            }
        }


        private async Task CreateNewTab(string initialUrl = null, bool persist = true)
        {
            if (string.IsNullOrEmpty(initialUrl)) initialUrl = sites[currentSiteIndex];

            //Please try to make it make sense to my stupid brain.
            int insertIndex = tabControl.TabPages.Count;
            var plusIdx = FindPlusTabIndex();
            if (plusIdx >= 0) insertIndex = plusIdx; //forgot to put this in dumbass.

            var tp = new TabPage("New Tab") { Padding = new Padding(0) };
            var view = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = System.Drawing.Color.White, ZoomFactor = 1D };
            tp.Controls.Add(view);

            tabControl.TabPages.Insert(insertIndex, tp);
            tabWebViews[tp] = view;
            tabUrls[tp] = initialUrl;
            tabControl.SelectedTab = tp;
            currentWebView = view;

            if (persist)
            {
                if (settings.OpenTabs == null) settings.OpenTabs = new List<string>();
                settings.OpenTabs.Add(initialUrl);
                SaveSettings();
            }

            try
            {
                if (!Directory.Exists(userDataFolder)) Directory.CreateDirectory(userDataFolder);
            }
            catch { }

            await InitializeWebViewFor(view, initialUrl);
            tp.Text = GetFriendlyTitle(initialUrl);
            EnsurePlusTab();
            tabControl.Invalidate();
        }

        private int FindPlusTabIndex()
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                if (tabControl.TabPages[i].Text == "+") return i;
            }
            return -1;
        }

        private void EnsurePlusTab()
        {
            var idx = FindPlusTabIndex();
            if (idx >= 0) return;
            var plus = new TabPage("+") { Padding = new Padding(0) };
            tabControl.TabPages.Add(plus);
        }

        private string GetFriendlyTitle(string uri)
        {
            try
            {
                var u = new Uri(uri);
                return u.Host.Replace("www.", "");
            }
            catch
            {
                return uri.Length > 20 ? uri.Substring(0, 20) + "..." : uri;
            }
        }

        private async Task InitializeWebViewFor(WebView2 webView, string initialUrl)
        {
            string tabUserData = userDataFolder;

            if (!Directory.Exists(tabUserData))
            {
                Directory.CreateDirectory(tabUserData);
            }

            try
            {
                var env = await CoreWebView2Environment.CreateAsync(null, tabUserData, new CoreWebView2EnvironmentOptions("--allow-file-access-from-files"));
                await webView.EnsureCoreWebView2Async(env);

                webView.NavigationStarting += WebView_NavigationStarting;
                webView.NavigationCompleted += WebView_NavigationCompleted;
                webView.SourceChanged += WebView_SourceChanged;
                webView.ContentLoading += WebView_ContentLoading;
                webView.Click += WebView_Click;

                webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", rootDataFolder, CoreWebView2HostResourceAccessKind.Allow);

                webView.CoreWebView2.Navigate(initialUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 initialization failed for tab: " + ex.Message); //if you get this error, your fucked.
            }
        }

        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var uri = e.Request.Uri;
            if (uri.Contains("r.bing.com/rp/kFAqShRrnkQMbH6NYLBYoJ3lq9s.png"))
            {
                var core = sender as CoreWebView2;
                if (core?.Environment != null)
                {
                    e.Response = core.Environment.CreateWebResourceResponse(null, 403, "Blocked", "Content-Type: text/plain");
                    return;
                }

                e.Response = tabWebViews.Values.FirstOrDefault()?.CoreWebView2?.Environment.CreateWebResourceResponse(null, 403, "Blocked", "Content-Type: text/plain");
            }
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            this.BeginInvoke(new Action(async () => await CreateNewTab(e.Uri)));
        }

        private void SearchIT()
        {
            _ = SearchITAsync();
        }

        private async Task SearchITAsync()
        {
            var selectedSearchEngine = new UriBuilder(sites[currentSiteIndex]);
            try
            {
                string url = textBox2.Text;
                if (string.IsNullOrWhiteSpace(url)) url = sites[currentSiteIndex];
                else
                {
                    if (!url.StartsWith("http"))
                    {
                        if (!url.StartsWith("www."))
                        {
                            static bool splitnum(string input)
                            {
                                string[] parts = input.Split('.');

                                foreach (var part in parts)
                                {
                                    if (part.Length > 0 && (part[0] >= '0' && part[0] <= '9')) return true;
                                }

                                return false;
                            }
                            if (!url.Contains("."))
                            {
                                selectedSearchEngine = new UriBuilder(sites[currentSiteIndex]);
                                selectedSearchEngine.Query = "q=" + Uri.EscapeDataString(url);
                                url = selectedSearchEngine.ToString();
                            }
                            else
                            {
                                if (!splitnum(url))
                                {
                                    var RUrl = url;
                                    url = "https://www." + url;
                                    try
                                    {
                                        textBox2.Text = url;
                                        if (currentWebView != null)
                                        {
                                            await currentWebView.EnsureCoreWebView2Async(null);
                                            currentWebView.CoreWebView2.Navigate(url);
                                        }
                                    }
                                    catch
                                    {
                                        selectedSearchEngine.Query = "q=" + Uri.EscapeDataString(RUrl);
                                        var fallback = selectedSearchEngine.ToString();
                                        if (currentWebView != null)
                                        {
                                            await currentWebView.EnsureCoreWebView2Async(null);
                                            currentWebView.CoreWebView2.Navigate(fallback);
                                        }
                                    }
                                    return;
                                }
                                else
                                {
                                    selectedSearchEngine = new UriBuilder(sites[currentSiteIndex]);
                                    selectedSearchEngine.Query = "q=" + Uri.EscapeDataString(url);
                                    url = selectedSearchEngine.ToString();
                                }
                            }
                        }
                        else
                        {
                            var RUrl = url;
                            url = "https://" + url;
                            try
                            {
                                textBox2.Text = url;
                                if (currentWebView != null)
                                {
                                    await currentWebView.EnsureCoreWebView2Async(null);
                                    currentWebView.CoreWebView2.Navigate(url);
                                }
                            }
                            catch
                            {
                                selectedSearchEngine.Query = "q=" + Uri.EscapeDataString(RUrl);
                                var fallback = selectedSearchEngine.ToString();
                                if (currentWebView != null)
                                {
                                    await currentWebView.EnsureCoreWebView2Async(null);
                                    currentWebView.CoreWebView2.Navigate(fallback);
                                }
                            }
                            return;
                        }
                    }
                }
                textBox2.Text = url;
                if (currentWebView != null)
                {
                    await currentWebView.EnsureCoreWebView2Async(null);
                    currentWebView.CoreWebView2.Navigate(url);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var view = sender as WebView2;
            if (view == currentWebView)
            {
                try
                {
                    var sURL = view.Source.ToString();
                    textBox2.Text = sURL;
                }
                catch { }
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            var view = sender as WebView2;
            try
            {
                string sURL = view.Source.ToString();
                var tp = tabWebViews.FirstOrDefault(kv => kv.Value == view).Key;
                if (tp != null)
                {
                    tp.Text = GetFriendlyTitle(sURL);
                    tabUrls[tp] = sURL;

                    
                    var urls = tabControl.TabPages.Cast<TabPage>()
                                .Where(t => t.Text != "+")
                                .Select(t => tabUrls.TryGetValue(t, out var u) ? u : "")
                                .Where(u => !string.IsNullOrEmpty(u))
                                .ToList();
                    settings.OpenTabs = urls;
                    SaveSettings();
                }

                if (view == currentWebView)
                {
                    textBox2.Text = sURL;
                }
            }
            catch { }
        }

        private void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            var view = sender as WebView2;
            if (view == currentWebView)
            {
                try
                {
                    var sURL = view.Source.ToString();
                    textBox2.Text = sURL;
                }
                catch { }
            }
        }

        private void WebView_ContentLoading(object sender, CoreWebView2ContentLoadingEventArgs e)
        {
            var view = sender as WebView2;
            if (view == currentWebView)
            {
                try
                {
                    var sURL = view.Source.ToString();
                    textBox2.Text = sURL;
                }
                catch { }
            }
        }

        private void WebView_Click(object sender, EventArgs e)
        {
            var view = sender as WebView2;
            if (view == currentWebView)
            {
                textBox2.Text = view.Source.ToString();
            }
        }

        private void ConfigureSearchSuggestions()
        {
            try
            {
                var source = new AutoCompleteStringCollection();
                source.AddRange(settings.Suggestions.ToArray());
                textBox2.AutoCompleteCustomSource = source;
            }
            catch { }
        }

        private void PopulateQuickLinksMenu()
        {
            quickLinksMenuItem.DropDownItems.Clear();
            foreach (var kv in settings.SideButtons)
            {
                var item = new ToolStripMenuItem(kv.Key) { Tag = kv.Value };
                item.Click += QuickLinkMenuItem_Click;
                quickLinksMenuItem.DropDownItems.Add(item);
            }
        }

        private void QuickLinkMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is string url)
            {
                NavigateActive(url);
            }
        }

        private void InitializeComponent()
        {
            topPanel = new Panel();
            sidePanel = new Panel();
            sideFlow = new FlowLayoutPanel();

            textBox2 = new TextBox();
            Search = new Button();
            webView21 = new WebView2();
            Refresh = new Button();
            Forward = new Button();
            Backward = new Button();
            button3 = new Button();
            HOME = new Button();
            NewTabButton = new Button();
            CloseTabButton = new Button();
            tabControl = new TabControl();
            menuStrip = new MenuStrip();
            settingsMenuItem = new ToolStripMenuItem("Settings");
            quickLinksMenuItem = new ToolStripMenuItem("Quick Links");
            editQuickLinksMenuItem = new ToolStripMenuItem("Edit Quick Links");

            SuspendLayout();

            menuStrip.Items.Add(settingsMenuItem);
            settingsMenuItem.DropDownItems.Add(quickLinksMenuItem);
            settingsMenuItem.DropDownItems.Add(editQuickLinksMenuItem);
            editQuickLinksMenuItem.Click += (_, _) => OpenQuickLinksEditor();
            menuStrip.Dock = DockStyle.Top;

            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 34;
            topPanel.Padding = new Padding(4);
            topPanel.BackColor = System.Drawing.SystemColors.Control;

            //by the way, dont put snippets in WITHOUT CHECKING THAT YOU FORGOT TO PUT THE INITILIFIERS IN
            Forward.Text = "←"; Forward.Width = 28; Forward.Height = 24; Forward.Location = new System.Drawing.Point(4, 6);
            Backward.Text = "→"; Backward.Width = 28; Backward.Height = 24; Backward.Location = new System.Drawing.Point(34, 6);
            Refresh.Text = "֎"; Refresh.Width = 28; Refresh.Height = 24; Refresh.Location = new System.Drawing.Point(64, 6);
            HOME.Text = "⌂"; HOME.Width = 28; HOME.Height = 24; HOME.Location = new System.Drawing.Point(94, 6);

            textBox2.Width = 520;
            textBox2.Height = 24;
            textBox2.Location = new System.Drawing.Point(124, 6);
            textBox2.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            Search.Text = "🔎"; Search.Width = 28; Search.Height = 24; Search.Location = new System.Drawing.Point(650, 6);
            button3.Text = "⚙"; button3.Width = 28; button3.Height = 24; button3.Location = new System.Drawing.Point(754, 6); //how did you find an ascii gear????
            button3.Click += (_, _) => OpenQuickLinksEditor();

            Search.Click += (_, _) => SearchIT();
            Forward.Click += (_, _) => { if (currentWebView?.CoreWebView2 != null && currentWebView.CoreWebView2.CanGoForward) currentWebView.CoreWebView2.GoForward(); };
            Backward.Click += (_, _) => { if (currentWebView?.CoreWebView2 != null && currentWebView.CoreWebView2.CanGoBack) currentWebView.CoreWebView2.GoBack(); };
            Refresh.Click += (_, _) => { if (currentWebView?.CoreWebView2 != null) currentWebView.CoreWebView2.Reload(); };
            HOME.Click += async (_, _) => { if (currentWebView != null) { await currentWebView.EnsureCoreWebView2Async(null); currentWebView.CoreWebView2.Navigate(sites[currentSiteIndex]); } };

            topPanel.Controls.Add(Forward);
            topPanel.Controls.Add(Backward);
            topPanel.Controls.Add(Refresh);
            topPanel.Controls.Add(HOME);
            topPanel.Controls.Add(textBox2);
            topPanel.Controls.Add(Search);
            topPanel.Controls.Add(button3);

            sidePanel.Dock = DockStyle.Right;
            sidePanel.Width = 120;
            sidePanel.Padding = new Padding(4);
            sidePanel.BackColor = System.Drawing.SystemColors.ControlLight;

            sideFlow.FlowDirection = FlowDirection.TopDown;
            sideFlow.Dock = DockStyle.Fill;
            sideFlow.AutoScroll = true;
            sideFlow.WrapContents = false;
            sideFlow.Padding = new Padding(4);

            sidePanel.Controls.Add(sideFlow);

            //make it fat.
            tabControl.Dock = DockStyle.Fill;
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.MouseDown += TabControl_MouseDown;
            tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (tabControl.SelectedTab != null && tabWebViews.TryGetValue(tabControl.SelectedTab, out var w))
                {
                    currentWebView = w;
                    try { textBox2.Text = currentWebView.Source?.ToString() ?? ""; } catch { }
                }
            };
            //my sweet innocent child, why did we have to get rid of you?
            webView21.Visible = false;

            Controls.Add(tabControl);
            Controls.Add(sidePanel);
            Controls.Add(topPanel);
            Controls.Add(menuStrip);

            Text = "BroTWoser";
            ClientSize = new System.Drawing.Size(960, 600);
            StartPosition = FormStartPosition.CenterScreen;

            ResumeLayout(false);
            PerformLayout();
        }

        private void ApplySideButtons()
        {
            sideFlow.Controls.Clear();
            foreach (var kv in settings.SideButtons)
            {
                var b = new Button
                {
                    Text = kv.Key,
                    Tag = kv.Value,
                    AutoSize = false,
                    Width = sideFlow.ClientSize.Width - 10,
                    Height = 30,
                    Margin = new Padding(2)
                };
                b.Click += (s, e) =>
                {
                    if (s is Button btn && btn.Tag is string url) NavigateActive(url);
                };
                sideFlow.Controls.Add(b);
            }
        }
        //im not mad you Ai generated this, but I am dissapointed.
        private void LayoutControls()
        {
            if (topPanel != null && textBox2 != null && Search != null)
            {
                int rightSpace = topPanel.ClientSize.Width - (Search.Right + 4);
                textBox2.Width = Math.Max(200, rightSpace - 120);
            }

            foreach (Control c in sideFlow.Controls)
            {
                if (c is Button btn)
                {
                    btn.Width = Math.Max(60, sideFlow.ClientSize.Width - 10);
                }
            }
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var g = e.Graphics;
            var tab = tabControl.TabPages[e.Index];
            var rect = tabControl.GetTabRect(e.Index);
            var isSelected = (tabControl.SelectedIndex == e.Index);

            Color back = isSelected ? SystemColors.ControlLightLight : SystemColors.Control;
            using (var b = new SolidBrush(back)) g.FillRectangle(b, rect);

            if (tab.Text == "+")
            {
                var plusRect = new Rectangle(rect.Left + 4, rect.Top + 4, rect.Width - 8, rect.Height - 8);
                TextRenderer.DrawText(g, "+", Font, plusRect, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }       //the fuck is this?
            string title = tab.Text;
            var textRect = new Rectangle(rect.Left + 4, rect.Top + 4, rect.Width - 20, rect.Height - 4);
            TextRenderer.DrawText(g, title, Font, textRect, Color.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            var closeRect = new Rectangle(rect.Right - 16, rect.Top + 6, 12, 12);
            TextRenderer.DrawText(g, "×", Font, closeRect, Color.DarkRed, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void TabControl_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabCount; i++)
            {
                var rect = tabControl.GetTabRect(i);
                if (tabControl.TabPages[i].Text == "+" && rect.Contains(e.Location))
                {
                    _ = CreateNewTab(sites[currentSiteIndex]);
                    return;
                }

                var closeRect = new Rectangle(rect.Right - 16, rect.Top + 6, 12, 12);
                if (closeRect.Contains(e.Location))
                {
                    var tp = tabControl.TabPages[i];
                    if (tabUrls.TryGetValue(tp, out var url))
                    {
                        settings.OpenTabs?.Remove(url);
                    }
                    if (tabWebViews.TryGetValue(tp, out var w))
                    {
                        try { w.Dispose(); } catch { }
                        tabWebViews.Remove(tp);
                    }
                    tabUrls.Remove(tp);
                    tabControl.TabPages.Remove(tp);
                    if (tabControl.TabPages.Count == 0) _ = CreateNewTab(sites[currentSiteIndex]);
                    EnsurePlusTab();
                    SaveSettings();
                    break;
                }
            }
        }

        private void NavigateActive(string destination)
        {
            if (string.IsNullOrWhiteSpace(destination)) return;

            if (currentWebView?.CoreWebView2 != null)
            {
                try { currentWebView.CoreWebView2.Navigate(destination); }
                catch { }
            }
            else
            {
                var first = tabWebViews.Values.FirstOrDefault();
                if (first?.CoreWebView2 != null)
                {
                    try { first.CoreWebView2.Navigate(destination); currentWebView = first; }
                    catch { }
                }
            }
        }

        private void OpenQuickLinksEditor() //eat this and become cheese.
        {
            using var dlg = new QuickLinksEditor(settings);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                settings.SideButtons = dlg.ResultLinks;
                settings.Suggestions = dlg.ResultSuggestions;
                SaveSettings();
                PopulateQuickLinksMenu();
                ApplySideButtons();
                ConfigureSearchSuggestions();
            }
        } //this is edible, dont worry, but dont eat that \/

        private class QuickLinksEditor : Form
        {
            public Dictionary<string, string> ResultLinks { get; private set; } = new();
            public List<string> ResultSuggestions { get; private set; } = new();

            readonly DataGridView dgv;
            readonly TextBox suggestionsBox;
            readonly Button ok;
            readonly Button cancel;

            public QuickLinksEditor(AppSettings settings)
            {
                Text = "Edit Quick Links";
                Width = 620;
                Height = 420;
                StartPosition = FormStartPosition.CenterParent;

                dgv = new DataGridView
                {
                    Dock = DockStyle.Top,
                    Height = 260,
                    AllowUserToAddRows = true,
                    AllowUserToDeleteRows = true,
                    RowHeadersVisible = false,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
                };
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", Width = 220 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "URL", HeaderText = "URL", Width = 360 });

                foreach (var kv in settings.SideButtons)
                {
                    dgv.Rows.Add(kv.Key, kv.Value);
                }

                suggestionsBox = new TextBox
                {
                    Multiline = true,
                    Dock = DockStyle.Top,
                    Height = 80,
                    Text = string.Join(Environment.NewLine, settings.Suggestions),
                    ScrollBars = ScrollBars.Vertical
                };

                ok = new Button { Text = "OK", Dock = DockStyle.Right, Width = 90 };
                cancel = new Button { Text = "Cancel", Dock = DockStyle.Right, Width = 90 };

                var panel = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(6) };
                panel.Controls.Add(cancel);
                panel.Controls.Add(ok);

                Controls.Add(panel);
                Controls.Add(suggestionsBox);
                Controls.Add(dgv);

                ok.Click += Ok_Click;
                cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            }

            private void Ok_Click(object? sender, EventArgs e)
            {
                var dict = new Dictionary<string, string>();
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;
                    var name = (row.Cells["Name"].Value ?? "").ToString().Trim();
                    var url = (row.Cells["URL"].Value ?? "").ToString().Trim();
                    if (name.Length == 0 || url.Length == 0) continue;
                    dict[name] = url;
                }
                ResultLinks = dict;
                var suggestions = suggestionsBox.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();
                ResultSuggestions = suggestions;
                DialogResult = DialogResult.OK;
            }
        }
    }
}

//you scrolled down too far.