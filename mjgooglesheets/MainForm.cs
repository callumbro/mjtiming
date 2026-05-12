using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;

namespace RaceBeam
{
    public partial class MainForm : Form
    {
        private string timingFolder;
        private string mjFolder;
        private string today;
        private readonly CSVData configData = new CSVData();
        private string configFilename = "";
        private readonly object lockObject = new object();
        private FileSystemWatcher timingDataFileWatcher;

        private const string separator = "\r\n--------------------------------------------------------------------------------\r\n";
        private string runTimes;
        private string paxTimes;
        private string rawTimes;
        private string coneCounts;
        private string classtimes;
        private string teamtimes;
        private string statistics;
        private string compare_results = "";
        private string prev_results = "";

        private UserCredential sheetCredential;
        private readonly System.Windows.Forms.Timer sheetDebounceTimer = new System.Windows.Forms.Timer();
        private volatile bool sheetPushInFlight;
        private scoreArgs lastPushArgsForSheets;

        public MainForm()
        {
            InitializeComponent();
            sheetDebounceTimer.Interval = 450;
            sheetDebounceTimer.Tick += SheetDebounceTimer_Tick;

            var saved = SheetFormSettings.Load();
            clientIdTextBox.Text = saved.ClientId;
            clientSecretTextBox.Text = saved.ClientSecret;
            spreadsheetIdTextBox.Text = saved.SpreadsheetId;
            applicationNameTextBox.Text = string.IsNullOrEmpty(saved.ApplicationName) ? "MJ Timing Live" : saved.ApplicationName;
            syncEnabledCheckBox.Checked = saved.SyncEnabled;

            Load_config();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            new SheetFormSettings
            {
                ClientId = clientIdTextBox.Text,
                ClientSecret = clientSecretTextBox.Text,
                SpreadsheetId = spreadsheetIdTextBox.Text,
                ApplicationName = applicationNameTextBox.Text,
                SyncEnabled = syncEnabledCheckBox.Checked
            }.Save();
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

            DataFolderTextBox.Text = timingFolder;
            timingDataFileWatcher = new FileSystemWatcher(timingFolder, "*_timingData.csv");
            timingDataFileWatcher.Changed += OnChanged;
            timingDataFileWatcher.NotifyFilter = NotifyFilters.LastWrite;

            prev_results = "";
            GenScores();
            timingDataFileWatcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            GenScores();
        }

        private void ScoringListSelectedIndexChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }

