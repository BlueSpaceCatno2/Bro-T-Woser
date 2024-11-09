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
        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;

        public Form1()
        {
            InitializeComponent();
            textBox2.KeyDown += textBox2_KeyDown;
            InitializeWebView2();
            this.Resize += new EventHandler(Form_Resize);
        }
        static private int currentSiteIndex = 0;
        static private readonly string[] sites = { "https://www.google.com/xhtml/search", "https://search.yahoo.com", "https://bing.com", "https://duckduckgo.com" };
        static private readonly string[] siteNames = { "Google", "Yahoo", "Bing", "DuckDuckGo" };

        static string userDataFolder = @"C:\ProgramData\PrjT\UserData";
        private async void InitializeWebView2()
        {
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
            try {
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


        private void Form_Resize(object sender, EventArgs e)
        {
            webView21.Width = this.ClientSize.Width - 70;
           webView21.Height = this.ClientSize.Height - (webView21.Top - 70);
        }

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
                            if (!url.Contains("."))
                            {
                                var selectedSearchEngine = new UriBuilder(sites[currentSiteIndex]);
                                selectedSearchEngine.Query = "q=" + Uri.EscapeDataString(url);
                                url = selectedSearchEngine.ToString();

                            }
                            else
                            {
                                url = "https://www." + url;
                            }
                        }
                        else
                        {
                            url = "https://" + url;
                        }
                    }
                }

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
            webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            Refresh = new System.Windows.Forms.Button();
            Forward = new System.Windows.Forms.Button();
            Backward = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            HOME = new System.Windows.Forms.Button();
            stoverflwbutton = new System.Windows.Forms.Button();
            outlookbutton = new System.Windows.Forms.Button();
            gitbutton = new System.Windows.Forms.Button();
            wikibutton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)webView21).BeginInit();
            SuspendLayout();
            // 
            // textBox2
            // 
            textBox2.Location = new Point(95, 6);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(682, 23);
            textBox2.TabIndex = 1;
            // 
            // Search
            // 
            Search.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Search.Location = new Point(783, 6);
            Search.Name = "Search";
            Search.Size = new Size(75, 23);
            Search.TabIndex = 3;
            Search.Text = "SearchIT";
            Search.UseVisualStyleBackColor = true;
            Search.Click += button2_Click;
            // 
            // webView21
            // 
            webView21.AllowExternalDrop = true;
            webView21.CreationProperties = null;
            webView21.DefaultBackgroundColor = Color.White;
            webView21.Location = new Point(8, 35);
            webView21.Name = "webView21";
            webView21.Size = new Size(861, 489);
            webView21.TabIndex = 5;
            webView21.ZoomFactor = 1D;
            webView21.Click += webView21_Click;
            // 
            // Refresh
            // 
            Refresh.Location = new Point(56, 6);
            Refresh.Name = "Refresh";
            Refresh.Size = new Size(16, 23);
            Refresh.TabIndex = 6;
            Refresh.Text = "֎";
            Refresh.UseVisualStyleBackColor = true;
            Refresh.Click += Refresh_Click;
            // 
            // Forward
            // 
            Forward.Location = new Point(8, 6);
            Forward.Name = "Forward";
            Forward.Size = new Size(20, 23);
            Forward.TabIndex = 7;
            Forward.Text = "←";
            Forward.UseVisualStyleBackColor = true;
            Forward.Click += Backward_Click;
            // 
            // Backward
            // 
            Backward.Location = new Point(32, 6);
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
            button3.Location = new Point(861, 6);
            button3.Name = "button3";
            button3.Size = new Size(103, 23);
            button3.TabIndex = 9;
            button3.Text = "button3";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // HOME
            // 
            HOME.Location = new Point(69, 6);
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
            stoverflwbutton.Location = new Point(875, 404);
            stoverflwbutton.Name = "stoverflwbutton";
            stoverflwbutton.Size = new Size(89, 23);
            stoverflwbutton.TabIndex = 11;
            stoverflwbutton.Text = "SOverflow";
            stoverflwbutton.UseVisualStyleBackColor = true;
            stoverflwbutton.Click += stoverflw_Click;
            // 
            // outlookbutton
            // 
            outlookbutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            outlookbutton.Location = new Point(875, 433);
            outlookbutton.Name = "outlookbutton";
            outlookbutton.Size = new Size(89, 23);
            outlookbutton.TabIndex = 12;
            outlookbutton.Text = "Outlook";
            outlookbutton.UseVisualStyleBackColor = true;
            outlookbutton.Click += Outlook_Click;
            // 
            // gitbutton
            // 
            gitbutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            gitbutton.Location = new Point(875, 462);
            gitbutton.Name = "gitbutton";
            gitbutton.Size = new Size(89, 23);
            gitbutton.TabIndex = 14;
            gitbutton.Text = "Github";
            gitbutton.UseVisualStyleBackColor = true;
            gitbutton.Click += Github_Click;
            // 
            // wikibutton
            // 
            wikibutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            wikibutton.Location = new Point(875, 491);
            wikibutton.Name = "wikibutton";
            wikibutton.Size = new Size(89, 23);
            wikibutton.TabIndex = 15;
            wikibutton.Text = "Wikipedia";
            wikibutton.UseVisualStyleBackColor = true;
            wikibutton.Click += wiki_Click;
            // 
            // Form1
            // 
            ClientSize = new Size(962, 526);
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
        private System.Windows.Forms.TextBox textBox1;

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }


        private void webView21_Click(object sender, EventArgs e)
        {

        }
        //webView21.CoreWebView2.Navigate("https://stackoverflow.com");
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

    }
}
