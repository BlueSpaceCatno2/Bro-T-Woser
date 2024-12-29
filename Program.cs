using System;
using System.Reflection.Metadata;
using System.Resources;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Windows.Forms;
using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.VisualBasic;
using Microsoft.Web.WebView2.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using Microsoft.Web.WebView2.WinForms;
using Win32Interop.Structs;
using System.Diagnostics;
using ITHit;

using ITHit.FileSystem.Windows;
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

        static int progress = 0;
        static int mode = 0;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button Forward;
        private System.Windows.Forms.Button Backward;
        private System.Windows.Forms.Button Refresh;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button HOME;
        private System.Windows.Forms.Button stoverflwbutton;
        private System.Windows.Forms.Button outlookbutton;
        private System.Windows.Forms.Button gitbutton;
        private System.Windows.Forms.Button wikibutton;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button SPOTIFY;
        private System.Windows.Forms.Button SPEEDTEST;
        private System.Windows.Forms.Button ARCHIVE;
        private System.Windows.Forms.Button DSKLOAD;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;


        public Form1()
        {
            InitializeComponent();
            textBox2.KeyDown += textBox2_KeyDown;
            InitializeWebView2();
            this.Resize += new EventHandler(Form_Resize);
            this.MouseUp += Form1_MouseUp;
        }
        static private int currentSiteIndex = 0;
        static private readonly string[] sites = { "https://www.google.com/xhtml/search", "https://search.yahoo.com", "https://bing.com", "https://duckduckgo.com" };
        static private readonly string[] siteNames = { "Google", "Yahoo", "Bing", "DuckDuckGo" };
        static string url = "bing.com";

        static string userDataFolder = @"C:\ProgramData\PrjT\UserData";
        public static extern string InputBox(string Prompt, string Title = "DSK file", string DefaultResponse = "test", int XPos = -1, int YPos = -1);
        public async void GETDSK()
        {

            string filename = InputBox("Input DSK file name (without extension)");
            try {
                string file = File.ReadAllText(filename + ".DSK");
            }
            catch
            {
                MessageBox.Show("Invalid DSK file", "Error", MessageBoxButtons.OK);
            }
            }
        private async void InitializeWebView2()
        {
            if(!(Directory.Exists(@"C:\ProgramData\DSK")))
            {
                Directory.CreateDirectory(@"C:\ProgramData\DSK");
            }
            url = textBox2.Text;
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions("--allow-file-access-from-files"));
                await webView21.EnsureCoreWebView2Async(env);

                if (!Directory.Exists(@"C:\ProgramData\PrjT"))
                {
                    Directory.CreateDirectory(@"C:\ProgramData\PrjT");
                }

                EncryptFolder(userDataFolder);

                webView21.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", @"C:\ProgramData\PrjT", CoreWebView2HostResourceAccessKind.Allow);
                webView21.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                webView21.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

                webView21.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59";
                LoadSearchEngineIndex();
                webView21.CoreWebView2.Navigate(sites[currentSiteIndex]);
                button3.Text = siteNames[currentSiteIndex];
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 initialization failed: " + ex.Message);
            }
        }
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                DoSomething_LeftClick();
            }
            else if (e.Button == MouseButtons.Right)
            {
                DoSomething_RightClick();
            }
        }

        private void DoSomething_LeftClick()
        {
            string sURL = webView21.Source.ToString();
            url = sURL;

            textBox2.Text = sURL;
        }
        private void DoSomething_RightClick()
        {
            string sURL = webView21.Source.ToString();
            url = sURL;
            textBox2.Text = sURL;
        }
        private void LoadSearchEngineIndex()
        {
            string filePath = Path.Combine(userDataFolder, "searchEngineIndex.txt");
            if (File.Exists(filePath))
            {
                string indexString = File.ReadAllText(filePath);
                if (int.TryParse(indexString, out int index))
                {
                    currentSiteIndex = index;
                }
            }
        }
        private void SaveSearchEngineIndex()
        {
            string filePath = Path.Combine(userDataFolder, "searchEngineIndex.txt");
            File.WriteAllText(filePath, currentSiteIndex.ToString());
        }





        private void button3_Click(object sender, EventArgs e)
        {
            currentSiteIndex = (currentSiteIndex + 1) % sites.Length;
            webView21.CoreWebView2.Navigate(sites[currentSiteIndex]);
            button3.Text = siteNames[currentSiteIndex];
            SaveSearchEngineIndex();
        }

        private async void HOME_Click(object sender, EventArgs e)

        {
            try
            {
                await webView21.EnsureCoreWebView2Async(null);
                webView21.CoreWebView2.Navigate(sites[currentSiteIndex]);
                button3.Text = siteNames[currentSiteIndex];
                SaveSearchEngineIndex();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to navigate to the selected site: " + ex.Message);
            }
        }



        private void EncryptFolder(string folderPath)
        {
            System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo(folderPath);
            directoryInfo.Attributes |= System.IO.FileAttributes.Encrypted;
        }

        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var uri = e.Request.Uri;
            if (uri.Contains("r.bing.com/rp/kFAqShRrnkQMbH6NYLBYoJ3lq9s.png"))
            {
                e.Response = webView21.CoreWebView2.Environment.CreateWebResourceResponse(null, 403, "Blocked", "Content-Type: text/plain");
            }
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.NewWindow = webView21.CoreWebView2;
            e.Handled = true;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            SearchIT();
        }
        List<string> bookmarks = new List<string>();
        List<string> bookmarksurl = new List<string>();

        private void Form_Resize(object sender, EventArgs e)
        {
            webView21.Width = this.ClientSize.Width - 100;
            webView21.Height = this.ClientSize.Height - 30;
        }



        //webView21.CoreWebView2.Navigate(url);
        private Dictionary<TabPage, string> tabUrls = new Dictionary<TabPage, string>();
        public async void SearchIT()
        {
            try
            {
                string url = textBox2.Text;

                if (string.IsNullOrWhiteSpace(url))
                {
                    url = sites[currentSiteIndex];
                }
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
                                    if (part.Length > 0 && part.StartsWith('1') || part.StartsWith('2') || part.StartsWith('3') || part.StartsWith('4') || part.StartsWith('5') || part.StartsWith('6') || part.StartsWith('7') || part.StartsWith('8') || part.StartsWith('9') || part.StartsWith('0'))
                                    {
                                        return true;
                                    }
                                }

                                return false;
                            }

                            if (!url.Contains("."))
                            {
                                var selectedSearchEngine = new UriBuilder(sites[currentSiteIndex]);
                                selectedSearchEngine.Query = "q=" + Uri.EscapeDataString(url);
                                url = selectedSearchEngine.ToString();
                            }
                            else
                            {
                                if (splitnum(url) == false)
                                {
                                    url = "https://www." + url;
                                }
                                else
                                {
                                    var selectedSearchEngine = new UriBuilder(sites[currentSiteIndex]);
                                    selectedSearchEngine.Query = "q=" + Uri.EscapeDataString(url);
                                    url = selectedSearchEngine.ToString();
                                }
                            }
                        }
                        else
                        {
                            url = "https://" + url;
                        }
                    }
                }
                textBox2.Text = url;
                await webView21.EnsureCoreWebView2Async(null);
                webView21.CoreWebView2.Navigate(url);

            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
        }






        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SearchIT();
            }
        }

        private void Forward_Click(object sender, EventArgs e)
        {
            if (webView21.CoreWebView2 != null && webView21.CoreWebView2.CanGoForward)
            {
                webView21.CoreWebView2.GoForward();
            }
        }

        private void Backward_Click(object sender, EventArgs e)
        {
            if (webView21.CoreWebView2 != null && webView21.CoreWebView2.CanGoBack)
            {
                webView21.CoreWebView2.GoBack();
            }
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            if (webView21.CoreWebView2 != null)
            {
                webView21.CoreWebView2.Reload();
            }
        }











        private void InitializeComponent()
        {
            textBox2 = new System.Windows.Forms.TextBox();
            Search = new System.Windows.Forms.Button();
            webView21 = new WebView2();
            Refresh = new System.Windows.Forms.Button();
            Forward = new System.Windows.Forms.Button();
            Backward = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            HOME = new System.Windows.Forms.Button();
            stoverflwbutton = new System.Windows.Forms.Button();
            outlookbutton = new System.Windows.Forms.Button();
            gitbutton = new System.Windows.Forms.Button();
            wikibutton = new System.Windows.Forms.Button();
            button4 = new System.Windows.Forms.Button();
            SPOTIFY = new System.Windows.Forms.Button();
            SPEEDTEST = new System.Windows.Forms.Button();
            ARCHIVE = new System.Windows.Forms.Button();
            DSKLOAD = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)webView21).BeginInit();
            SuspendLayout();
            // 
            // textBox2
            // 
            textBox2.Location = new Point(124, 5);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(458, 23);
            textBox2.TabIndex = 1;
            textBox2.TextChanged += textBox2_TextChanged;
            // 
            // Search
            // 
            Search.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Search.BackColor = Color.DarkRed;
            Search.Location = new Point(588, 5);
            Search.Name = "Search";
            Search.Size = new Size(22, 25);
            Search.TabIndex = 3;
            Search.Text = "🔎︎";
            Search.UseVisualStyleBackColor = false;
            Search.Click += button2_Click;
            // 
            // webView21
            // 
            webView21.AllowExternalDrop = true;
            webView21.CreationProperties = null;
            webView21.DefaultBackgroundColor = Color.White;
            webView21.Location = new Point(0, 35);
            webView21.Name = "webView21";
            webView21.Size = new Size(875, 527);
            webView21.TabIndex = 5;
            webView21.ZoomFactor = 1D;
            webView21.NavigationStarting += webView21_NavigationStarting;
            webView21.NavigationCompleted += Url_Load;
            webView21.SourceChanged += webView21_SourceChanged;
            webView21.ContentLoading += webView21_ContentLoading;
            webView21.Click += webView21_Click;
            // 
            // Refresh
            // 
            Refresh.Location = new Point(56, 5);
            Refresh.Name = "Refresh";
            Refresh.Size = new Size(16, 23);
            Refresh.TabIndex = 6;
            Refresh.Text = "֎";
            Refresh.UseVisualStyleBackColor = true;
            Refresh.Click += Refresh_Click;
            // 
            // Forward
            // 
            Forward.Location = new Point(8, 5);
            Forward.Name = "Forward";
            Forward.Size = new Size(20, 23);
            Forward.TabIndex = 7;
            Forward.Text = "←";
            Forward.UseVisualStyleBackColor = true;
            Forward.Click += Backward_Click;
            // 
            // Backward
            // 
            Backward.Location = new Point(34, 5);
            Backward.Name = "Backward";
            Backward.Size = new Size(18, 23);
            Backward.TabIndex = 8;
            Backward.Text = "→";
            Backward.UseVisualStyleBackColor = true;
            Backward.Click += Forward_Click;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button3.BackColor = SystemColors.Info;
            button3.Location = new Point(861, 6);
            button3.Name = "button3";
            button3.Size = new Size(103, 23);
            button3.TabIndex = 9;
            button3.Text = "button3";
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;
            // 
            // HOME
            // 
            HOME.Location = new Point(69, 5);
            HOME.Name = "HOME";
            HOME.Size = new Size(20, 23);
            HOME.TabIndex = 10;
            HOME.Text = "⌂";
            HOME.UseVisualStyleBackColor = true;
            HOME.Click += HOME_Click;
            // 
            // stoverflwbutton
            // 
            stoverflwbutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            stoverflwbutton.Location = new Point(889, 441);
            stoverflwbutton.Name = "stoverflwbutton";
            stoverflwbutton.Size = new Size(75, 23);
            stoverflwbutton.TabIndex = 11;
            stoverflwbutton.Text = "SOverflow";
            stoverflwbutton.UseVisualStyleBackColor = true;
            stoverflwbutton.Click += stoverflw_Click;
            // 
            // outlookbutton
            // 
            outlookbutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            outlookbutton.Location = new Point(889, 470);
            outlookbutton.Name = "outlookbutton";
            outlookbutton.Size = new Size(75, 23);
            outlookbutton.TabIndex = 12;
            outlookbutton.Text = "Outlook";
            outlookbutton.UseVisualStyleBackColor = true;
            outlookbutton.Click += Outlook_Click;
            // 
            // gitbutton
            // 
            gitbutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            gitbutton.Location = new Point(889, 499);
            gitbutton.Name = "gitbutton";
            gitbutton.Size = new Size(75, 23);
            gitbutton.TabIndex = 14;
            gitbutton.Text = "Github";
            gitbutton.UseVisualStyleBackColor = true;
            gitbutton.Click += Github_Click;
            // 
            // wikibutton
            // 
            wikibutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            wikibutton.Location = new Point(889, 528);
            wikibutton.Name = "wikibutton";
            wikibutton.Size = new Size(75, 23);
            wikibutton.TabIndex = 15;
            wikibutton.Text = "Wikipedia";
            wikibutton.UseVisualStyleBackColor = true;
            wikibutton.Click += wiki_Click;
            // 
            // button4
            // 
            button4.BackColor = SystemColors.ActiveCaption;
            button4.Location = new Point(844, 7);
            button4.Name = "button4";
            button4.Size = new Size(22, 21);
            button4.TabIndex = 16;
            button4.Text = "?";
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click_1;
            // 
            // SPOTIFY
            // 
            SPOTIFY.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            SPOTIFY.Location = new Point(889, 412);
            SPOTIFY.Name = "SPOTIFY";
            SPOTIFY.Size = new Size(75, 23);
            SPOTIFY.TabIndex = 17;
            SPOTIFY.Text = "Spotify";
            SPOTIFY.UseVisualStyleBackColor = true;
            SPOTIFY.Click += SPOTIFY_Click;
            // 
            // SPEEDTEST
            // 
            SPEEDTEST.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            SPEEDTEST.Location = new Point(889, 383);
            SPEEDTEST.Name = "SPEEDTEST";
            SPEEDTEST.Size = new Size(75, 23);
            SPEEDTEST.TabIndex = 18;
            SPEEDTEST.Text = "IntSpeed";
            SPEEDTEST.UseVisualStyleBackColor = true;
            SPEEDTEST.Click += SPEEDTEST_Click;
            // 
            // ARCHIVE
            // 
            ARCHIVE.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ARCHIVE.Location = new Point(889, 354);
            ARCHIVE.Name = "ARCHIVE";
            ARCHIVE.Size = new Size(75, 23);
            ARCHIVE.TabIndex = 19;
            ARCHIVE.Text = "Archive";
            ARCHIVE.UseVisualStyleBackColor = true;
            ARCHIVE.Click += ARCHIVE_Click;
            // 
            // DSKLOAD
            // 
            DSKLOAD.Location = new Point(95, 6);
            DSKLOAD.Name = "DSKLOAD";
            DSKLOAD.Size = new Size(23, 23);
            DSKLOAD.TabIndex = 20;
            DSKLOAD.Text = "∆";
            DSKLOAD.UseVisualStyleBackColor = true;
            DSKLOAD.Click += DSKLOAD_Click;
            // 
            // Form1
            // 
            ClientSize = new Size(962, 563);
            Controls.Add(DSKLOAD);
            Controls.Add(ARCHIVE);
            Controls.Add(SPEEDTEST);
            Controls.Add(SPOTIFY);
            Controls.Add(button4);
            Controls.Add(wikibutton);
            Controls.Add(gitbutton);
            Controls.Add(outlookbutton);
            Controls.Add(stoverflwbutton);
            Controls.Add(HOME);
            Controls.Add(button3);
            Controls.Add(Backward);
            Controls.Add(Forward);
            Controls.Add(Refresh);
            Controls.Add(webView21);
            Controls.Add(Search);
            Controls.Add(textBox2);
            Name = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)webView21).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.GetFirstCharIndexFromLine(e.NewValue);
            richTextBox1.ScrollToCaret();
        }

        private VScrollBar vScrollBar1;
        private RichTextBox richTextBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button Search;

        private void webView21_Click(object sender, EventArgs e)
        {
            textBox2.Text = url;
        }
        private async void stoverflw_Click(object sender, EventArgs e)
        {
            webView21.CoreWebView2.Navigate("https://stackoverflow.com");
        }
        private void Outlook_Click(object sender, EventArgs e)
        {
            webView21.CoreWebView2.Navigate("https://outlook.com");
        }

        private void Github_Click(object sender, EventArgs e)
        {
            webView21.CoreWebView2.Navigate("https://Github.com");
        }

        private void wiki_Click(object sender, EventArgs e)
        {
            webView21.CoreWebView2.Navigate("https://wikipedia.org");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("The Bro T Woser is a fully C# based program designed to be used for custom blocking, security, and web based searches." + Environment.NewLine + "The Bro T Woser is designed for the primary job of embedded applications for searches, but can be used a a daily driver if you want." + Environment.NewLine + "The current state of the browser is not finished, but I personally daily drive this browser, and I think it it very functional.");
        }

        private void Url_Load(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string sURL = webView21.Source.ToString();
            url = sURL;
        }

        private void webView21_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {

            string sURL = webView21.Source.ToString();
            url = sURL;
        }

        private void webView21_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {

            string sURL = webView21.Source.ToString();
            url = sURL;
        }

        private void webView21_ContentLoading(object sender, CoreWebView2ContentLoadingEventArgs e)
        {

            string sURL = webView21.Source.ToString();
            url = sURL;
        }

        private async void ARCHIVE_Click(object sender, EventArgs e)
        {
            webView21.CoreWebView2.Navigate("https://archive.org");
        }

        private void SPEEDTEST_Click(object sender, EventArgs e)
        {
            var watch = new Stopwatch();
            byte[] data;
            using (var client = new System.Net.WebClient())
            {
                watch.Start();
                data = client.DownloadData("http://dl.google.com/googletalk/googletalk-setup.exe?t=" + DateTime.Now.Ticks);
                watch.Stop();
            }
            double speed = data.LongLength / watch.Elapsed.TotalSeconds;
            double Data = data.Length;
            Data = Data * 0.00001;
            speed = speed * 0.00001;
            MessageBox.Show("Download Duration: " + watch.Elapsed + " File size: " + Data.ToString() + " MB" + " Speed: " + speed.ToString("N0").ToString() + " mbs", "SpeedTestUtility", MessageBoxButtons.OK);
        }

        private async void SPOTIFY_Click(object sender, EventArgs e)
        {
            webView21.CoreWebView2.Navigate("https://spotify.com");
        }

        private void DSKLOAD_Click(object sender, EventArgs e)
        {

        }
    }
}