        private void Set1RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }

        private void Set2RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }

        private void Day1Plusset2RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }

        private void BestRunRadioButtonCheckedChanged(object sender, EventArgs e)
        {
            prev_results = "";
            GenScores();
        }

        public delegate void InvokeScoring();

        private void GenScores()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new InvokeScoring(GenScores));
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
                {
                    args.showRunTimes = true;
                }
                else if (item.Contains("Raw Times"))
                {
                    args.showRawTimes = true;
                }
                else if (item.Contains("Class Times"))
                {
                    args.showClassTimes = true;
                }
                else if (item.Contains("PAX Times"))
                {
                    args.showPaxTimes = true;
                }
                else if (item.Contains("Team"))
                {
                    args.showTeams = true;
                }
                else if (item.Contains("Cone"))
                {
                    args.showConeCounts = true;
                }
            }

            if (bestRunRadioButton.Checked == true)
            {
                args.bestSingleRun = true;
            }
            else if (day1Plusset2RadioButton.Checked == true)
            {
                args.set1PlusSet2 = true;
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

                if (args.showRunTimes)
                {
                    results += separator + runTimes;
                }

                if (args.showRawTimes)
                {
                    results += separator + rawTimes;
                }

                if (args.showPaxTimes)
                {
                    results += separator + paxTimes;
                }

                if (args.showClassTimes)
                {
                    results += separator + classtimes;
                }

                if (args.showConeCounts)
                {
                    results += separator + coneCounts;
                }

                if (args.showTeams)
                {
                    results += separator + teamtimes;
                }

                compare_results = results;
                if (string.IsNullOrEmpty(statistics) == false)
                {
                    results += separator + statistics;
                }

                lastPushArgsForSheets = new scoreArgs(args);
            }

            if ((compare_results != prev_results) &&
                (compare_results.Length >= prev_results.Length))
            {
                prev_results = compare_results;
                scoresTextBox.Clear();
                scoresTextBox.AppendText(results);
                scoresTextBox.SelectionStart = 0;
                scoresTextBox.ScrollToCaret();
                ScheduleSheetSync();
            }
        }

        private void ScheduleSheetSync()
        {
            if (!syncEnabledCheckBox.Checked)
            {
                return;
            }

            sheetDebounceTimer.Stop();
            sheetDebounceTimer.Start();
        }

        private void SheetDebounceTimer_Tick(object sender, EventArgs e)
        {
            sheetDebounceTimer.Stop();
            _ = PushSheetAsync();
        }

        private async Task PushSheetAsync(bool allowWhenSyncDisabled = false, bool wipeTargetTabsBeforePush = false)
        {
            if (sheetPushInFlight)
            {
                return;
            }

            if (!allowWhenSyncDisabled && !syncEnabledCheckBox.Checked)
            {
                return;
            }

            sheetPushInFlight = true;
            try
            {
                if (string.IsNullOrWhiteSpace(spreadsheetIdTextBox.Text))
                {
                    SetSheetStatus("Spreadsheet ID is empty.");
                    return;
                }

                if (sheetCredential == null)
                {
                    SetSheetStatus("Click “Grant Google permission” after entering Client ID and Secret.");
                    return;
                }

                SheetScoreExport.SheetScoreGrid overallGrid;
                List<(string Title, SheetScoreExport.SheetScoreGrid Grid)> classTabs;
                SheetScoreExport.SheetScoreGrid statisticsTab;
                lock (lockObject)
                {
                    if (textScores.scores == null || textScores.scores.Count == 0)
                    {
                        SetSheetStatus("No scores loaded yet.");
                        return;
                    }

                    if (lastPushArgsForSheets == null)
                    {
                        SetSheetStatus("Scoring not ready yet.");
                        return;
                    }

                    overallGrid = SheetScoreExport.SheetOverallRankingRows(
                        lastPushArgsForSheets,
                        textScores.scores,
                        textScores.showLastName);
                    classTabs = SheetScoreExport.SheetClassRankingTabs(
                        lastPushArgsForSheets,
                        textScores.scores,
                        textScores.sortedClassList,
                        textScores.showLastName);
                    statisticsTab = SheetScoreExport.SheetStatisticsTab(textScores.stats);
                }

                SetSheetStatus("Updating Google Sheets…");
                int rangeCount = await GoogleSheetSync.PushOverallAndClassSheetsAsync(
                    sheetCredential,
                    applicationNameTextBox.Text,
                    spreadsheetIdTextBox.Text.Trim(),
                    overallGrid,
                    classTabs,
                    new Progress<string>(SetSheetStatus),
                    CancellationToken.None,
                    statisticsTab,
                    wipeTargetTabsBeforePush).ConfigureAwait(true);
                if (rangeCount == 0)
                {
                    SetSheetStatus("Sheets unchanged (already up to date) at " + DateTime.Now.ToString("HH:mm:ss"));
                }
                else
                {
                    SetSheetStatus("Sheets updated (" + rangeCount + " range(s)) at " + DateTime.Now.ToString("HH:mm:ss"));
                }
            }
            catch (Exception ex)
            {
                SetSheetStatus("Sheet error: " + ex.Message);
            }
            finally
            {
                sheetPushInFlight = false;
            }
        }

        private void SetSheetStatus(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetSheetStatus), msg);
                return;
            }

            sheetStatusLabel.Text = msg;
        }

        private void UpdateGrantGoogleButtonText()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateGrantGoogleButtonText));
                return;
            }

            grantGoogleButton.Text = sheetCredential != null
                ? "Regrant Google permission…"
                : "Grant Google permission…";
        }

        private async void ForceFullRefreshButtonClick(object sender, EventArgs e)
        {
            GoogleSheetSync.ClearPushSnapshotsForSpreadsheet(spreadsheetIdTextBox.Text);
            SetSheetStatus("Deleting all worksheets and recreating result tabs…");
            await PushSheetAsync(allowWhenSyncDisabled: true, wipeTargetTabsBeforePush: true).ConfigureAwait(true);
        }

        private async void GrantGoogleButtonClick(object sender, EventArgs e)
        {
            grantGoogleButton.Enabled = false;
            SetSheetStatus("Authorizing…");
            try
            {
                sheetCredential = await GoogleSheetSync.AuthorizeAsync(
                    clientIdTextBox.Text,
                    clientSecretTextBox.Text,
                    applicationNameTextBox.Text,
                    new Progress<string>(SetSheetStatus),
                    CancellationToken.None).ConfigureAwait(true);
                SetSheetStatus("Signed in. Token saved under " + GoogleSheetSync.TokenStorePath);
                if (syncEnabledCheckBox.Checked)
                {
                    ScheduleSheetSync();
                }
            }
            catch (Exception ex)
            {
                SetSheetStatus("Authorization failed: " + ex.Message);
                sheetCredential = null;
            }
            finally
            {
                grantGoogleButton.Enabled = true;
                UpdateGrantGoogleButtonText();
            }
        }

        private void SyncEnabledCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            if (syncEnabledCheckBox.Checked)
            {
                ScheduleSheetSync();
            }
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(clientIdTextBox.Text) ||
                string.IsNullOrWhiteSpace(clientSecretTextBox.Text))
            {
                UpdateGrantGoogleButtonText();
                return;
            }

            if (!Directory.Exists(GoogleSheetSync.TokenStorePath) ||
                Directory.GetFiles(GoogleSheetSync.TokenStorePath).Length == 0)
            {
                SetSheetStatus("Enter OAuth credentials, then click Grant Google permission.");
                UpdateGrantGoogleButtonText();
                return;
            }

            try
            {
                SetSheetStatus("Restoring saved Google sign-in…");
                sheetCredential = await GoogleSheetSync.AuthorizeAsync(
                    clientIdTextBox.Text,
                    clientSecretTextBox.Text,
                    applicationNameTextBox.Text,
                    null,
                    CancellationToken.None).ConfigureAwait(true);
                SetSheetStatus("Google sign-in restored.");
                if (syncEnabledCheckBox.Checked)
                {
                    ScheduleSheetSync();
                }
            }
            catch
            {
                sheetCredential = null;
                SetSheetStatus("Saved sign-in invalid — click Grant Google permission.");
            }

            UpdateGrantGoogleButtonText();
        }
    }
}
