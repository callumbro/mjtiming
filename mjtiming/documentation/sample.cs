// Compile command:
// C:\Windows\Microsoft.NET\Framework\v3.5\Csc.exe /reference:c:\mjtiming\bin\mjcommon.dll /platform:x86 sample.cs
// Note that the resulting sample.exe will not work unless it is copied into mjtiming\bin

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace RaceBeam  // this is required to get easy reference to the datatypes
{
    class PaxScore
    {
        // returned list of all driver's score data
        public static Dictionary<string, scoreCalcs.driverScoreData> scores;
        // Returned list of team scores
        public static List<scoreCalcs.teamData> teamScores;
        // Returned list of all class data, sorted by sort order given in file
        public static SortedDictionary<int, scoreCalcs.paxInfo> sortedClassList;
        // Returned statistics
        public static scoreCalcs.statsDataClass stats;

        public static CSVData configData = new CSVData();
        public static bool showLastName = false;
        private static string htmlStyle = "";
        private static string htmlHeader = "<head><style>%{STYLE}%</style><script type=\"text/javascript\">function showResults(id) { let allResults = document.getElementsByClassName('results'); for (const element of allResults) { element.setAttribute('style', 'display: none;'); } document.getElementById(id).setAttribute('style', 'display: block;'); }</script></head>";
        private static string htmlBody = "<body><h1>%{TITLE}%</h1>%{BUTTONS}%%{RESULTS}%</body>";
        private static string htmlFooter = "<footer><hr><h3 margin-top=\"50px\">Statistics</h3>%{STATS}%</footer>";
        private static string htmlFooterContent = "<div style=\"padding-bottom: 15px;\"><div>%{DAY}%, ${SET}%: </div><span style=\"padding-right: 20px;\">First run: %{FIRST}%</span><span style=\"padding-right: 20px;\">Last run: %{LAST}% </span><span style=\"padding-right: 20px;\">Number of runs: %{NUMRUNS}% </span><span style=\"padding-right: 20px;\">Avg run time: %{AVGTIME}%</span></div>";

        public static void Usage()
        {
            Console.WriteLine("Usage: score -day1 <day1 date> -day2 <day2 date> -set1only -set2only -maxofficialruns <# runs> -classfile <path to class.csv file> -title <string> -path <path to event data folder>");
            Environment.Exit(0);
        }

        public static void Main(string[] args)
        {
            // parse command line arguments
            // default to 1 day scoring, today's date, first set only

            var argblock = new scoreArgs();     // parameters passed to the scoring module
            argblock.day1 = DateTime.Now.ToString("yyyy_MM_dd");
            argblock.set1Only = true;

            string configFolder = Environment.ProcessPath;
            configFolder = Path.GetDirectoryName(configFolder);
            configFolder += "\\..\\config";

            string configFilename = configFolder + "\\configData.csv";
            string err = configData.LoadData(configFilename, ',', "Parameter");
            if (err != "")
            {
                Console.WriteLine("Unable to load config file: " + err);
                Usage();
            }

            if (configData.GetField("ShowLastName", "Value").Contains("Y"))
            {
                showLastName = true;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "-h") | (args[i] == "-help") | (args[i] == "-?"))
                {
                    Usage();
                }
                else if (args[i] == "-day1")
                {
                    i += 1;
                    argblock.day1 = args[i];
                }
                else if (args[i] == "-day2")
                {
                    i += 1;
                    argblock.day2 = args[i];
                }
                else if (args[i] == "-set1only")
                {
                    argblock.set1Only = true;
                }
                else if (args[i] == "-set2only")
                {
                    argblock.set2Only = true;
                }
                else if (args[i] == "-maxofficialruns")
                {
                    i += 1;
                    if (int.TryParse(args[i], out argblock.maxOfficialRuns) == false)
                    {
                        argblock.maxOfficialRuns = 999;
                    }
                }
                else if (args[i] == "-classfile")
                {
                    i += 1;
                    argblock.classFile = args[i];
                }
                else if (args[i] == "-title")
                {
                    i += 1;
                    argblock.title = args[i];
                }
                else if (args[i] == "-path")
                {
                    i += 1;
                    argblock.eventFolder = args[i];
                }
                else
                {
                    Usage();
                }
            }

            // Do the scoring calcs
            err = scoreCalcs
                .doScore(
                    argblock,
                    out scores,
                    out teamScores,
                    out stats,
                    out sortedClassList);

            if (string.IsNullOrEmpty(err) == false)
            {
                Console.WriteLine(err);
                return;
            }

            // Now generate results in format of our choice
            // Let the cmd script redirect to the file

            string styleFilename = configFolder + "\\_scoreStyles.css";
            try
            {
                htmlStyle = File.ReadAllText(styleFilename);
            }
            catch
            {
                // No style sheet there, so don't use  style sheet
                htmlStyle = "";
            }

            htmlHeader = htmlHeader.Replace("%{STYLE}%", htmlStyle);

            string pathStart;
            if (string.IsNullOrEmpty(argblock.day2))
            {
                pathStart = argblock.eventFolder + "\\" + argblock.day1;
            }
            else
            {
                pathStart = argblock.eventFolder + "\\" + argblock.day2 + "__2-day";
            }

            string htmlButtons = "";
            string htmlResults = "";
            string htmlStats = "";

            htmlButtons += "<button class=\"button\" onclick=\"showResults('class-ranking');\">Class</button>";
            htmlButtons += "<button class=\"button\" onclick=\"showResults('overall-ranking');\">Overall</button>";

            htmlResults += ClassTimes(argblock);
            htmlResults += OverallTimes(argblock);
            htmlStats += Statistics(argblock);

            htmlBody = htmlBody.Replace("%{TITLE}%", argblock.title);
            htmlBody = htmlBody.Replace("%{BUTTONS}%", htmlButtons);
            htmlBody = htmlBody.Replace("%{RESULTS}%", htmlResults);
            htmlFooter = htmlFooter.Replace("%{STATS}%", htmlStats);

            string results = "<html>" + htmlHeader + htmlBody + htmlFooter + "</html>";

            string path = pathStart + "__htmlResults.html";
            File.WriteAllText(path, results);

            Console.WriteLine($"Results saved to '{path}'");
        }

        /// <summary>
        /// print out times organized by classes, ordered by raw time
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string ClassTimes(scoreArgs args)
        {
            StringBuilder results = new();
            results.Append("<div id=\"class-ranking\" class=\"results\">");
            results.Append("<h2>Class Ranking</h2>");
            results.Append("<table>");

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
                    results.Append(PrintSet(args, driver.Value.Day1Set1, 1, 1));
                    results.Append("</div>");
                    results.Append($"</td>");
                    results.Append("</tr>");

                    // Update the previous time for the next driver's time difference calculation.
                    classPrevTime = driver.Value.scoreData.bestRAW;
                }

                results.Append("</tbody>");
            }

            results.Append("</table>");
            results.Append("</div>");
            return results.ToString();
        }

        /// <summary>
        /// print out overall run time data
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string OverallTimes(scoreArgs args)
        {
            StringBuilder results = new();
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
                results.Append(PrintSet(args, driver.Value.Day1Set1, 1, 1));
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

        public static string Statistics(scoreArgs args)
        {
            StringBuilder results = new();

            results.Append("<div style=\"padding-bottom: 15px;\">");

            if (stats.day1.set1NumberOfRuns > 0)
            {
                results.Append("<div>Day 1, Set 1</div>");
                results.Append($"<span style=\"padding-right: 20px;\">First run: {stats.day1.set1TimeOfFirstRun}</span>");
                results.Append($"<span style=\"padding-right: 20px;\">Last run: {stats.day1.set1TimeOfLastRun}</span>");
                results.Append($"<span style=\"padding-right: 20px;\">Number of runs: {stats.day1.set1NumberOfRuns}</span>");
                results.Append($"<span style=\"padding-right: 20px;\">Average run time: {(stats.day1.set1TotalTime / stats.day1.set1NumberOfRuns).ToString("#0.000")}</span>");
                results.Append("</div>");
            }
            if (stats.day1.set2NumberOfRuns > 0)
            {
                results.Append("<div>Day 1, Set 2</div>");
                results.Append($"<span style=\"padding-right: 20px;\">First run: {stats.day1.set2TimeOfFirstRun}</span>");
                results.Append($"<span style=\"padding-right: 20px;\">Last run: {stats.day1.set2TimeOfLastRun}</span>");
                results.Append($"<span style=\"padding-right: 20px;\">Number of runs: {stats.day1.set2NumberOfRuns}</span>");
                results.Append($"<span style=\"padding-right: 20px;\">Average run time: {(stats.day1.set2TotalTime / stats.day1.set2NumberOfRuns).ToString("#0.000")}</span>");
                results.Append("</div>");
            }

            results.Append("</div>");

            return results.ToString();
        }

        #region Helpers

        protected static string PrintSet(scoreArgs args, scoreCalcs.singleSetData setData, int dayNumber, int setNumber)
        {
            List<string> runs = new();

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
                        currentRun += "<span class=\"raw-time\">DNS</span>";
                    }
                    else
                    {
                        currentRun += $"<span class=\"raw-time\">{run.time.ToString("#0.000")}+DNF</span>";
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
                        currentRun += $"<span class=\"highlight raw-time\">{run.time.ToString("#0.000")}+{run.penalty}</span>";
                    }
                    else
                    {
                        currentRun += $"<span class=\"raw-time\">{run.time.ToString("#0.000")}+{run.penalty}</span>";
                    }
                }

                runs.Add(currentRun);
            }
            if (hasRuns == false)
            {
                runs.Add("<span class=\"raw-time\">DNS</span>");
            }

            return string.Join("", runs);
        }

        #endregion
    }
}