/*
 * Generates score output in html format
 */
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RaceBeam
{
    public static class htmlScores
    {
        // returned list of all driver's score data
        public static Dictionary<string, scoreCalcs.driverScoreData> scores;
        // Returned list of team scores
        public static List<scoreCalcs.teamData> teamScores;
        // Returned list of all class data, sorted by sort order given in file
        public static SortedDictionary<int, scoreCalcs.paxInfo> sortedClassList;
        // Returned statistics
        public static scoreCalcs.statsDataClass stats;
        // The arguments to tell us what to show
        public static scoreArgs args;

        public static CSVData configData = new CSVData();
        public static bool showLastName = false;
        private static string htmlStyleTemplate = "";
        private static string htmlHeaderTemplate = "<head><style>%{STYLE}%</style><script type=\"text/javascript\">function showResults(id) { let allResults = document.getElementsByClassName('results'); for (const element of allResults) { element.setAttribute('style', 'display: none;'); } document.getElementById(id).setAttribute('style', 'display: block;'); }</script></head>";
        private static string htmlBodyTemplate = "<body><h1>%{TITLE}%</h1>%{BUTTONS}%%{RESULTS}%</body>";
        private static string htmlFooterTemplate = "<hr><footer><h3 margin-top=\"50px\">Statistics</h3>%{STATS}%</footer>";
        private static string htmlFooterContentTemplate = "<div style=\"padding-bottom: 15px;\"><div>%{DAY}%, ${SET}%: </div><span>First run: %{FIRST}%</span><span>Last run: %{LAST}% </span><span>Number of runs: %{NUMRUNS}% </span><span>Avg run time: %{AVGTIME}%</span></div>";

        public static string htmlScore(scoreArgs myArgs, out string html)
        {
            args = myArgs;

            html = "";
            string htmlStyle = htmlStyleTemplate;
            string htmlHeader = htmlHeaderTemplate;
            string htmlBody = htmlBodyTemplate;
            string htmlFooter = htmlFooterTemplate;

            string configFolder = args.configFolder;
            string configFilename;
            if (configFolder == "")
            {
                configFolder = Process.GetCurrentProcess().MainModule.FileName;
                configFolder = Path.GetDirectoryName(configFolder);
                configFolder = configFolder + "\\..\\config";
            }
            configFilename = configFolder + "\\configData.csv";
            string err = configData.LoadData(configFilename, ',', "Parameter");
            if (err != "")
            {
                return "Unable to load config file: " + err;
            }

            if (configData.GetField("ShowLastName", "Value").Contains("Y"))
            {
                showLastName = true;
            }

            // Do some scoring calcs
            err = scoreCalcs
                .doScore(
                    args,
                    out scores,
                    out teamScores,
                    out stats,
                    out sortedClassList);

            if (string.IsNullOrEmpty(err) == false)
            {
                return err;
            }

            string styleFilename = configFolder + "\\_scoreStyles.css";
            try
            {
                htmlStyle = File.ReadAllText(styleFilename);
            }
            catch
            {
                // No style sheet there, so don't use style sheet
                htmlStyle = "";
            }

            htmlHeader = htmlHeader.Replace("%{STYLE}%", htmlStyle);

            string htmlButtons = "";
            string htmlResults = "";
            string htmlStats = "";

            if (args.showRunTimes == true)
            {
                string runTimesResult = runTimes(args);
                if (!string.IsNullOrWhiteSpace(runTimesResult))
                {
                    htmlButtons += "<button class=\"button\" onclick=\"showResults('run-times');\">Runs</button>";
                    htmlResults += runTimesResult;
                }
            }
            if (args.showRawTimes == true)
            {
                string rawTimesResult = rawTimes(args);
                if (!string.IsNullOrEmpty(rawTimesResult))
                {
                    htmlButtons += "<button class=\"button\" onclick=\"showResults('overall-ranking');\">Overall</button>";
                    htmlResults += rawTimesResult;
                }
            }
            if (args.showPaxTimes == true)
            {
                string paxTimesResult = paxTimes(args);
                if (!string.IsNullOrEmpty(paxTimesResult))
                {
                    htmlButtons += "<button class=\"button\" onclick=\"showResults('pax-ranking');\">Pax</button>";
                    htmlResults += paxTimesResult;
                }
            }
            if (args.showClassTimes == true)
            {
                string classTimesResult = classTimes(args);
                if (!string.IsNullOrEmpty(classTimesResult))
                {
                    htmlButtons += "<button class=\"button\" onclick=\"showResults('class-ranking');\">Class</button>";
                    htmlResults += classTimesResult;
                }
            }
            if (args.showTeams == true)
            {
                string teamsResult = Teams(args);
                if (!string.IsNullOrEmpty(teamsResult))
                {
                    htmlButtons += "<button class=\"button\" onclick=\"showResults('team-ranking');\">Teams</button>";
                    htmlResults += teamsResult;
                }
            }
            if (args.showConeCounts == true)
            {
                string coneCountsResult = coneCounts(args);
                if (!string.IsNullOrEmpty(coneCountsResult))
                {
                    htmlButtons += "<button class=\"button\" onclick=\"showResults('cone-ranking');\">Cones</button>";
                    htmlResults += coneCountsResult;
                }
            }

            htmlStats += Statistics(args);

            htmlBody = htmlBody.Replace("%{TITLE}%", "M&J Solo Live Timing");
            htmlBody = htmlBody.Replace("%{BUTTONS}%", htmlButtons);
            htmlBody = htmlBody.Replace("%{RESULTS}%", htmlResults);
            htmlFooter = htmlFooter.Replace("%{STATS}%", htmlStats);

            html = "<html>" + htmlHeader + htmlBody + htmlFooter + "</html>";

            return "";
        }

        /// <summary>
        /// print out run data
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string runTimes(scoreArgs args)
        {
            return "";
        }

        public static string rawTimes(scoreArgs args)
        {
            StringBuilder results = new StringBuilder();
            results.Append("<div id=\"overall-ranking\" class=\"results\" style=\"display: none;\">");
            results.Append("<h2>Overall Ranking</h2>");
            results.Append("<table>");
            results.Append("<thead>");
            results.Append("<tr>");
            results.Append("<th colspan=\"8\">Run Times (ordered by quickest)</th>");
            results.Append("</tr>");
            results.Append("<tr>");
            results.Append("<th>Rank</th>");
            results.Append("<th>Class</th>");
            results.Append("<th>Driver</th>");
            results.Append("<th>Car</th>");
            results.Append("<th>Best Time</th>");
            results.Append("<th>Diff.</th>");
            results.Append("<th>Diff. Prev.</th>");
            results.Append("<th>Raw Times</th>");
            results.Append("</tr>");
            results.Append("</thead>");
            results.Append("<tbody>");

            var myList = new List<KeyValuePair<string, scoreCalcs.driverScoreData>>(scores);
            // Sort by raw time
            myList
                .Sort(delegate (
                    KeyValuePair<string, scoreCalcs.driverScoreData> firstPair,
                    KeyValuePair<string, scoreCalcs.driverScoreData> nextPair)
                {
                    return firstPair.Value.scoreData.bestRAW.CompareTo(nextPair.Value.scoreData.bestRAW);
                });

            int rank = 1;
            double classLeadTime = 0.0;
            double classPrevTime = 0.0;
            foreach (KeyValuePair<string, scoreCalcs.driverScoreData> driver in myList)
            {
                if (rank == 1)
                {
                    // Initialize for time difference calculations.
                    classLeadTime = driver.Value.scoreData.bestRAW;
                    classPrevTime = driver.Value.scoreData.bestRAW;
                }

                string driverName = driver.Value.firstName + " " + driver.Value.lastName.Substring(0, 1);
                if (showLastName)
                {
                    driverName = driver.Value.firstName + " " + driver.Value.lastName;
                }

                string driverRAW;
                string driverDiff = "";
                string driverDiffPrev = "";
                driverRAW = driver.Value.scoreData.bestRAW.ToString("#0.000");
                if (driver.Value.scoreData.bestRAW >= scoreCalcs.DNFvalue)
                {
                    driverRAW = "DNS";
                }
                else
                {
                    driverDiff = (driver.Value.scoreData.bestRAW - classLeadTime).ToString("#0.000");
                    driverDiffPrev = (driver.Value.scoreData.bestRAW - classPrevTime).ToString("#0.000");
                }

                results.Append("<tr>");
                results.Append($"<td>{rank}</td>");
                results.Append($"<td>{driver.Value.carClass}</td>");
                results.Append($"<td>{driverName}</td>");
                results.Append($"<td>{driver.Value.carDescription}</td>");
                results.Append($"<td>{driverRAW}</td>");
                results.Append($"<td>{driverDiff}</td>");
                results.Append($"<td>{driverDiffPrev}</td>");
                results.Append("<td>");
                results.Append("<div class=\"raw-results\">");
                results.Append(printSet(driver.Value.Day1Set1, 1, 1));
                results.Append("</div>");
                results.Append("</td>");
                results.Append("</tr>");

                // Update the previous time for the next driver's time difference calculation.
                classPrevTime = driver.Value.scoreData.bestRAW;
                rank++;
            }

            results.Append("</tbody>");
            results.Append("</table>");
            results.Append("</div>");
            return results.ToString();
        }

        /// <summary>
        /// Return a printable text string for PAX data
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string paxTimes(scoreArgs args)
        {
            return "";
        }

        /// <summary>
        /// print out times grouped by classes, ordered by raw time
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string classTimes(scoreArgs args)
        {
            StringBuilder results = new StringBuilder();
            results.Append("<div id=\"class-ranking\" class=\"results\">");
            results.Append("<h2>Class Ranking</h2>");

            // Sort drivers by raw time
            var myList = new List<KeyValuePair<string, scoreCalcs.driverScoreData>>(scores);
            myList
                .Sort(delegate (
                    KeyValuePair<string, scoreCalcs.driverScoreData> firstPair,
                    KeyValuePair<string, scoreCalcs.driverScoreData> nextPair)
                {
                    return firstPair.Value.scoreData.bestRAW.CompareTo(nextPair.Value.scoreData.bestRAW);
                });

            foreach (KeyValuePair<int, scoreCalcs.paxInfo> classInfo in sortedClassList)
            {
                var curClass = classInfo.Value;
                string curClassGroup = curClass.group;

                if (!myList.Any(o => o.Value.carClass == curClass.carClass))
                {
                    // Skip if there are no drivers in the class.
                    continue;
                }

                results.Append("<table>");
                results.Append("<thead>");
                results.Append("<tr>");
                results.Append($"<th colspan=\"9\">{curClass.carClass} ({curClass.description})</th>");
                results.Append("</tr>");
                results.Append("<tr>");
                results.Append("<th>Rank</th>");
                results.Append("<th>Class</th>");
                results.Append("<th>Driver</th>");
                results.Append("<th>Car</th>");
                results.Append("<th>Best Time</th>");
                results.Append("<th>Diff.</th>");
                results.Append("<th>Diff. Prev.</th>");
                results.Append("<th>Score</th>");
                results.Append("<th>Raw Times</th>");
                results.Append("</tr>");
                results.Append("</thead>");
                results.Append("<tbody>");

                double classLeadTime = 0.0;
                double classPrevTime = 0.0;
                foreach (KeyValuePair<string, scoreCalcs.driverScoreData> driver in myList)
                {
                    scoreCalcs.groupscore grpPtr = null;
                    foreach (scoreCalcs.groupscore grp in driver.Value.scoreData.groupScores)
                    {
                        if (grp.groupName == curClass.carClass)
                        {
                            grpPtr = grp;
                            break;
                        }
                    }
                    if (grpPtr == null)
                    {
                        continue;
                    }

                    if (grpPtr.groupRank == 1)
                    {
                        // Initialize for time difference calculations.
                        classLeadTime = driver.Value.scoreData.bestRAW;
                        classPrevTime = driver.Value.scoreData.bestRAW;
                    }

                    string driverName = driver.Value.firstName + " " + driver.Value.lastName.Substring(0, 1);
                    if (showLastName)
                    {
                        driverName = driver.Value.firstName + " " + driver.Value.lastName;
                    }

                    string driverRAW;
                    string driverDiff = "";
                    string driverDiffPrev = "";
                    driverRAW = driver.Value.scoreData.bestRAW.ToString("#0.000");
                    if (driver.Value.scoreData.bestRAW >= scoreCalcs.DNFvalue)
                    {
                        driverRAW = "DNS";
                    }
                    else
                    {
                        driverDiff = (driver.Value.scoreData.bestRAW - classLeadTime).ToString("#0.000");
                        driverDiffPrev = (driver.Value.scoreData.bestRAW - classPrevTime).ToString("#0.000");
                    }

                    results.Append("<tr>");
                    results.Append($"<td>{grpPtr.groupRank}</td>");
                    results.Append($"<td>{driver.Value.carClass}</td>");
                    results.Append($"<td>{driverName}</td>");
                    results.Append($"<td>{driver.Value.carDescription}</td>");
                    results.Append($"<td>{driverRAW}</td>");
                    results.Append($"<td>{driverDiff}</td>");
                    results.Append($"<td>{driverDiffPrev}</td>");
                    results.Append($"<td>{grpPtr.groupScore.ToString("#0.000")}</td>");
                    results.Append("<td>");
                    results.Append("<div class=\"raw-results\">");
                    results.Append(printSet(driver.Value.Day1Set1, 1, 1));
                    results.Append("</div>");
                    results.Append($"</td>");
                    results.Append("</tr>");

                    // Update the previous time for the next driver's time difference calculation.
                    classPrevTime = driver.Value.scoreData.bestRAW;
                }

                results.Append("</tbody>");
                results.Append("</table>");
            }

            results.Append("</div>");
            return results.ToString();
        }

        /// <summary>
        /// Team scores
        /// We group RAW and PAX if given in the driver's group filed
        /// If the group is something else, then we ignore the group and show the team for both raw and pax
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Teams(scoreArgs args)
        {
            return "";
        }

        /// <summary>
        /// Return a printable text string for cone counts
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string coneCounts(scoreArgs args)
        {
            return "";
        }

        public static string Statistics(scoreArgs args)
        {
            StringBuilder results = new StringBuilder();

            results.Append("<div class=\"statistics\">");

            if (stats.day1 != null)
            {
                if (stats.day1.set1NumberOfRuns > 0)
                {
                    results.Append("<span class=\"driving-set-title\">Day 1, Set 1</span>");
                    results.Append("<br>");
                    results.Append("<span class=\"driving-set\">");
                    results.Append($"<span>First run: {stats.day1.set1TimeOfFirstRun}</span>");
                    results.Append($"<span>Last run: {stats.day1.set1TimeOfLastRun}</span>");
                    results.Append($"<span>Number of runs: {stats.day1.set1NumberOfRuns}</span>");
                    results.Append($"<span>Average run time: {(stats.day1.set1TotalTime / stats.day1.set1NumberOfRuns).ToString("#0.000")}</span>");
                    results.Append("</span>");
                    results.Append("</div>");
                }
                if (stats.day1.set2NumberOfRuns > 0)
                {
                    results.Append("<span class=\"driving-set-title\">Day 1, Set 2</span>");
                    results.Append("<br>");
                    results.Append("<span class=\"driving-set\">");
                    results.Append($"<span>First run: {stats.day1.set2TimeOfFirstRun}</span>");
                    results.Append($"<span>Last run: {stats.day1.set2TimeOfLastRun}</span>");
                    results.Append($"<span>Number of runs: {stats.day1.set2NumberOfRuns}</span>");
                    results.Append($"<span>Average run time: {(stats.day1.set2TotalTime / stats.day1.set2NumberOfRuns).ToString("#0.000")}</span>");
                    results.Append("</span>");
                    results.Append("</div>");
                }
                if (stats.day1.set3NumberOfRuns > 0)
                {
                    results.Append("<span class=\"driving-set-title\">Day 1, Fun Runs (Set 3)</span>");
                    results.Append("<br>");
                    results.Append("<span class=\"driving-set\">");
                    results.Append($"<span>First run: {stats.day1.set3TimeOfFirstRun}</span>");
                    results.Append($"<span>Last run: {stats.day1.set3TimeOfLastRun}</span>");
                    results.Append($"<span>Number of runs: {stats.day1.set3NumberOfRuns}</span>");
                    results.Append($"<span>Average run time: {(stats.day1.set3TotalTime / stats.day1.set3NumberOfRuns).ToString("#0.000")}</span>");
                    results.Append("</span>");
                    results.Append("</div>");
                }
            }

            // Day 2 if we have one
            if (stats.day2 != null)
            {
                if (stats.day2.set1NumberOfRuns > 0)
                {
                    results.Append("<span class=\"driving-set-title\">Day 1, Set 1</span>");
                    results.Append("<br>");
                    results.Append("<span class=\"driving-set\">");
                    results.Append($"<span>First run: {stats.day2.set1TimeOfFirstRun}</span>");
                    results.Append($"<span>Last run: {stats.day2.set1TimeOfLastRun}</span>");
                    results.Append($"<span>Number of runs: {stats.day2.set1NumberOfRuns}</span>");
                    results.Append($"<span>Average run time: {(stats.day2.set1TotalTime / stats.day2.set1NumberOfRuns).ToString("#0.000")}</span>");
                    results.Append("</span>");
                    results.Append("</div>");
                }
                if (stats.day2.set2NumberOfRuns > 0)
                {
                    results.Append("<span class=\"driving-set-title\">Day 1, Set 2</span>");
                    results.Append("<br>");
                    results.Append("<span class=\"driving-set\">");
                    results.Append($"<span>First run: {stats.day2.set2TimeOfFirstRun}</span>");
                    results.Append($"<span>Last run: {stats.day2.set2TimeOfLastRun}</span>");
                    results.Append($"<span>Number of runs: {stats.day2.set2NumberOfRuns}</span>");
                    results.Append($"<span>Average run time: {(stats.day2.set2TotalTime / stats.day2.set2NumberOfRuns).ToString("#0.000")}</span>");
                    results.Append("</span>");
                    results.Append("</div>");
                }
                if (stats.day2.set3NumberOfRuns > 0)
                {
                    results.Append("<span class=\"driving-set-title\">Day 1, Fun Runs (Set 3)</span>");
                    results.Append("<br>");
                    results.Append("<span class=\"driving-set\">");
                    results.Append($"<span>First run: {stats.day2.set3TimeOfFirstRun}</span>");
                    results.Append($"<span>Last run: {stats.day2.set3TimeOfLastRun}</span>");
                    results.Append($"<span>Number of runs: {stats.day2.set3NumberOfRuns}</span>");
                    results.Append($"<span>Average run time: {(stats.day2.set3TotalTime / stats.day2.set3NumberOfRuns).ToString("#0.000")}</span>");
                    results.Append("</span>");
                    results.Append("</div>");
                }
            }

            results.Append("</div>");

            return results.ToString();
        }

        #region Private Helpers

        private static string printSet(scoreCalcs.singleSetData setData, int dayNumber, int setNumber)
        {
            List<string> runs = new List<string>();

            bool hasRuns = false;
            int startRunNumber = -1;
            foreach (var run in setData.runs)
            {
                string currentRun = "";
                hasRuns = true;
                if (startRunNumber < 0)
                {
                    // Note that run numbers don't start at 1 -- they increment across sets
                    startRunNumber = run.runNumber;
                }
                if ((run.runNumber - startRunNumber) == args.maxOfficialRuns)
                {
                    // Skip fun runs
                }
                if (run.penalty == "DNF")
                {
                    if (run.time >= scoreCalcs.DNFvalue)
                    {
                        // didn't even run
                        currentRun += "<span class=\"raw-time penalty\">DNS</span>";
                    }
                    else
                    {
                        currentRun += $"<span class=\"raw-time\">{run.time.ToString("#0.000")}<span class=\"penalty\">+DNF</span></span>";
                    }
                }
                else if (run.penalty == "")
                {
                    if (run == setData.bestRun)
                    {
                        currentRun += $"<span class=\"highlight raw-time\">{run.time.ToString("#0.000")}</span>";
                    }
                    else
                    {
                        currentRun += $"<span class=\"raw-time\">{run.time.ToString("#0.000")}</span>";
                    }
                }
                else
                {
                    if (run == setData.bestRun)
                    {
                        currentRun += $"<span class=\"highlight raw-time\">{run.time.ToString("#0.000")}<span class=\"penalty\">+{run.penalty}</span></span>";
                    }
                    else
                    {
                        currentRun += $"<span class=\"raw-time\">{run.time.ToString("#0.000")}<span class=\"penalty\">+{run.penalty}</span></span>";
                    }
                }

                runs.Add(currentRun);
            }
            if (hasRuns == false)
            {
                runs.Add("<span class=\"raw-time penalty\">DNS</span>");
            }

            return string.Join("", runs);
        }

        /// <summary>
        /// Times > DNFvalue denote a DNS
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private static string timeOrDNS(double time)
        {
            if (time >= scoreCalcs.DNFvalue)
            {
                // didn't even run
                return "DNS   ";
            }
            else
            {
                return string.Format("{0,8:#.000}", time);
            }
        }

        private static int GetLeadingInt(string input)
        {
            int i = 0;
            input = input.Trim();
            while (i < input.Length && char.IsDigit(input[i])) i++;

            input = input.Substring(0, i);
            if (int.TryParse(input, out int value) == false)
            {
                return 0;
            }
            return value;
        }

        #endregion
    }
}
