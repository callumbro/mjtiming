/*
 * Created by SharpDevelop.
 * User: Murray
 * Date: 3/4/2014
 * Time: 5:14 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RaceBeam
{
    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {
        private string timingFolder;
        private string mjFolder;
        private string today;
        private readonly CSVData configData = new CSVData();
        string configFilename = "";
        private readonly object lockObject = new object();

        private const string separator = "\r\n--------------------------------------------------------------------------------\r\n";
        private string runTimes;
        private string paxTimes;
        private string rawTimes;
        private string coneCounts;
        private string classtimes;
        private string teamtimes;
        private string statistics;
        private int queryCount = 0;
        private string compare_results = "";
        private string prev_results = "";
        private string template = "<html><head><style type=\"text/css\">%{style}%</style></head><body><h3>M&J Solo Live Timing</h3><a href=\"/\">Home</a><div>%{content}%</div></body></html>";
        private string style = "";
        private readonly static string links = "<ul>%{runs}%%{raw}%%{classes}%%{pax}%%{team}%%{cones}%</ul>";

        HttpListener listener = null;

        public MainForm()
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();

            Load_config();
        }

        public void Load_config()
        {
            mjFolder = Process.GetCurrentProcess().MainModule.FileName;
            mjFolder = Path.GetDirectoryName(mjFolder);
            mjFolder += "\\..";
            configFilename = mjFolder + "\\config\\configData.csv";
            scoresTextBox.Font = new Font(scoresTextBox.Font.FontFamily, 6);

            if (File.Exists(configFilename) == false)
            {
                MessageBox.Show("Unable to find config file: " + configFilename);
                Environment.Exit(0);
            }
            string err = configData.LoadData(configFilename, ',', "Parameter");
            if (err != "")
            {
                MessageBox.Show("Unable to load config file: " + err, "MJTiming");
                Environment.Exit(0);
            }
            // set some scoring defaults
            int items = scoringList.Items.Count;
            for (int dex = 0; dex < items; dex++)
            {
                scoringList.SetItemChecked(dex, true);
            }
            bestRunRadioButton.Checked = true;

            today = DateTime.Now.ToString("yyyy_MM_dd");
            if (Day1TextBox.Text == "")
            {
                Day1TextBox.Text = today;
            }
            Day2TextBox.Text = "";
            timingFolder = configData.GetField("eventDataFolder", "Value");
            if (timingFolder == "")
            {
                MessageBox.Show("Timing data folder not defined in config file");
                Environment.Exit(0);
            }
            // Copy over webstyles file if one doesn't exist
            string styleFile = mjFolder + "\\config\\_webStyle.css";
            if (File.Exists(styleFile) == false)
            {
                string templateFile = mjFolder + "\\config_templates\\_webStyle.css";
                File.Copy(templateFile, styleFile, false);
            }
            DataFolderTextBox.Text = timingFolder;
            var watcher = new FileSystemWatcher(timingFolder, "*_timingData.csv");
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.NotifyFilter = NotifyFilters.LastWrite;

            prev_results = "";
            GenScores();
            watcher.EnableRaisingEvents = true;
        }
        public delegate void InvokeScoring();

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            GenScores();
        }
        void ScoringListSelectedIndexChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }

        void Set1RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }

        void Set2RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }

        void Day1Plusset2RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }

        void BestRunRadioButtonCheckedChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }
        public bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public delegate void InvokeshowCount(string msg);
        public void ShowCount(string msg)
        {
            // Do the invoke here to save everyone else the bother
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new InvokeshowCount(ShowCount), new object[] { msg });
                return;
            }
            queryCount += 1;
            queryCountBox.Text = queryCount.ToString();

        }

        void WebserverButtonClick(object sender, EventArgs e)
        {
            if (webserverButton.Text.Contains("Start"))
            {
                if (IsAdministrator() == true)
                {
                    StartWebserver();
                }
                else
                {
                    scoresTextBox.Clear();
                    scoresTextBox.AppendText("Not running as administrator\n");
                    return;
                }
                webserverButton.Text = "Stop web server";
                webserverButton.BackColor = Color.Red;
                webserverButton.ForeColor = Color.White;
            }
            else
            {
                StopWebserver();
                webserverButton.Text = "Start web server";
                webserverButton.BackColor = Color.Lime;
                webserverButton.ForeColor = Color.Black;

            }
        }

        void GenScores()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new InvokeScoring(GenScores));
                return;
            }
            var args = new scoreArgs
            {
                day1 = Day1TextBox.Text,
                day2 = Day2TextBox.Text
            };

            if (int.TryParse(OfficialRunsTextBox.Text, out args.maxOfficialRuns) == false)
            {
                args.maxOfficialRuns = 999;
            }
            args.writeCSV = false;
            args.eventFolder = "";

            foreach (string item in scoringList.CheckedItems)
            {
                if (item.Contains("Run Times"))
                    args.showRunTimes = true;
                else if (item.Contains("Raw Times"))
                    args.showRawTimes = true;
                else if (item.Contains("Class Times"))
                    args.showClassTimes = true;
                else if (item.Contains("PAX Times"))
                    args.showPaxTimes = true;
                else if (item.Contains("Team"))
                    args.showTeams = true;
                else if (item.Contains("Cone"))
                    args.showConeCounts = true;
            }

            if (bestRunRadioButton.Checked == true)
            {
                args.bestSingleRun = true;  // override to single day
            }
            else if (day1Plusset2RadioButton.Checked == true)
            {
                args.set1PlusSet2 = true;   // score as recorded
            }
            else if (set1RadioButton.Checked == true)
            {
                args.set1Only = true;
            }
            if (set2RadioButton.Checked == true)
            {
                args.set2Only = true;
            }

            string results = "";
            lock (lockObject)
            {
                textScores
                    .textScoreSplit(
                        args,
                        out runTimes,
                        out rawTimes,
                        out paxTimes,
                        out classtimes,
                        out teamtimes,
                        out coneCounts,
                        out statistics);

                if (args.showRunTimes) results += separator + runTimes;
                if (args.showRawTimes) results += separator + rawTimes;
                if (args.showPaxTimes) results += separator + paxTimes;
                if (args.showClassTimes) results += separator + classtimes;
                if (args.showConeCounts) results += separator + coneCounts;
                if (args.showTeams) results += separator + teamtimes;
                compare_results = results;
                if (string.IsNullOrEmpty(statistics) == false)
                    results += separator + statistics;
            }

            // Only update display if results have changed
            // We get triggered when the timing file is zeroed out for the next save,
            // so we need to check for reducing size as well as changing content
            if ((compare_results != prev_results) &&
                (compare_results.Length >= prev_results.Length))
            {
                prev_results = compare_results;
                scoresTextBox.Clear();
                scoresTextBox.AppendText(results);
                scoresTextBox.SelectionStart = 0;
                scoresTextBox.ScrollToCaret();

                // TODO: Send generated html files to webserver, via ftp?.

            }
        }

        public void StartWebserver()
        {
            if (listener != null)
            {
                try
                {
                    listener.Close();
                    listener = null;
                }
                catch
                {

                }
            }
            listener = new HttpListener
            {
                IgnoreWriteExceptions = true
            };

            // read in web formatting files from the pages folder
            string folder = Environment.ProcessPath;
            folder = Path.GetDirectoryName(folder);
            folder += "\\..";
            string styleFilename = folder + "\\config\\_webStyle.css";
            style = File.ReadAllText(styleFilename);
            template = template.Replace("%{style}%", style);

            listener.Prefixes.Add("http://+:80/");
            listener.Start();

            listener.BeginGetContext(new AsyncCallback(OnRequestReceive), listener);
        }

        private void OnRequestReceive(IAsyncResult result)
        {
            var webListener = (HttpListener)result.AsyncState;
            try
            {
                ShowCount("");
                if ((webListener == null) || (webListener.IsListening == false))
                {
                    return;
                }
                HttpListenerContext context = webListener.EndGetContext(result);
                string response = "";

                string request = context.Request.RawUrl;
                if (request == "/")
                {
                    string currentLinks = links;
                    foreach (string item in scoringList.CheckedItems)
                    {
                        if (item.Contains("Run Times"))
                            currentLinks = currentLinks.Replace("%{runs}%", "<li><a href=\"runs\">Runs</a></li>");
                        else if (item.Contains("Raw Times"))
                            currentLinks = currentLinks.Replace("%{raw}%", "<li><a href=\"raw\">Raw</a></li>");
                        else if (item.Contains("PAX Times"))
                            currentLinks = currentLinks.Replace("%{pax}%", "<li><a href=\"pax\">PAX</a></li>");
                        else if (item.Contains("Class Times"))
                            currentLinks = currentLinks.Replace("%{classes}%", "<li><a href=\"classes\">Classes</a></li>");
                        else if (item.Contains("Cone"))
                            currentLinks = currentLinks.Replace("%{cones}%", "<li><a href=\"cones\">Cones</a></li>");
                        else if (item.Contains("Team"))
                            currentLinks = currentLinks.Replace("%{team}%", "<li><a href=\"team\">Team</a></li>");
                    }
                    response = template.Replace("%{content}%", currentLinks);
                }
                else if (request == "/runs")
                {
                    lock (lockObject)
                    {
                        response = template.Replace("%{content}%", "<pre>" + runTimes + "</pre>");
                    }
                }
                else if (request == "/raw")
                {
                    lock (lockObject)
                    {
                        response = template.Replace("%{content}%", "<pre>" + rawTimes + "</pre>");
                    }
                }
                else if (request == "/pax")
                {
                    lock (lockObject)
                    {
                        response = template.Replace("%{content}%", "<pre>" + paxTimes + "</pre>");
                    }
                }
                else if (request == "/classes")
                {
                    lock (lockObject)
                    {
                        response = template.Replace("%{content}%", "<pre>" + classtimes + "</pre>");
                    }
                }
                else if (request == "/team")
                {
                    lock (lockObject)
                    {
                        response = template.Replace("%{content}%", "<pre>" + teamtimes + "</pre>");
                    }
                }
                else if (request == "/cones")
                {
                    lock (lockObject)
                    {
                        response = template.Replace("%{content}%", "<pre>" + coneCounts + "</pre>");
                    }
                }

                // Replace any remaining placeholders
                response = Regex.Replace(response, @"%{\w*}%", "");

                byte[] byteArr = Encoding.ASCII.GetBytes(response);
                context.Response.OutputStream.Write(byteArr, 0, byteArr.Length);
                context.Response.Close();
            }
            catch //(Exception ex)
            {
                //scoresTextBox.Clear();
                //scoresTextBox.AppendText(ex.Message + "\n\r" + ex.StackTrace);
            }
            // ---> start listening for another request
            webListener.BeginGetContext(new AsyncCallback(OnRequestReceive), webListener);
        }

        public void StopWebserver()
        {
            try
            {
                if (listener != null)
                {
                    listener.Close();
                    listener = null;
                }
            }
            catch
            {

            }
        }
    }
}
