/*
 * Generates score output in text format
 */
// disable CompareOfFloatsByEqualityOperator
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RaceBeam
{
    public static class TextScores
    {
        // returned list of all driver's score data
        public static Dictionary<string, ScoreCalcs.DriverScoreData> scores;
        // Returned list of team scores
        public static List<ScoreCalcs.TeamData> teamScores;
        // Returned list of all class data, sorted by sort order given in file
        public static SortedDictionary<int, ScoreCalcs.PaxInfo> sortedClassList;
        // Returned statistics
        public static ScoreCalcs.StatsDataClass stats;
        // The arguments to tell us what to show
        public static ScoreArgs args;

        public static CSVData configData = new CSVData();
        public static bool showLastName = false;

        private const string separator = "\r\n--------------------------------------------------------------------------------\r\n";
        public static string TextScore(ScoreArgs myArgs)
        {
            args = myArgs;
            string configFolder = args.ConfigFolder;
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
            err = ScoreCalcs.DoScore(args, out scores, out teamScores, out stats, out sortedClassList);

            if (!string.IsNullOrEmpty(err))
            {
                return err;
            }
            string results = "";

            if (args.ShowRunTimes)
            {
                results += separator + RunTimes(args);
            }
            if (args.ShowRawTimes)
            {
                results += separator + RawTimes(args);
            }
            if (args.ShowPaxTimes)
            {
                results += separator + PaxTimes(args);
            }
            if (args.ShowClassTimes)
            {
                results += separator + ClassTimes(args);
            }
            if (args.ShowTeams)
            {
                results += separator + Teams(args);
            }
            if (args.ShowConeCounts)
            {
                results += separator;
                results += ConeCounts(args);
            }
            results += separator;
            if (stats.Day1.Set1TimeOfFirstRun != "")
            {
                results += string.Format("Set1: First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                                         stats.Day1.Set1TimeOfFirstRun, stats.Day1.Set1TimeOfLastRun, stats.Day1.Set1NumberOfRuns, stats.Day1.Set1TotalTime / stats.Day1.Set1NumberOfRuns);
            }
            if (stats.Day1.Set2TimeOfFirstRun != "")
            {
                results += string.Format("Set2: First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                                         stats.Day1.Set2TimeOfFirstRun, stats.Day1.Set2TimeOfLastRun, stats.Day1.Set2NumberOfRuns, stats.Day1.Set2TotalTime / stats.Day1.Set2NumberOfRuns);
            }
            if (stats.Day1.Set3TimeOfFirstRun != "")
            {
                results += string.Format("Fun:  First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                                         stats.Day1.Set3TimeOfFirstRun, stats.Day1.Set3TimeOfLastRun, stats.Day1.Set3NumberOfRuns, stats.Day1.Set3TotalTime / stats.Day1.Set3NumberOfRuns);
            }
            // Day 2 if we have one
            if (stats.Day2.Set1TimeOfFirstRun != "")
            {
                results += string.Format("Set1: First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                                         stats.Day2.Set1TimeOfFirstRun, stats.Day2.Set1TimeOfLastRun, stats.Day2.Set1NumberOfRuns, stats.Day2.Set1TotalTime / stats.Day2.Set1NumberOfRuns);
            }
            if (stats.Day2.Set2TimeOfFirstRun != "")
            {
                results += string.Format("Set2: First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                                         stats.Day2.Set2TimeOfFirstRun, stats.Day2.Set2TimeOfLastRun, stats.Day2.Set2NumberOfRuns, stats.Day2.Set2TotalTime / stats.Day2.Set2NumberOfRuns);
            }
            if (stats.Day2.Set3TimeOfFirstRun != "")
            {
                results += string.Format("Fun:  First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                                         stats.Day2.Set3TimeOfFirstRun, stats.Day2.Set3TimeOfLastRun, stats.Day2.Set3NumberOfRuns, stats.Day2.Set3TotalTime / stats.Day2.Set3NumberOfRuns);
            }
            return results;
        }

        public static string TextScoreSplit(
            ScoreArgs myArgs,
            out string runtimes,
            out string rawtimes,
            out string paxtimes,
            out string classtimes,
            out string teamtimes,
            out string cones,
            out string statistics)
        {
            args = myArgs;

            runtimes = "";
            rawtimes = "";
            paxtimes = "";
            classtimes = "";
            teamtimes = "";
            cones = "";
            statistics = "";

            string configFolder = args.ConfigFolder;
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
            err = ScoreCalcs
                .DoScore(
                    args,
                    out scores,
                    out teamScores,
                    out stats,
                    out sortedClassList);

            if (!string.IsNullOrEmpty(err))
            {
                return err;
            }

            if (args.ShowRunTimes)
            {
                runtimes = RunTimes(args);
            }
            if (args.ShowRawTimes)
            {
                rawtimes = RawTimes(args);
            }
            if (args.ShowPaxTimes)
            {
                paxtimes = PaxTimes(args);
            }
            if (args.ShowClassTimes)
            {
                classtimes = ClassTimes(args);
            }
            if (args.ShowTeams)
            {
                teamtimes = Teams(args);
            }
            if (args.ShowConeCounts)
            {
                cones = ConeCounts(args);
            }

            if (stats.Day1.Set1TimeOfFirstRun != "")
            {
                statistics += string
                    .Format("Set1: First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                        stats.Day1.Set1TimeOfFirstRun,
                        stats.Day1.Set1TimeOfLastRun,
                        stats.Day1.Set1NumberOfRuns,
                        stats.Day1.Set1TotalTime / stats.Day1.Set1NumberOfRuns);
            }
            if (stats.Day1.Set2TimeOfFirstRun != "")
            {
                statistics += string
                    .Format("Set2: First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                        stats.Day1.Set2TimeOfFirstRun,
                        stats.Day1.Set2TimeOfLastRun,
                        stats.Day1.Set2NumberOfRuns,
                        stats.Day1.Set2TotalTime / stats.Day1.Set2NumberOfRuns);
            }
            if (stats.Day1.Set3TimeOfFirstRun != "")
            {
                statistics += string
                    .Format("Fun:  First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                        stats.Day1.Set3TimeOfFirstRun,
                        stats.Day1.Set3TimeOfLastRun,
                        stats.Day1.Set3NumberOfRuns,
                        stats.Day1.Set3TotalTime / stats.Day1.Set3NumberOfRuns);
            }
            // Day 2 if we have one
            if (stats.Day2.Set1TimeOfFirstRun != "")
            {
                statistics += string
                    .Format("Set1: First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                        stats.Day2.Set1TimeOfFirstRun,
                        stats.Day2.Set1TimeOfLastRun,
                        stats.Day2.Set1NumberOfRuns,
                        stats.Day2.Set1TotalTime / stats.Day2.Set1NumberOfRuns);
            }
            if (stats.Day2.Set2TimeOfFirstRun != "")
            {
                statistics += string
                    .Format("Set2: First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                        stats.Day2.Set2TimeOfFirstRun,
                        stats.Day2.Set2TimeOfLastRun,
                        stats.Day2.Set2NumberOfRuns,
                        stats.Day2.Set2TotalTime / stats.Day2.Set2NumberOfRuns);
            }
            if (stats.Day2.Set3TimeOfFirstRun != "")
            {
                statistics += string
                    .Format("Fun:  First run: {0} Last run: {1} Number of runs: {2} Avg run time: {3,5:#0.00}\r\n",
                        stats.Day2.Set3TimeOfFirstRun,
                        stats.Day2.Set3TimeOfLastRun,
                        stats.Day2.Set3NumberOfRuns,
                        stats.Day2.Set3TotalTime / stats.Day2.Set3NumberOfRuns);
            }
            return "";
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

        public static string RawTimes(ScoreArgs args)
        {
            string results = "";
            string rookieResults = separator;
            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            // Sort by raw time
            myList
                .Sort(delegate (
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> firstPair,
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> nextPair)
                {
                    return firstPair.Value.ScoreData.BestRAW.CompareTo(nextPair.Value.ScoreData.BestRAW);
                });

            rookieResults += string.Format("Rookie ranking by raw time:\r\n");
            results += string.Format("Overall ranking by raw time:\r\n");
            string hdr = string
                .Format("{0,4} {1,8} {2,11} {3,-16} {4,-22} {5,8} {6,7}\r\n",
                    "Rank",
                    "Car#",
                    "Class",
                    "Driver",
                    "Car",
                    "Raw Time",
                    "Score");
            results += hdr;
            rookieResults += hdr;

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
                    if (driverName.Length > 16)
                    {
                        driverName = driverName.Substring(0, 16);
                    }
                }
                string line = string
                    .Format("{0,4} {1,8} {2,11} {3,-16} {4,-22} {5,8:#.000} {6,7:#0.000}\r\n",
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
                        .Format("{0,4} {1,8} {2,11} {3,-16} {4,-22} {5,8:#.000} {6,7:#0.000}\r\n",
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
            string results = "";
            string rookieResults = separator;

            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            // Sort by pax time
            myList
                .Sort(delegate (
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> firstPair,
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> nextPair)
                {
                    return firstPair.Value.ScoreData.BestPAX.CompareTo(nextPair.Value.ScoreData.BestPAX);
                });

            results += string.Format("Overall ranking by PAX:\r\n");
            rookieResults += string.Format("Rookie ranking by PAX:\r\n");
            string hdr = string
                .Format("{0,4} {1,8} {2,11} {3,-16} {4,-22} {5,8}  {6,5} {7,9} {8,7} {9,8} {10,8}\r\n",
                    "Rank",
                    "Car#",
                    "Class",
                    "Driver",
                    "Car",
                    "Raw Time",
                    "PAX #",
                    "PAX Time",
                    "Score",
                    "RAWtoNext",
                    "RAWtoFirst");

            results += hdr;
            rookieResults += hdr;

            foreach (KeyValuePair<string, ScoreCalcs.DriverScoreData> driver in myList)
            {
                string driverName = driver.Value.FirstName + " " + driver.Value.LastName.Substring(0, 1);
                if (showLastName)
                {
                    driverName = driver.Value.FirstName + " " + driver.Value.LastName;
                    if (driverName.Length > 16)
                    {
                        driverName = driverName.Substring(0, 16);
                    }
                }
                results += string
                    .Format("{0,4} {1,8} {2,11} {3,-16} {4,-22} {5,8:#.000}  {6,5:#0.000} {7,9:#.000} {8,7:#0.000} {9,8:#.000} {10,11:#.000}\r\n",
                        driver.Value.ScoreData.PAXrank,
                        driver.Value.Number,
                        driver.Value.CarClass,
                        driverName,
                        driver.Value.CarDescription,
                        driver.Value.ScoreData.BestRAW < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestRAW.ToString("#.000") : "DNS",
                        driver.Value.Pax,
                        driver.Value.ScoreData.BestPAX < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestPAX.ToString("#.000") : "DNS",
                        driver.Value.ScoreData.PAXscore,
                        driver.Value.ScoreData.RAWtoNext,
                        driver.Value.ScoreData.RAWtoFirst);

                if (driver.Value.Rookie)
                {
                    rookieResults += string
                        .Format("{0,4} {1,8} {2,11} {3,-16} {4,-22} {5,8:#.000}  {6,5:#0.000} {7,9:#.000} {8,7:#0.000} {9,8:#.000} {10,11:#.000}\r\n",
                            driver.Value.ScoreData.PAXRookieRank,
                            driver.Value.Number,
                            driver.Value.CarClass,
                            driverName,
                            driver.Value.CarDescription,
                            driver.Value.ScoreData.BestRAW < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestRAW.ToString("#.000") : "DNS",
                            driver.Value.Pax,
                            driver.Value.ScoreData.BestPAX < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestPAX.ToString("#.000") : "DNS",
                            driver.Value.ScoreData.PAXrookieScore,
                            driver.Value.ScoreData.RookieRAWtoNext,
                            driver.Value.ScoreData.RookieRAWtoFirst);
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
        /// print out times, groups by Group, ordered within group by PAX time
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string ClassTimes(ScoreArgs args)
        {
            string results = "";
            string rookieResults = separator;

            // Sort drivers by PAX time
            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            myList
                .Sort(delegate (
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> firstPair,
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> nextPair)
                {
                    return firstPair.Value.ScoreData.BestPAX.CompareTo(nextPair.Value.ScoreData.BestPAX);
                });

            rookieResults += string.Format("Rookie ranking by Group:\r\n");
            results += string.Format("Overall ranking by Group:\r\n");

            foreach (KeyValuePair<int, ScoreCalcs.PaxInfo> classInfo in sortedClassList)
            {
                var curClass = classInfo.Value;
                string curClassGroup = curClass.Group;

                double bestTime = 0.0;

                foreach (KeyValuePair<string, ScoreCalcs.DriverScoreData> driver in myList)
                {
                    bool driverIsInXgroup = false;
                    foreach (ScoreCalcs.Groupscore grp in driver.Value.ScoreData.GroupScores)
                    {
                        if (grp.GroupName == curClass.CarClass)
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

                    line += string.Format("\r\nGroup: " + curClass.CarClass + " (" + curClass.Description + ")\r\n");

                    line += string
                        .Format("{0,4} {1,8} {2,11} {3,-16} {4,-22} {5,8}  {6,5} {7,9} {8,6}",
                            "Rank",
                            "Car#",
                            "Class",
                            "Driver",
                            "Car",
                            "Raw Time",
                            "PAX #",
                            "PAX Time",
                            "Score");

                    line += string.Format("\r\n");

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
                    if (driver.Value.CarDescription.Length > 20)
                    {
                        driver.Value.CarDescription = driver.Value.CarDescription.Substring(0, 20);
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
                        trophyIndicator = "T";
                    else
                        trophyIndicator = "";
                    if (grpPtr.GroupRookieTrophy)
                        rookieTrophyIndicator = "T";
                    else
                        rookieTrophyIndicator = "";
                    string driverName = driver.Value.FirstName + " " + driver.Value.LastName.Substring(0, 1);
                    if (showLastName)
                    {
                        driverName = driver.Value.FirstName + " " + driver.Value.LastName;
                        if (driverName.Length > 16)
                        {
                            driverName = driverName.Substring(0, 16);
                        }
                    }
                    results += string
                        .Format("{0,-1}{1,3} {2,8} {3,11} {4,-16} {5,-22} {6,8:#.000} {7,6:#.000} {8,9:#0.000} {9,8:#0.000} ",
                            trophyIndicator,
                            grpPtr.GroupRank,
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
                            .Format("{0,-1}{1,3} {2,8} {3,11} {4,-16} {5,-22} {6,8:#.000} {7,6:#.000} {8,9:#0.000} {9,8:#0.000} ",
                                rookieTrophyIndicator,
                                grpPtr.GroupRookieRank,
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
        /// Team scores
        /// We group RAW and PAX if given in the driver's group filed
        /// If the group is something else, then we ignore the group and show the team for both raw and pax
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Teams(ScoreArgs args)
        {
            string results = "";
            results += "RAW Team scores\r\n";

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
            results = "RAW Team scores\r\n\r\n";
            results += string.Format("{0,4}", "Rank");
            int rank = 1;
            foreach (ScoreCalcs.TeamData tm in teamScores)
            {
                if (tm.TeamType.StartsWith("PAX"))
                {
                    continue;
                }

                results += string
                    .Format("\r\n{0,4} Team:{1,6} Total: RAW:{2,8:#.000} PAX:{3,8:#.000} Cones: {4}\r\n",
                        rank.ToString(),
                        tm.Team,
                        TimeOrDNS(tm.RawTotal),
                        TimeOrDNS(tm.PaxTotal),
                        tm.ConeTotal);

                rank += 1;
                foreach (ScoreCalcs.DriverScoreData driver in tm.TeamDrivers)
                {
                    string driverName = driver.FirstName + " " + driver.LastName.Substring(0, 1);
                    if (showLastName)
                    {
                        driverName = driver.FirstName + " " + driver.LastName;
                        if (driverName.Length > 16)
                        {
                            driverName = driverName.Substring(0, 16);
                        }
                    }
                    results += string
                        .Format("\tDriver {0,3}: {1,-16}  RAW:{2, 8:#.000}  PAX:{3, 8:#.000}\r\n",
                            driver.Number,
                            driverName,
                            TimeOrDNS(driver.ScoreData.BestRAW),
                            TimeOrDNS(driver.ScoreData.BestPAX));
                }
            }

            // Do the same thing for PAX teams
            // Now we sort our teams by PAX time
            teamScores
                .Sort(delegate (
                    ScoreCalcs.TeamData first,
                    ScoreCalcs.TeamData next)
                {
                    return first.PaxTotal.CompareTo(next.PaxTotal);
                });

            results += separator + "PAX Team scores\r\n\r\n";
            results += string.Format("{0,4}", "Rank");
            rank = 1;
            foreach (ScoreCalcs.TeamData tm in teamScores)
            {
                if (tm.TeamType.StartsWith("RAW"))
                {
                    continue;
                }

                results += string
                    .Format("\r\n{0,4} Team:{1,6} Total:  RAW:{2, 8:#.000} PAX:{3, 8:#.000} Cones: {4}\r\n",
                        rank.ToString(),
                        tm.Team,
                        TimeOrDNS(tm.RawTotal),
                        TimeOrDNS(tm.PaxTotal),
                        tm.ConeTotal);

                rank += 1;
                foreach (ScoreCalcs.DriverScoreData driver in tm.TeamDrivers)
                {
                    string driverName = driver.FirstName + " " + driver.LastName.Substring(0, 1);
                    if (showLastName)
                    {
                        driverName = driver.FirstName + " " + driver.LastName;
                        if (driverName.Length > 16)
                        {
                            driverName = driverName.Substring(0, 16);
                        }
                    }
                    results += string
                        .Format("\tDriver {0,3}: {1,-16}  RAW:{2, 8:#.000}  PAX:{3, 8:#.000}\r\n",
                            driver.Number,
                            driverName,
                            TimeOrDNS(driver.ScoreData.BestRAW),
                            TimeOrDNS(driver.ScoreData.BestPAX));
                }
            }
            return results;
        }

        /// <summary>
        /// Return a printable text string for cone counts
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string ConeCounts(ScoreArgs args)
        {
            string results = "";
            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            // Sort by cone counts
            myList
                .Sort(delegate (
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> firstPair,
                    KeyValuePair<string, ScoreCalcs.DriverScoreData> nextPair)
                {
                    return nextPair.Value.ConeCount.CompareTo(firstPair.Value.ConeCount);
                });

            results += string.Format("Cone counts:\r\n");

            results += string
                .Format("{0,4} {1,8} {2,-16} {3,4}\r\n",
                    "Rank",
                    "Car#",
                    "Driver",
                    "Cones");

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
                    if (driverName.Length > 16)
                    {
                        driverName = driverName.Substring(0, 16);
                    }
                }
                results += string
                    .Format("{0,4} {1,8} {2,-16} {3,4}\r\n",
                        rank,
                        driver.Value.Number,
                        driverName,
                        tcones.ToString());

                lastCount = tcones;
            }
            return results;
        }

        public static string PrintSet(ScoreCalcs.SingleSetData setData, int dayNumber, int setNumber)
        {
            string results = string.Format("{0,19}Day {1,1} Set {2,1}", " ", dayNumber, setNumber);
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
                    results += "Fun-->";
                }
                if (run.Penalty == "DNF")
                {
                    if (run.Time >= ScoreCalcs.DNFvalue)
                    {
                        // didn't even run
                        results += string.Format("{0,8:#.000}{1,-4}", "DNS   ", "    ");
                    }
                    else
                    {
                        results += string.Format("{0,8:#.000}{1,-4}", run.Time, "+DNF");
                    }
                }
                else if (run.Penalty == "")
                {
                    if (run == setData.BestRun)
                    {
                        results += string.Format("{0,8: (#.000}{1,-4}", run.Time, ")  ");
                    }
                    else
                    {
                        results += string.Format("{0,8:#.000}{1,-4}", run.Time, "    ");
                    }
                }
                else
                {
                    if (run == setData.BestRun)
                    {
                        results += string.Format("{0,8: (#.000}{1,-4}", run.Time, "+" + run.Penalty + ")");
                    }
                    else
                    {
                        results += string.Format("{0,8:#.000}{1,-4}", run.Time, "+" + run.Penalty);
                    }
                }
            }
            if (!hasRuns)
            {
                results += string.Format("{0,8:#.000}{1,-4}", "DNS   ", "    ");
            }
            results += string.Format("\r\n");
            return results;
        }

        public static int GetLeadingInt(string input)
        {
            int i = 0;
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
            string results = "";
            var myList = new List<KeyValuePair<string, ScoreCalcs.DriverScoreData>>(scores);
            // Sort by car number
            // TODO sort properly, even if number has trailing letters (delegate in mjcommon?)
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

            results += "Run data (ordered by car number):\r\n";
            // see http://blog.stevex.net/string-formatting-in-csharp/ for a C# formatting info
            results += string
                .Format("{0,8} {1,3} {2,3} {3,15} {4,-16} {5,-22} {6,-22}\r\n",
                    "Car#",
                    "Mbr",
                    "Rky",
                    "Class",
                    "Driver",
                    "Car",
                    "Sponsor");
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
                    if (driverName.Length > 16)
                    {
                        driverName = driverName.Substring(0, 16);
                    }
                }
                string line = string
                    .Format("{0,8} {1,3} {2,3} {3,15} {4,-16} {5,-22} {6,-22}\r\n",
                        driver.Value.Number,
                        driver.Value.Member,
                        driver.Value.Rookie ? "Yes" : "No",
                        driver.Value.CarClass,
                        driverName,
                        driver.Value.CarDescription,
                        driver.Value.Sponsor);

                results += string.Format(line);
                if (day1NumSets >= 1)
                {
                    results += PrintSet(driver.Value.Day1Set1, 1, 1);
                }
                if (day1NumSets >= 2)
                {
                    results += PrintSet(driver.Value.Day1Set2, 1, 2);
                }
                if (day1NumSets >= 3)
                {
                    results += PrintSet(driver.Value.Day1Set3, 1, 3);
                }
                if (day2NumSets >= 1)
                {
                    results += PrintSet(driver.Value.Day2Set1, 2, 1);
                }
                if (day2NumSets >= 2)
                {
                    results += PrintSet(driver.Value.Day2Set2, 2, 2);
                }
                if (day2NumSets >= 3)
                {
                    results += PrintSet(driver.Value.Day2Set3, 2, 3);
                }

                if (args.Set1Only)
                {
                    results += string.Format("{0,35}", "Total (set1 only):");
                }
                else if (args.Set2Only)
                {
                    results += string.Format("{0,35}", "Total (set2 only):");
                }
                else if (args.BestSingleRun)
                {
                    results += string.Format("{0,35}", "Total (single best run):");
                }
                else
                {
                    results += string.Format("{0,35}", "Total (both sets):");
                }


                if (driver.Value.ScoreData.BestRAW >= ScoreCalcs.DNFvalue)
                {
                    results += string.Format("{0,8}\r\n", "DNS");
                }
                else
                {
                    results += string.Format("{0,8:#.000}\r\n", driver.Value.ScoreData.BestRAW);
                }
            }
            return results;
        }
    }
}