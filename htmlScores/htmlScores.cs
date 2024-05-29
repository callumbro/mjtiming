/*
 * Created by Murray Peterson
 * 
 * Compile command:
 * C:\Windows\Microsoft.NET\Framework\v3.5\Csc.exe /reference:c:\mjtiming\bin\mjcommon.dll /platform:x86 htmlScores.cs
 * You will need to copy the resulting htmlScores.exe into mjtiming\bin before it will work.
 */

// Compile command:
// C:\Windows\Microsoft.NET\Framework\v3.5\csc.exe /reference:c:\mjtiming\bin\mjcommon.dll /platform:x86 sample.cs
// Note that the resulting sample.exe will not work unless it is copied into mjtiming\bin

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RaceBeam  // this is required to get easy reference to the datatypes
{
    static class HtmlScore
    {
        // returned list of all driver's score data
        public static Dictionary<string, ScoreCalcs.DriverScoreData> scores;
        // Returned list of team scores
        public static List<ScoreCalcs.TeamData> teamScores;
        // Returned list of all class data, sorted by sort order given in file
        public static SortedDictionary<int, ScoreCalcs.PaxInfo> sortedClassList;
        // Returned statistics
        public static ScoreCalcs.StatsDataClass stats;

        public static CSVData configData = new CSVData();
        public static bool showLastName = false;
        private static string htmlStyle = "";
        private static string htmlHeader = "<html><head>%STYLE%</head><body>";
        private const string htmlFooter = @"</body></html>";
        private const string htmlTableHeader = @"
<table width=""100%"" cellpadding=""3"" style=""border-collapse: collapse"" border=""1"" align=""left"">
<th class=hdr1 colspan=""99"" nowrap align=""center"">%TABLENAME%</th>
";
        private const string htmlTableFooter = @"</table>";

        public static void Usage()
        {
            Console.WriteLine("Usage: htmlscore -day1 <day1 date> -day2 <day2 date> -bestsinglerun -set1only -set2only -runtimes -rawtimes -paxtimes -teams -conecounts -classtimes -xgrouptimes -rookie -maxofficialruns <# runs> -classfile <path to class.csv file> -title <string> -path <path to event data folder>");
            Environment.Exit(0);
        }

        public static void Main(string[] args)
        {
            // parse command line arguments
            // default to 1 day scoring, today's date, first set only
            ScoreArgs scoreArgs = new ScoreArgs();
            var argblock = scoreArgs;
            argblock.Day1 = DateTime.Now.ToString("yyyy_MM_dd");
            string configFilename;
            string configFolder = argblock.ConfigFolder;

            configFolder = Process.GetCurrentProcess().MainModule.FileName;
            configFolder = Path.GetDirectoryName(configFolder);
            configFolder = configFolder + "\\..\\config";

            configFilename = configFolder + "\\configData.csv";
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
                    argblock.Day1 = args[i];
                }
                else if (args[i] == "-day2")
                {
                    i += 1;
                    argblock.Day2 = args[i];
                }
                else if (args[i] == "-classfile")
                {
                    i += 1;
                    argblock.ClassFile = args[i];
                }
                else if (args[i] == "-title")
                {
                    i += 1;
                    argblock.Title = @"<h1 align=""center"">" + args[i] + "</h1>";
                }
                else if (args[i] == "-path")
                {
                    i += 1;
                    argblock.EventFolder = args[i];
                }
                else if (args[i] == "-set1only")
                {
                    argblock.Set1Only = true;
                }
                else if (args[i] == "-set2only")
                {
                    argblock.Set2Only = true;
                }
                else if (args[i] == "-bestsinglerun")
                {
                    argblock.BestSingleRun = true;
                }
                else if (args[i] == "-set1plusset2")
                {
                    argblock.Set1PlusSet2 = true;
                }
                else if (args[i] == "-runtimes")
                {
                    argblock.ShowRunTimes = true;
                }
                else if (args[i] == "-rawtimes")
                {
                    argblock.ShowRawTimes = true;
                }
                else if (args[i] == "-paxtimes")
                {
                    argblock.ShowPaxTimes = true;
                }
                else if (args[i] == "-rookie")
                {
                    argblock.ShowRookie = true;
                }
                else if (args[i] == "-classtimes")
                {
                    argblock.ShowClassTimes = true;
                }
                else if (args[i] == "-teams")
                {
                    argblock.ShowTeams = true;
                }
                else if (args[i] == "-conecounts")
                {
                    argblock.ShowConeCounts = true;
                }
                else if (args[i] == "-eventname")
                {
                    // do nothing
                }
                else if (args[i] == "-maxofficialruns")
                {
                    i += 1;
                    if (!int.TryParse(args[i], out argblock.MaxOfficialRuns))
                    {
                        argblock.MaxOfficialRuns = 999;
                    }
                }
                else
                {
                    Usage();
                }
            }
            // Do the scoring calcs ad get the results back
            err = ScoreCalcs.DoScore(argblock, out scores, out teamScores, out stats, out sortedClassList);

            if (!string.IsNullOrEmpty(err))
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
            htmlHeader = htmlHeader.Replace("%STYLE%", htmlStyle);

            string results;
            string pathStart;
            if (string.IsNullOrEmpty(argblock.Day2))
            {
                pathStart = argblock.EventFolder + "\\" + argblock.Day1;
            }
            else
            {
                pathStart = argblock.EventFolder + "\\" + argblock.Day2 + "__2-day";
            }
            if (argblock.ShowRawTimes)
            {
                results = htmlHeader + argblock.Title + RawTimes(argblock) + htmlFooter;
                string path = pathStart + "__htmlRAWScores.html";
                File.WriteAllText(path, results);
            }

            if (argblock.ShowPaxTimes)
            {
                results = htmlHeader + argblock.Title + PaxTimes(argblock) + htmlFooter;
                string path = pathStart + "__htmlPAXScores.html";
                File.WriteAllText(path, results);
            }

            if (argblock.ShowClassTimes)
            {
                results = htmlHeader + argblock.Title + ClassTimes(argblock) + htmlFooter;
                string path = pathStart + "__htmlClassScores.html";
                File.WriteAllText(path, results);
            }
            if (argblock.ShowRunTimes)
            {
                results = htmlHeader + argblock.Title + RunTimes(argblock) + htmlFooter;
                // Always show statistics
                results += Statistics(argblock);
                string path = pathStart + "__htmlRunTimes.html";
                File.WriteAllText(path, results);
            }
            if (argblock.ShowTeams)
            {
                results = htmlHeader + argblock.Title + TeamTimes(argblock) + htmlFooter;
                string path = pathStart + "__htmlTeamScores.html";
                File.WriteAllText(path, results);
            }
            if (argblock.ShowConeCounts)
            {
                results = htmlHeader + argblock.Title + ConeCounts(argblock) + htmlFooter;
                string path = pathStart + "__htmlConeCounts.html";
                File.WriteAllText(path, results);
            }

        }

        public static string RawTimes(ScoreArgs args)
        {
            string results = htmlTableHeader;
            results = results.Replace("%TABLENAME%", "Overall ranking by RAW time");
            results += @"<tr class=""hdr2""><td>Rank</td><td>Car#</td><td>Class</td><td>Driver</td><td>Car</td><td>Raw Time</td><td>Score</td></tr>";

            string rookieResults = htmlTableHeader;
            rookieResults = rookieResults.Replace("%TABLENAME%", "Rookie ranking by RAW time");
            rookieResults += @"<tr class=""hdr2""><td>Rank</td><td>Car#</td><td>Class</td><td>Driver</td><td>Car</td><td>Raw Time</td><td>Score</td></tr>";

            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            // Sort by raw time
            myList
                .Sort(delegate (
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> firstPair,
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> nextPair)
                {
                    return firstPair.Value.ScoreData.BestRAW.CompareTo(nextPair.Value.ScoreData.BestRAW);
                });

            foreach (KeyValuePair<string, ScoreCalcs.DriverScoreData> driver in myList)
            {
                string driverRAW = "";
                driverRAW = driver.Value.ScoreData.BestRAW.ToString("#0.000");
                if (driver.Value.ScoreData.BestRAW >= ScoreCalcs.DNFvalue)
                {
                    driverRAW = "DNS";
                }

                string driverName = driver.Value.FirstName + " " + driver.Value.LastName.Substring(0, 1);
                if (showLastName)
                {
                    driverName = driver.Value.FirstName + " " + driver.Value.LastName;
                }
                string line = string
                    .Format("<tr class=data><td>{0,4}</td><td>{1,4}</td><td>{2,3}</td><td>{3,3}</td><td>{4,5}</td><td>{5,8:#.000}</td><td>{6,7:#0.000}</td>\r\n",
                        driver.Value.ScoreData.RAWrank,
                        driver.Value.Number,
                        driver.Value.CarClass,
                        driverName,
                        driver.Value.CarDescription,
                        driverRAW,
                        driver.Value.ScoreData.RAWscore);

                results += string.Format(line);
                if (driver.Value.Rookie)
                {
                    line = string
                        .Format("<tr class=data><td>{0,4}</td><td>{1,4}</td><td>{2,3}</td><td>{3,3}</td><td>{4,5}</td><td>{5,8:#.000}</td><td>{6,7:#0.000}</td>\r\n",
                            driver.Value.ScoreData.RAWrookieRank,
                            driver.Value.Number,
                            driver.Value.CarClass,
                            driverName,
                            driver.Value.CarDescription,
                            driverRAW,
                            driver.Value.ScoreData.RAWrookieScore);

                    rookieResults += line;
                }
            }
            results += htmlTableFooter;
            rookieResults += htmlTableFooter;
            if (args.ShowRookie)
            {
                return results + rookieResults;
            }
            else
            {
                return results;
            }
        }

        /// <summary>
        /// Return a printable text string for PAX data
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string PaxTimes(ScoreArgs args)
        {
            string results = htmlTableHeader;
            results = results.Replace("%TABLENAME%", "Overall ranking by PAX time");
            results += @"<tr class=""hdr2""><td>Rank</td><td>Car#</td><td>Class</td><td>Driver</td><td>Car</td><td>Raw Time</td><td>PAX #</td><td>PAX Time</td><td>Score</td></tr>";

            string rookieResults = htmlTableHeader;
            rookieResults = rookieResults.Replace("%TABLENAME%", "Rookie ranking by PAX time");
            rookieResults += @"<tr class=""hdr2""><td>Rank</td><td>Car#</td><td>Class</td><td>Driver</td><td>Car</td><td>Raw Time</td><td>PAX #</td><td>PAX Time</td><td>Score</td></tr>";

            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            // Sort by pax time
            myList
                .Sort(delegate (
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> firstPair,
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> nextPair)
                {
                    return firstPair.Value.ScoreData.BestPAX.CompareTo(nextPair.Value.ScoreData.BestPAX);
                });

            foreach (KeyValuePair<string, ScoreCalcs.DriverScoreData> driver in myList)
            {
                string driverName = driver.Value.FirstName + " " + driver.Value.LastName.Substring(0, 1);
                if (showLastName)
                {
                    driverName = driver.Value.FirstName + " " + driver.Value.LastName;
                }
                results += string
                    .Format("<tr class=data><td>{0,4}</td><td>{1,4}</td><td>{2,3}</td><td>{3,3}</td><td>{4,5}</td><td>{5,5:#.000}</td><td>{6,5:#.000}</td><td> {7,5:#.000}</td><td>{8,5:#0.000}</td>\r\n",
                        driver.Value.ScoreData.PAXrank,
                        driver.Value.Number,
                        driver.Value.CarClass,
                        driverName,
                        driver.Value.CarDescription,
                        driver.Value.ScoreData.BestRAW < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestRAW.ToString("#.000") : "DNS",
                        driver.Value.Pax,
                        driver.Value.ScoreData.BestPAX < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestPAX.ToString("#.000") : "DNS",
                        driver.Value.ScoreData.PAXscore);

                if (driver.Value.Rookie)
                {
                    rookieResults += string
                        .Format("<tr class=data><td>{0,4}</td><td>{1,4}</td><td>{2,3}</td><td>{3,3}</td><td>{4,5}</td><td>{5,5:#.000}</td><td>{6,5:#.000}</td><td>{7,5:#.000}</td><td>{8,5:#0.000}</td>\r\n",
                            driver.Value.ScoreData.PAXRookieRank,
                            driver.Value.Number,
                            driver.Value.CarClass,
                            driverName,
                            driver.Value.CarDescription,
                            driver.Value.ScoreData.BestRAW < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestRAW.ToString("#.000") : "DNS",
                            driver.Value.Pax,
                            driver.Value.ScoreData.BestPAX < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestPAX.ToString("#.000") : "DNS",
                            driver.Value.ScoreData.PAXrookieScore);
                }
            }
            if (args.ShowRookie)
            {
                return results + rookieResults;
            }
            else
            {
                return results;
            }
        }

        public static int GetLeadingInt(string input)
        {
            var i = 0;
            input = input.Trim();
            while (i < input.Length && char.IsDigit(input[i])) i++;

            input = input.Substring(0, i);
            if (!int.TryParse(input, out int value))
            {
                return 0;
            }
            return value;
        }

        /// <summary>
        /// print out run data
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string RunTimes(ScoreArgs args)
        {
            string results = htmlTableHeader;
            results = results.Replace("%TABLENAME%", "Run times (ordered by car number)");
            results += @"<tr class=""hdr2""><td>Car #</td><td>Member</td><td>Rookie</td><td>Class</td><td>Driver</td><td>Car</td><td>Sponsor</td></tr>";

            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            // Sort by car number
            myList
                .Sort(delegate (
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> firstPair,
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> nextPair)
                {
                    int d1Number, d2Number;
                    d1Number = GetLeadingInt(firstPair.Value.Number);
                    d2Number = GetLeadingInt(nextPair.Value.Number);
                    return d1Number.CompareTo(d2Number);
                });

            // First we need to cycle through all our drivers and decide how many sets there were on each day
            // We need to do this because a single driver may have skipped a day or a set
            int numDays = 1;
            int day1NumSets = 1;    // min # of sets in a day
            int day2NumSets = 0;
            if (!string.IsNullOrEmpty(args.Day2))
            {
                numDays = 2;
                day2NumSets = 1;    // min # of sets in a day
            }
            foreach (KeyValuePair<string, ScoreCalcs.DriverScoreData> driver in myList)
            {
                if ((driver.Value.Day1Set2.Runs.Count > 0) && (day1NumSets < 2))
                {
                    day1NumSets = 2;
                }
                if ((driver.Value.Day1Set3.Runs.Count > 0) && (day1NumSets < 3))
                {
                    day1NumSets = 3;
                }
                if ((numDays == 2) && (driver.Value.Day2Set2.Runs.Count > 0) && (day2NumSets < 2))
                {
                    day2NumSets = 2;
                }
                if ((numDays == 2) && (driver.Value.Day2Set3.Runs.Count > 0) && (day2NumSets < 3))
                {
                    day2NumSets = 3;
                }
            }

            foreach (KeyValuePair<string, ScoreCalcs.DriverScoreData> driver in myList)
            {
                string driverName = driver.Value.FirstName + " " + driver.Value.LastName.Substring(0, 1);
                if (showLastName)
                {
                    driverName = driver.Value.FirstName + " " + driver.Value.LastName;
                }
                string line = string
                    .Format("<tr class=data><td>{0,4}</td><td>{1,4}</td><td>{2,3}</td><td>{3,3}</td><td>{4,5}</td><td>{5,5}</td><td>{6,5}</td></tr>\r\n",
                        driver.Value.Number,
                        driver.Value.Member,
                        driver.Value.Rookie ? "Yes" : "No",
                        driver.Value.CarClass,
                        driverName,
                        driver.Value.CarDescription,
                        driver.Value.Sponsor);

                results += string.Format(line);
                results += "<td colspan=\"1\"></td><td colspan=\"99\"><table width=\"100%\" cellpadding=\"2\" style=\"border-collapse: collapse\" border=\"1\" align=\"left\">";
                if (day1NumSets >= 1)
                {
                    results += PrintSet(args, driver.Value.Day1Set1, 1, 1);
                }
                if (day1NumSets >= 2)
                {
                    results += PrintSet(args, driver.Value.Day1Set2, 1, 2);
                }
                if (day1NumSets >= 3)
                {
                    results += PrintSet(args, driver.Value.Day1Set3, 1, 3);
                }
                if (day2NumSets >= 1)
                {
                    results += PrintSet(args, driver.Value.Day2Set1, 2, 1);
                }
                if (day2NumSets >= 2)
                {
                    results += PrintSet(args, driver.Value.Day2Set2, 2, 2);
                }
                if (day2NumSets >= 3)
                {
                    results += PrintSet(args, driver.Value.Day2Set3, 2, 3);
                }


                if (args.Set1Only)
                {
                    results += string.Format("<tr><td class=\"hdr3\">{0,35}</td>", "Total (set1 only):");
                }
                else if (args.Set2Only)
                {
                    results += string.Format("<tr><td class=\"hdr3\">{0,35}</td>", "Total (set2 only):");
                }
                else if (args.BestSingleRun)
                {
                    results += string.Format("<tr><td class=\"hdr3\">{0,35}</td>", "Total (single best run):");
                }
                else
                {
                    // two day scoring
                    results += string.Format("<tr><td class=\"hdr3\">{0,35}</td>", "Total (both sets):");
                }


                if (driver.Value.ScoreData.BestRAW >= ScoreCalcs.DNFvalue)
                {
                    results += string.Format("<td class=\"highlight\">{0,8}</td>\r\n", "DNS");
                }
                else
                {
                    results += string.Format("<td class=\"highlight\">{0,8:#.000}</td>\r\n", driver.Value.ScoreData.BestRAW);
                }
                results += "</table>";
            }
            results += "</table>";
            return results;
        }

        public static string PrintSet(ScoreArgs args, ScoreCalcs.SingleSetData setData, int dayNumber, int setNumber)
        {
            string results = string.Format("<tr><td class=\"hdr3\">Day {0,1} Set {1,1}</td>", dayNumber, setNumber);
            bool hasRuns = false;
            int startRunNumber = -1;
            foreach (var run in setData.Runs)
            {
                hasRuns = true;
                if (startRunNumber < 0)
                {
                    // Note that run numbers don't start at 1 -- they increment across sets
                    startRunNumber = run.RunNumber;
                }
                if ((run.RunNumber - startRunNumber) == args.MaxOfficialRuns)
                {
                    results += "<td class=\"data2\">Fun--></td>";
                }
                if (run.Penalty == "DNF")
                {
                    if (run.Time >= ScoreCalcs.DNFvalue)
                    {
                        // didn't even run
                        results += string.Format("<td class=\"data2\">{0,8:#.000}</td>", "DNS");
                    }
                    else
                    {
                        results += string.Format("<td class=\"data2\">{0,8:#.000}{1,-4}</td>", run.Time, "+DNF");
                    }
                }
                else if (run.Penalty == "")
                {
                    if (run == setData.BestRun)
                    {
                        results += string.Format("<td class=\"highlight\">{0,8:#.000}</td>", run.Time);
                    }
                    else
                    {
                        results += string.Format("<td class=\"data2\">{0,8:#.000}{1,-4}</td>", run.Time, "    ");
                    }
                }
                else
                {
                    if (run == setData.BestRun)
                    {
                        results += string.Format("<td class=\"data2\">{0,8: (#.000}{1,-4}</td>", run.Time, "+" + run.Penalty + ")");
                    }
                    else
                    {
                        results += string.Format("<td class=\"data2\">{0,8:#.000}{1,-4}</td>", run.Time, "+" + run.Penalty);
                    }
                }
            }
            if (!hasRuns)
            {
                results += string.Format("<td class=\"data2\">{0,8:#.000}{1,-4}</td>", "DNS   ", "    ");
            }
            results += string.Format("\r\n");
            return results;
        }

        /// <summary>
        /// print out times, groups by Group, ordered within group by PAX time
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string ClassTimes(ScoreArgs args)
        {
            string results = htmlTableHeader;
            results = results.Replace("%TABLENAME%", "Overall ranking by Group");

            string rookieResults = htmlTableHeader;
            rookieResults = rookieResults.Replace("%TABLENAME%", "Rookie ranking by Group");

            // Sort drivers by PAX time
            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            myList
                .Sort(delegate (
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> firstPair,
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> nextPair)
                {
                    return firstPair.Value.ScoreData.BestPAX.CompareTo(nextPair.Value.ScoreData.BestPAX);
                });

            foreach (KeyValuePair<int, ScoreCalcs.PaxInfo> classInfo in sortedClassList)
            {
                var curClass = classInfo.Value;
                string curClassGroup = curClass.Group;

                double bestTime = 0.0;

                foreach (KeyValuePair<string, ScoreCalcs.DriverScoreData> driver in myList)
                {
                    string origXgrps = driver.Value.CarXGroup;
                    origXgrps += ";" + driver.Value.CarGroup;

                    string[] xgroups = origXgrps.Split(';');
                    bool driverIsInXgroup = false;

                    foreach (string xg in xgroups)
                    {
                        if (xg == curClass.CarClass)
                        {
                            driverIsInXgroup = true;
                            break;
                        }
                    }
                    if (!driverIsInXgroup)
                    {
                        continue;
                    }
                    ScoreCalcs.Groupscore grpPtr = null;
                    foreach (ScoreCalcs.Groupscore grp in driver.Value.ScoreData.GroupScores)
                    {
                        if (grp.GroupName == curClass.CarClass)
                        {
                            grpPtr = grp;
                            break;
                        }
                    }
                    if (grpPtr == null)
                    {
                        continue;
                    }

                    string line = "";


                    line += string.Format("<tr class=\"hdr3\" nowrap align=\"center\"><td colspan=\"999\">" + curClass.CarClass + " (" + curClass.Description + ")</td></tr>\r\n");
                    line += @"<tr class=""hdr2""><td>Rank</td><td>Car#</td><td>Class</td><td>Driver</td><td>Car</td><td>Raw Time</td><td>PAX #</td><td>PAX time</td><td>Score</td>";

                    line += string.Format("</tr>\r\n");

                    if (grpPtr.GroupRank == 1)
                    {
                        results += line;
                    }
                    if ((grpPtr.GroupRookieRank == 1) && (driver.Value.Rookie))
                    {
                        rookieResults += line;
                    }

                    if (driver.Value.LastName == "")
                    {
                        driver.Value.LastName = "Unknown";
                    }
                    if (grpPtr.GroupRank == 1)
                    {
                        bestTime = driver.Value.ScoreData.BestPAX;
                    }

                    string driverRAW;
                    driverRAW = driver.Value.ScoreData.BestRAW.ToString("#0.000");
                    if (driver.Value.ScoreData.BestRAW >= ScoreCalcs.DNFvalue)
                    {
                        driverRAW = "DNS";
                    }

                    string driverPAX;
                    driverPAX = driver.Value.ScoreData.BestPAX.ToString("#0.000");

                    if (driver.Value.ScoreData.BestRAW >= ScoreCalcs.DNFvalue)
                    {
                        driverPAX = "DNS";
                    }

                    string trophyIndicator = "T";
                    string rookieTrophyIndicator = "T";
                    if (grpPtr.GroupTrophy)
                    {
                        trophyIndicator = "T";
                    }
                    else
                    {
                        trophyIndicator = "";
                    }

                    if (grpPtr.GroupRookieTrophy)
                    {
                        rookieTrophyIndicator = "T";
                    }
                    else
                    {
                        rookieTrophyIndicator = "";
                    }

                    string driverName = driver.Value.FirstName + " " + driver.Value.LastName.Substring(0, 1);
                    if (showLastName)
                    {
                        driverName = driver.Value.FirstName + " " + driver.Value.LastName;
                    }
                    results += string
                        .Format("<tr class=data><td>{0,4}</td><td>{1,4}</td><td>{2,3}</td><td>{3,3}</td><td>{4,5}</td><td>{5,8:#.000}</td><td>{6,9:#.000}</td><td>{7,9:#.000}</td><td>{8,7:#0.000}</td>\r\n",
                            trophyIndicator + grpPtr.GroupRank,
                            driver.Value.Number,
                            driver.Value.CarClass,
                            driverName,
                            driver.Value.CarDescription,
                            driverRAW,
                            driver.Value.Pax,
                            driverPAX,
                            grpPtr.GroupScore);

                    results += string.Format("\r\n");

                    if (driver.Value.Rookie)
                    {
                        rookieResults += string
                            .Format("<tr class=data><td>{0,4}</td><td>{1,4}</td><td>{2,3}</td><td>{3,3}</td><td>{4,5}</td><td>{5,8:#.000}</td><td>{6,9:#.000}</td><td>{7,9:#.000}</td><td>{8,7:#0.000}</td>\r\n",
                                rookieTrophyIndicator + grpPtr.GroupRookieRank,
                                driver.Value.Number,
                                driver.Value.CarClass,
                                driverName,
                                driver.Value.CarDescription,
                                driverRAW,
                                driver.Value.Pax,
                                driverPAX,
                                grpPtr.GroupRookieScore);

                        rookieResults += string.Format("\r\n");
                    }
                }
            }
            if (args.ShowRookie)
            {
                return results + rookieResults;
            }
            else
            {
                return results;
            }
        }

        /// <summary>
        /// Return a printable text string for cone counts
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string ConeCounts(ScoreArgs args)
        {
            string results = htmlTableHeader;
            results = results.Replace("%TABLENAME%", "Cone counts");
            if (args == null)
            {
                return "";
            }
            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            // Sort by cone counts
            myList
                .Sort(delegate (
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> firstPair,
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> nextPair)
                {
                    return nextPair.Value.ConeCount.CompareTo(firstPair.Value.ConeCount);
                });

            results += @"<tr class=""hdr2""><td>Rank</td><td>Car#</td><td>Driver</td><td>Cones</td></tr>";

            int rank = 0;
            int lastCount = 9999;
            foreach (KeyValuePair<string, ScoreCalcs.DriverScoreData> driver in myList)
            {
                int tcones = 0;
                tcones = driver.Value.ConeCount;

                if (tcones <= 0)
                {
                    break;
                }
                if (tcones < lastCount)
                {
                    rank += 1;
                }
                string driverName = driver.Value.FirstName + " " + driver.Value.LastName.Substring(0, 1);
                if (showLastName)
                {
                    driverName = driver.Value.FirstName + " " + driver.Value.LastName;
                }
                results += string
                    .Format("<tr class=data><td>{0,4}</td><td>{1,4}</td><td>{2,35}</td><td>{3,4}</td></tr>\r\n",
                        rank,
                        driver.Value.Number,
                        driverName,
                        tcones.ToString());

                lastCount = tcones;
            }
            return results;
        }

        /// <summary>
        /// Team scores
        /// We group RAW and PAX if given in the driver's group filed
        /// If the group is something else, then we ignore the group and show the team for both raw and pax
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string TeamTimes(ScoreArgs args)
        {
            if (args == null)
            {
                return "";
            }
            string RAWresults = htmlTableHeader;
            RAWresults = RAWresults.Replace("%TABLENAME%", "RAW team scores");

            string PAXresults = htmlTableHeader;
            PAXresults = PAXresults.Replace("%TABLENAME%", "PAX team scores");

            // We sort our teams by raw time
            teamScores
                .Sort(delegate (
                    ScoreCalcs.TeamData first,
                    ScoreCalcs.TeamData next)
                {
                    // disable ConvertToLambdaExpression
                    return first.RawTotal.CompareTo(next.RawTotal);
                });

            // now go through the sorted list and print out raw team results
            RAWresults += @"<tr class=""hdr2""><td>Rank</td><td>Team</td><td>Total RAW</td><td>Total PAX</td><td>Cones</td></tr>";
            int rank = 1;
            foreach (ScoreCalcs.TeamData tm in teamScores)
            {
                if (tm.TeamType.StartsWith("PAX"))
                {
                    continue;
                }

                RAWresults += string
                    .Format("\r\n<tr class=data><td class=hdr2>{0,4}</td><td>{1,6}</td><td>RAW:{2,8:#.000}</td><td>PAX:{3,8:#.000}</td><td>Cones: {4}</td></tr>\r\n",
                        rank.ToString(),
                        tm.Team,
                        TimeOrDNS(tm.RawTotal),
                        TimeOrDNS(tm.PaxTotal),
                        tm.ConeTotal);

                RAWresults += "<td colspan=\"1\"></td><td colspan=\"99\"><table width=\"100%\" cellpadding=\"2\" style=\"border-collapse: collapse\" border=\"1\" align=\"left\">";
                rank += 1;
                foreach (ScoreCalcs.DriverScoreData driver in tm.TeamDrivers)
                {
                    string driverName = driver.FirstName + " " + driver.LastName.Substring(0, 1);
                    if (showLastName)
                    {
                        driverName = driver.FirstName + " " + driver.LastName;
                    }
                    RAWresults += string
                        .Format("<tr class=data2><td>Driver {0,3}</td><td>{1,-14}</td><td>RAW:{2, 8:#.000}</td><td>PAX:{3, 8:#.000}</td></tr>\r\n",
                            driver.Number,
                            driverName,
                            TimeOrDNS(driver.ScoreData.BestRAW),
                            TimeOrDNS(driver.ScoreData.BestPAX));
                }
                RAWresults += "</table>";
            }
            RAWresults += "</table>";
            // Do the same thing for PAX teams
            // Now we sort our teams by PAX time
            teamScores
                .Sort(delegate (
                    ScoreCalcs.TeamData first,
                    ScoreCalcs.TeamData next)
                {
                    return first.PaxTotal.CompareTo(next.PaxTotal);
                });

            PAXresults += @"<tr class=""hdr2""><td>Rank</td><td>Team</td><td>Total RAW</td><td>Total PAX</td><td>Cones</td></tr>";

            rank = 1;
            foreach (ScoreCalcs.TeamData tm in teamScores)
            {
                if (tm.TeamType.StartsWith("RAW"))
                {
                    continue;
                }

                PAXresults += string
                    .Format("\r\n<tr class=data><td class=hdr2>{0,4}</td><td>{1,6}</td><td>RAW:{2,8:#.000}</td><td>PAX:{3,8:#.000}</td><td>Cones: {4}</td></tr>\r\n",
                        rank.ToString(),
                        tm.Team,
                        TimeOrDNS(tm.RawTotal),
                        TimeOrDNS(tm.PaxTotal),
                        tm.ConeTotal);

                PAXresults += "<td colspan=\"1\"></td><td colspan=\"99\"><table width=\"100%\" cellpadding=\"2\" style=\"border-collapse: collapse\" border=\"1\" align=\"left\">";
                rank += 1;
                foreach (ScoreCalcs.DriverScoreData driver in tm.TeamDrivers)
                {
                    string driverName = driver.FirstName + " " + driver.LastName.Substring(0, 1);
                    if (showLastName)
                    {
                        driverName = driver.FirstName + " " + driver.LastName;
                    }
                    PAXresults += string
                        .Format("<tr class=data2><td>Driver {0,3}</td><td>{1,-14}</td><td>RAW:{2, 8:#.000}</td><td>PAX:{3, 8:#.000}</td></tr>\r\n",
                            driver.Number,
                            driverName,
                            TimeOrDNS(driver.ScoreData.BestRAW),
                            TimeOrDNS(driver.ScoreData.BestPAX));
                }
                PAXresults += "</table>";
            }
            PAXresults += "</table>";
            return RAWresults + PAXresults;
        }

        public static string Statistics(ScoreArgs args)
        {
            string results = htmlTableHeader;
            results = results.Replace("%TABLENAME%", "Statistics");
            if (stats.Day1.Set1NumberOfRuns > 0)
            {
                results += string
                    .Format("<tr class=data><td>Day1, Set1: </td><td>First run: {0} </td><td>Last run: {1} </td><td>Number of runs: {2} </td><td>Avg run time: {3,5:#0.00}</td>\r\n",
                        stats.Day1.Set1TimeOfFirstRun,
                        stats.Day1.Set1TimeOfLastRun,
                        stats.Day1.Set1NumberOfRuns,
                        stats.Day1.Set1TotalTime / stats.Day1.Set1NumberOfRuns);
            }
            if (stats.Day1.Set2NumberOfRuns > 0)
            {
                results += string
                    .Format("<tr class=data><td>Day1, Set2: </td><td>First run: {0} </td><td>Last run: {1} </td><td>Number of runs: {2} </td><td>Avg run time: {3,5:#0.00}</td>\r\n",
                        stats.Day1.Set2TimeOfFirstRun,
                        stats.Day1.Set2TimeOfLastRun,
                        stats.Day1.Set2NumberOfRuns,
                        stats.Day1.Set2TotalTime / stats.Day1.Set2NumberOfRuns);
            }
            if (stats.Day1.Set3NumberOfRuns > 0)
            {
                results += string
                    .Format("<tr class=data><td>Day1, Fun runs: </td><td>First run: {0} </td><td>Last run: {1} </td><td>Number of runs: {2} </td><td>Avg run time: {3,5:#0.00}</td>\r\n",
                        stats.Day1.Set3TimeOfFirstRun,
                        stats.Day1.Set3TimeOfLastRun,
                        stats.Day1.Set3NumberOfRuns,
                        stats.Day1.Set3TotalTime / stats.Day1.Set3NumberOfRuns);
            }
            // Day 2 if we have one
            if (stats.Day2.Set1NumberOfRuns > 0)
            {
                results += string
                    .Format("<tr class=data><td>Day2, Set1: </td><td>First run: {0} </td><td>Last run: {1} </td><td>Number of runs: {2} </td><td>Avg run time: {3,5:#0.00}</td>\r\n",
                        stats.Day2.Set1TimeOfFirstRun,
                        stats.Day2.Set1TimeOfLastRun,
                        stats.Day2.Set1NumberOfRuns,
                        stats.Day2.Set1TotalTime / stats.Day2.Set1NumberOfRuns);
            }
            if (stats.Day2.Set2NumberOfRuns > 0)
            {
                results += string
                    .Format("<tr class=data><td>Day2, Set2: </td><td>First run: {0} </td><td>Last run: {1} </td><td>Number of runs: {2} </td><td>Avg run time: {3,5:#0.00}</td>\r\n",
                        stats.Day2.Set2TimeOfFirstRun,
                        stats.Day2.Set2TimeOfLastRun,
                        stats.Day2.Set2NumberOfRuns,
                        stats.Day2.Set2TotalTime / stats.Day2.Set2NumberOfRuns);
            }
            if (stats.Day2.Set3NumberOfRuns > 0)
            {
                results += string
                    .Format("<tr class=data><td>Day2, Fun runs: </td><td>First run: {0} </td><td>Last run: {1} </td><td>Number of runs: {2} </td><td>Avg run time: {3,5:#0.00}</td>\r\n",
                        stats.Day2.Set3TimeOfFirstRun,
                        stats.Day2.Set3TimeOfLastRun,
                        stats.Day2.Set3NumberOfRuns,
                        stats.Day2.Set3TotalTime / stats.Day2.Set3NumberOfRuns);
            }
            return results;
        }

        /// <summary>
        /// Times > DNFvalue denote a DNS
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string TimeOrDNS(double time)
        {
            if (time >= ScoreCalcs.DNFvalue)
            {
                // didn't even run
                return "DNS   ";
            }
            else
            {
                return string.Format("{0,8:#.000}", time);
            }
        }
    }
}