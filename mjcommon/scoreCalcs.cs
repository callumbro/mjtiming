/*
 * 
 * Created by Murray Peterson
 * 
3.5.1. Results Format
All results for National events shall meet the following requirements:
Results should be structured in category (Stock, Super Stock, Street Prepared, Modified), with Classes listed alphabetically in each Category;
Class winners shall be listed in order of fastest to slowest.
All times are to be displayed as the time plus the number of pylons, and the corrected time (e.g. –competitor A: 65.25 sec + 2 pylons = 69.25 sec);
Ladies Classes will be listed separately from each regular class;
Competitors with sponsors shall have their sponsors names listed alongside the competitors name in the results under a heading of driver sponsors;
A separate heading shall be used to list the top 10 competitors overall;
A separate listing of all competitors w/ indexed times showing the Overall National Champion rankings.
An indication of the total number of competitors at the event.

3.5.2. Final Results -Presentation Format
All final results for National events shall meet the following requirements and shall be sent to ASN Canada FIA office, all territories, stewards, members of the ASN National SoloSport Committee, sponsors, etc.:
i) Details concerning the event (name of event, name of organizing club, date of event, status of event, permit number);
ii) Acknowledgement of sponsors, stewards, organizers, etc;

3.5.3. ASN Canada FIA Canadian AutoSlalom Championship Awards
1 trophy for 1 to 3 competitors in a class;
2 trophies for 4 to 6 competitors;
3 trophies for 7 to 9 competitors;
1 additional trophy for every four additional competitors.
Basically, 3,3,3,4,4,4,4,.
 */
// disable CompareOfFloatsByEqualityOperator
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RaceBeam
{
    public class ScoreArgs
    {
        public string ConfigFolder;     // If blank, we use <current dir>\..\config
        public string EventFolder;      // If blank, we use config-->eventFolder
        public string ClassFile;        // If blank, we use config-->classFile
        public string Day1;
        public string Day2;
        public int MaxOfficialRuns;
        public bool Set1Only;
        public bool Set2Only;
        public bool Set1PlusSet2;
        public bool BestSingleRun;
        public bool ShowRunTimes;
        public bool ShowRawTimes;
        public bool ShowPaxTimes;
        public bool ShowClassTimes;
        public bool ShowConeCounts;
        public bool ShowTeams;
        public bool ShowRookie;         // Applies to raw/pax/class
        public bool WriteCSV;
        public string Title;
        public string OutFile;
        public ScoreArgs()
        {
            EventFolder = "";
            ConfigFolder = "";
            ClassFile = "";
            Day1 = "";
            Day2 = "";
            MaxOfficialRuns = 99;
            Set1Only = false;
            Set2Only = false;
            Set1PlusSet2 = false;
            BestSingleRun = false;
            ShowRunTimes = false;
            ShowRawTimes = false;
            ShowPaxTimes = false;
            ShowClassTimes = false;
            ShowConeCounts = false;
            ShowTeams = false;
            ShowRookie = false;
            WriteCSV = false;
            Title = "";
            OutFile = string.Empty;
        }
        // copy constructor
        public ScoreArgs(ScoreArgs src)
        {
            Day1 = src.Day1;
            Day2 = src.Day2;
            MaxOfficialRuns = src.MaxOfficialRuns;
            Set1Only = src.Set1Only;
            Set2Only = src.Set2Only;
            Set1PlusSet2 = src.Set1PlusSet2;
            BestSingleRun = src.BestSingleRun;
            ShowRunTimes = src.ShowRunTimes;
            ShowRawTimes = src.ShowRawTimes;
            ShowPaxTimes = src.ShowPaxTimes;
            ShowClassTimes = src.ShowClassTimes;
            ShowConeCounts = src.ShowConeCounts;
            ShowTeams = src.ShowTeams;
            ShowRookie = src.ShowRookie;
            WriteCSV = src.WriteCSV;
            EventFolder = src.EventFolder;
            Title = src.Title;
        }
    }


    /// <summary>
    /// Perform all of the scoring functions and return a string which can be printed out to a text file or displayed in a window.
    /// </summary>
    public static class ScoreCalcs
    {
        #region Public Static Members

        public static double DNFvalue = 9999.0;

        #endregion

        #region Public Classes

        // All the info about a single run
        public class SingleRunData
        {
            public int RunNumber;
            public double Time;
            public string Penalty;
            public double AdjustedRawTime;      // 1000 for DNF
            public double PAXtime;          // 1000 for DNF
            public int ConeCount;
            public string StartTime;
            public string StopTime;
        }
        // All the run data for a single set
        public class SingleSetData
        {
            public double BestRAW = DNFvalue;
            public double BestPAX = DNFvalue;
            public double BestSUM = DNFvalue;
            public int Cones = 0;
            public SingleRunData BestRun;
            public List<SingleRunData> Runs = new List<SingleRunData>();
        }
        public class Groupscore
        {
            public string GroupName;
            public int GroupRank;
            public int GroupRookieRank;
            public double GroupScore;
            public double GroupRookieScore;
            public bool GroupTrophy;
            public bool GroupRookieTrophy;
        }
        // Calculated score data for one type of score (PAX, RAW, class, etc)
        public class ScoreDataClass
        {
            public double BestFTD = DNFvalue;   // best raw time, any day, any set
            public double BestRAW;
            public double BestPAX;
            public double BestSUM;


            public double FTDpax;       // used for match scoring
            public int RAWrank;
            public int RAWrookieRank;
            public double RAWscore;
            public double RAWrookieScore;
            public bool RAWtrophy;
            public bool RAWrookieTrophy;

            public int PAXrank;
            public int PAXRookieRank;
            public double PAXscore;
            public double PAXrookieScore;
            public bool PAXtrophy;
            public bool PAXrookieTrophy;
            public double RAWtoNext;
            public double RAWtoFirst;
            public double RookieRAWtoNext;
            public double RookieRAWtoFirst;

            // For Group and XGroup (multiple groups here, with ones inXgroup separated by a ';')
            public List<Groupscore> GroupScores = new List<Groupscore>();
        }

        // Everything we know and calculate about a single driver
        public class DriverScoreData
        {
            public string Number;
            public string FirstName;
            public string LastName;
            public string CarDescription;
            public string Member;
            public bool Rookie;
            public string CarGroup;         // Primary group
            public string CarGroupDescription;
            public string CarXGroup;            // Xtra group
            public string CarXGroupDescription;
            public int MaxOfficialRuns; // we use this to limit max runs that count for this driver
            public string CarClass;
            public string Team;
            public double Pax;
            public string Sponsor;
            public PaxInfo ClassInfo;

            public int ConeCount = 0;       // Total for all sets, all days

            public string Day1Set1Info = "";    // Run info packed into a string
            public string Day1Set2Info = "";
            public string Day1Set3Info = "";
            public string Day2Set1Info = "";
            public string Day2Set2Info = "";
            public string Day2Set3Info = "";

            public SingleSetData Day1Set1 = new SingleSetData();
            public SingleSetData Day1Set2 = new SingleSetData();
            public SingleSetData Day1Set3 = new SingleSetData();
            public SingleSetData Day2Set1 = new SingleSetData();
            public SingleSetData Day2Set2 = new SingleSetData();
            public SingleSetData Day2Set3 = new SingleSetData();
            public ScoreDataClass ScoreData = new ScoreDataClass();
        }

        public class PaxInfo
        {
            public string CarClass;
            public string Pax;
            public string Description;
            public string Group;
            public string DisplayOrder;
        }
        public class TeamData
        {
            public string Team;
            public string TeamType;
            public double RawTotal;
            public int RawRank;
            public double RawScore;
            public double PaxTotal;
            public int PaxRank;
            public double PaxScore;
            public int ConeTotal;
            public int DriversTotalled;
            public List<DriverScoreData> TeamDrivers;
            public TeamData()
            {
                Team = "";
                TeamType = "";
                RawTotal = 0.0;
                RawScore = 0.0;
                PaxTotal = 0.0;
                PaxScore = 0.0;
                ConeTotal = 0;
                DriversTotalled = 0;
                TeamDrivers = new List<DriverScoreData>();
            }
        }
        public class DailyStats
        {
            public string Set1TimeOfFirstRun = "";
            public string Set1TimeOfLastRun = "";
            public int Set1NumberOfRuns = 0;
            public double Set1TotalTime = 0.0;
            public string Set2TimeOfFirstRun = "";
            public string Set2TimeOfLastRun = "";
            public int Set2NumberOfRuns = 0;
            public double Set2TotalTime = 0.0;
            public string Set3TimeOfFirstRun = "";
            public string Set3TimeOfLastRun = "";
            public int Set3NumberOfRuns = 0;
            public double Set3TotalTime = 0.0;
            public DailyStats()
            {
                Set1NumberOfRuns = 0;
                Set1TimeOfFirstRun = "";
                Set1TimeOfLastRun = "";
                Set1TotalTime = 0.0;
                Set2NumberOfRuns = 0;
                Set2TimeOfFirstRun = "";
                Set2TimeOfLastRun = "";
                Set2TotalTime = 0.0;
                Set3NumberOfRuns = 0;
                Set3TimeOfFirstRun = "";
                Set3TimeOfLastRun = "";
                Set3TotalTime = 0.0;
            }
        }
        public class StatsDataClass
        {
            public DailyStats Day1 = new DailyStats();
            public DailyStats Day2 = new DailyStats();
        }

        #endregion

        #region Private Members

        private static CSVData driverData = new CSVData();
        private static CSVData timingDataDay1 = new CSVData();
        private static CSVData timingDataDay2 = new CSVData();
        private static CSVData classData = new CSVData();
        private static CSVData configData = new CSVData();

        private static Dictionary<string, DriverScoreData> scores = new Dictionary<string, DriverScoreData>();
        private static SortedDictionary<int, PaxInfo> sortedClassList = null;
        private static StatsDataClass stats = new StatsDataClass();

        private static string driverDataFile = "";
        private static string classDataFile = "";
        private static string timingFileName = "";
        private static bool ConesGetPAXed = false;   // Do we apply pax to time+cones?
                                                     // ---------------------------------------------------------------------------
                                                     // calculate raw scores

        #endregion

        #region Public Static Methods

        public static string DoScore(
            ScoreArgs args,
            out Dictionary<string, DriverScoreData> driverScoreData,
            out List<TeamData> teamScoreData,
            out StatsDataClass statistics,
            out SortedDictionary<int, PaxInfo> sortedClassDict)
        {
            driverScoreData = null; // assume things go bad
            teamScoreData = null;
            statistics = null;
            sortedClassDict = null;

            driverData = new CSVData();
            timingDataDay1 = new CSVData();
            timingDataDay2 = new CSVData();
            classData = new CSVData();
            configData = new CSVData();
            scores = new Dictionary<string, DriverScoreData>();
            driverDataFile = "";
            classDataFile = "";
            timingFileName = "";
            string configFolder = args.ConfigFolder;
            string configFilename = "";
            if (string.IsNullOrEmpty(configFolder))
            {
                configFolder = Process.GetCurrentProcess().MainModule.FileName;
                configFolder = Path.GetDirectoryName(configFolder);
                configFilename = configFolder + "\\..\\config\\configData.csv";
            }
            else
            {
                configFilename = configFolder + "\\configData.csv";
            }
            string err = configData.LoadData(configFilename, ',', "Parameter");
            if (err != "")
            {
                return ("Unable to load config file: " + err);
            }
            if (configData.GetField("ConesGetPAXed", "Value") == "Yes")
            {
                ConesGetPAXed = true;
            }

            driverDataFile = configData.GetField("driverDataFile", "Value");
            if (driverDataFile == "")
            {
                return ("Driver data file not defined in config file");
            }

            classDataFile = configData.GetField("classDataFile", "Value");
            if (classDataFile == "")
            {
                return ("Class data file not defined in config file");
            }
            if (!string.IsNullOrEmpty(args.ClassFile))
            {
                classDataFile = args.ClassFile;
            }

            string eventFolder = configData.GetField("eventDataFolder", "Value");
            if (eventFolder == "")
            {
                return ("Timing data folder not defined in config file");
            }
            if (!string.IsNullOrEmpty(args.EventFolder))
            {
                eventFolder = args.EventFolder;
            }
            timingFileName = eventFolder + "\\" + args.Day1 + "_timingData.csv";

            // Read in timing data
            err = timingDataDay1.LoadData(timingFileName, ',', "index");
            if (err != "")
            {
                return (timingFileName + ": " + err);
            }
            // If day 2 data given, then read it in too
            if (args.Day2 != "")
            {
                string filename = eventFolder + "\\" + args.Day2 + "_timingData.csv";
                err = timingDataDay2.LoadData(filename, ',', "index");
                if (err != "")
                {
                    return (filename + ": " + err);
                }
            }

            // Make a copy of the driverData file if we haven't already
            string eventDriverDataFile = eventFolder + "\\" + args.Day1 + "_driverData.csv";
            try
            {
                // Update copy of driver DataFile if we are working on today's data
                string today = DateTime.Now.ToString("yyyy_MM_dd");
                if ((configData.GetField("eventDataFolder", "Value") == eventFolder) && (today == args.Day1))
                {
                    File.Delete(eventDriverDataFile);
                }
                File.Copy(driverDataFile, eventDriverDataFile);
            }
            catch
            {
                // not interested in any errors
            }

            // Read in driverData file(s) for this event
            if (args.Day1 != "")
            {
                string filename = eventFolder + "\\" + args.Day1 + "_driverData.csv";
                err = driverData.LoadData(filename, ',', "Number");
                if (err != "")
                {
                    return (filename + ": " + err);
                }
            }

            // If day 2 data given, then read it in too
            if (args.Day2 != "")
            {
                string filename = eventFolder + "\\" + args.Day2 + "_driverData.csv";
                err = driverData.LoadData(filename, ',', "Number");
                if (err != "")
                {
                    return (filename + ": " + err);
                }
            }

            // Read in class data file
            err = classData.LoadData(classDataFile, ',', "Class");
            if (err != "")
            {
                return (err);
            }
            // First build a sorted class and group list
            // Sort order is given in the csv file
            if (sortedClassList == null)
            {
                sortedClassList = new SortedDictionary<int, PaxInfo>();

                List<string> classList = classData.GetKeys();
                foreach (string className in classList)
                {
                    var p = new PaxInfo();
                    p.CarClass = className;
                    p.Pax = classData.GetField(className, "PAX");
                    p.Description = classData.GetField(className, "Description");
                    p.Group = classData.GetField(className, "Group");
                    p.DisplayOrder = classData.GetField(className, "Display Order");
                    if (!int.TryParse(p.DisplayOrder, out int orderVal))
                    {
                        return string.Format("Display order value for class " + p.CarClass + " is not a valid integer\r\n");
                    }
                    else
                    {
                        try
                        {
                            sortedClassList.Add(orderVal, p);
                        }
                        catch
                        {
                            return string.Format("Multiple sort order entries for class " + p.CarClass + "\r\n");
                        }
                    }
                }
            }
            if (args.Day2 != "")
            {
                // Ignore day flags in each file
                ParseRunData(args, timingDataDay1, 1);
                ParseRunData(args, timingDataDay2, 2);
            }
            else
            {
                // Use day flags inside timing file
                ParseRunData(args, timingDataDay1, 1);
            }

            // Do some calcs and fixups on the run data
            CalcRunTimes(args);
            RawTimes(args);
            PaxTimes(args);


            List<TeamData> teamScores = TeamTimes(args);
            err = ClassTimes(args);
            if (err != "")
                return err;

            statistics = stats;
            driverScoreData = scores;
            teamScoreData = teamScores;
            sortedClassDict = sortedClassList;

            if (args.WriteCSV)
            {
                // Write out csv data
                string csvresults = DoCSV(args);
                string csvFileName;

                if (args.Day2 == "")
                {
                    csvFileName = eventFolder + "\\" + args.Day1 + "_CSVData.csv";
                }
                else
                {
                    csvFileName = eventFolder + "\\" + args.Day2 + "__2-day__CSVData.csv";
                }
                try
                {
                    TextWriter tw = new StreamWriter(csvFileName);
                    tw.WriteLine(csvresults);
                    tw.Close();
                }
                catch
                {

                }
            }
            return "";
        }

        public static void RawTimes(ScoreArgs args)
        {
            var myList = new List<KeyValuePair<string, DriverScoreData>>(scores);
            // Sort by raw time
            myList
                .Sort(delegate (
                    KeyValuePair<string, DriverScoreData> firstPair,
                    KeyValuePair<string, DriverScoreData> nextPair)
                {
                    return firstPair.Value.ScoreData.BestRAW.CompareTo(nextPair.Value.ScoreData.BestRAW);
                });

            int rank = 1;
            int rookieRank = 1;
            double bestTime = 0.0;
            double rookieBestTime = 0.0;
            foreach (KeyValuePair<string, DriverScoreData> driver in myList)
            {
                if (!driver.Value.Rookie)
                {
                    driver.Value.ScoreData.RAWrookieScore = 0.0;
                    driver.Value.ScoreData.RAWrookieRank = 0;
                }

                if (rank == 1)
                {
                    bestTime = driver.Value.ScoreData.BestRAW;
                    driver.Value.ScoreData.RAWscore = 100.00;
                    driver.Value.ScoreData.RAWrank = rank;
                }
                if ((rookieRank == 1) && driver.Value.Rookie)
                {
                    rookieBestTime = driver.Value.ScoreData.BestRAW;
                    driver.Value.ScoreData.RAWscore = 100.0;
                    driver.Value.ScoreData.RAWrank = rank;
                }

                if (rank != 1)  // include rookies in overall
                {
                    driver.Value.ScoreData.RAWscore = bestTime / driver.Value.ScoreData.BestRAW * 100.00;
                    driver.Value.ScoreData.RAWrank = rank;
                    if (driver.Value.Rookie)
                    {
                        driver.Value.ScoreData.RAWrookieScore = rookieBestTime / driver.Value.ScoreData.BestRAW * 100.00;
                        driver.Value.ScoreData.RAWrookieRank = rookieRank;
                    }
                    if (driver.Value.ScoreData.BestRAW >= DNFvalue)
                    {
                        driver.Value.ScoreData.RAWscore = 0.0;
                        driver.Value.ScoreData.RAWrookieScore = 0.0;
                    }
                }
                rank += 1;
                if (driver.Value.Rookie)
                {
                    rookieRank += 1;
                }
            }
        }

        /// <summary>
        /// Calculate PAX scores
        /// </summary>
        /// <param name="args"></param>
        public static void PaxTimes(ScoreArgs args)
        {
            int rank = 1;
            int rookieRank = 1;
            double bestTime = 0.0;
            double rookieBestTime = 0.0;
            double prevTime = 0.0;
            double prevRookieTime = 0.0;

            var myList = new List<KeyValuePair<string, DriverScoreData>>(scores);
            // Sort by pax time
            myList
                .Sort(delegate (
                    KeyValuePair<string, DriverScoreData> firstPair,
                    KeyValuePair<string, DriverScoreData> nextPair)
                {
                    return firstPair.Value.ScoreData.BestPAX.CompareTo(nextPair.Value.ScoreData.BestPAX);
                });

            foreach (KeyValuePair<string, DriverScoreData> driver in myList)
            {
                if (!driver.Value.Rookie)
                {
                    driver.Value.ScoreData.PAXrookieScore = 0.0;
                }
                if (rank == 1)
                {
                    bestTime = driver.Value.ScoreData.BestPAX;
                    prevTime = bestTime;
                    driver.Value.ScoreData.PAXscore = 100.00;
                    driver.Value.ScoreData.PAXrank = 1;
                    driver.Value.ScoreData.RAWtoFirst = 0.0;
                    driver.Value.ScoreData.RAWtoNext = 0.0;
                }
                if ((rookieRank == 1) && driver.Value.Rookie)
                {
                    rookieBestTime = driver.Value.ScoreData.BestPAX;
                    prevRookieTime = rookieBestTime;
                    driver.Value.ScoreData.PAXrookieScore = 100.0;
                    driver.Value.ScoreData.PAXRookieRank = 1;
                    driver.Value.ScoreData.RookieRAWtoFirst = 0.0;
                    driver.Value.ScoreData.RookieRAWtoNext = 0.0;
                }
                if (rank != 1)  // include rookies in overall
                {
                    driver.Value.ScoreData.PAXscore = bestTime / driver.Value.ScoreData.BestPAX * 100.00;
                    if (driver.Value.ScoreData.BestPAX >= DNFvalue)
                    {
                        driver.Value.ScoreData.PAXscore = 0.0;
                        //calc raw from pax difference
                        if (bestTime >= DNFvalue)
                        {
                            driver.Value.ScoreData.RAWtoFirst = 0.0;
                        }
                        else
                        {
                            driver.Value.ScoreData.RAWtoFirst = bestTime / driver.Value.Pax;
                        }
                        if (prevTime >= DNFvalue)
                        {
                            driver.Value.ScoreData.RAWtoNext = 0.0;
                        }
                        else
                        {
                            driver.Value.ScoreData.RAWtoNext = prevTime / driver.Value.Pax;
                        }
                    }
                    else
                    {
                        //calc raw from pax difference
                        driver.Value.ScoreData.RAWtoFirst = (driver.Value.ScoreData.BestPAX - bestTime) / driver.Value.Pax;
                        driver.Value.ScoreData.RAWtoNext = (driver.Value.ScoreData.BestPAX - prevTime) / driver.Value.Pax;
                    }
                    driver.Value.ScoreData.PAXrank = rank;
                    if (driver.Value.Rookie)
                    {
                        driver.Value.ScoreData.PAXrookieScore = rookieBestTime / driver.Value.ScoreData.BestPAX * 100.00;
                        if (driver.Value.ScoreData.BestPAX >= DNFvalue)
                        {
                            driver.Value.ScoreData.PAXrookieScore = 0.0;
                            if (rookieBestTime >= DNFvalue)
                            {
                                driver.Value.ScoreData.RookieRAWtoFirst = 0.0;
                            }
                            else
                            {
                                driver.Value.ScoreData.RookieRAWtoFirst = rookieBestTime / driver.Value.Pax;
                            }
                            if (prevRookieTime >= DNFvalue)
                            {
                                driver.Value.ScoreData.RookieRAWtoNext = 0.0;
                            }
                            else
                            {
                                driver.Value.ScoreData.RookieRAWtoNext = prevRookieTime / driver.Value.Pax;
                            }
                        }
                        else
                        {
                            //calc raw from pax difference
                            driver.Value.ScoreData.RookieRAWtoFirst = (driver.Value.ScoreData.BestPAX - rookieBestTime) / driver.Value.Pax;
                            driver.Value.ScoreData.RookieRAWtoNext = (driver.Value.ScoreData.BestPAX - prevRookieTime) / driver.Value.Pax;
                        }
                        driver.Value.ScoreData.PAXRookieRank = rookieRank;
                    }
                }
                if (driver.Value.ScoreData.BestPAX < DNFvalue)
                {
                    prevTime = driver.Value.ScoreData.BestPAX;
                }
                rank += 1;
                if (driver.Value.Rookie)
                {
                    rookieRank += 1;
                    if (driver.Value.ScoreData.BestPAX < DNFvalue)
                    {
                        prevRookieTime = driver.Value.ScoreData.BestPAX;
                    }
                }
            }
        }

        /// <summary>
        /// print out times ordered within group and Xgroup by PAX time
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string ClassTimes(ScoreArgs args)
        {

            // Sort drivers by PAX time
            var myList = new List<KeyValuePair<string, DriverScoreData>>(scores);
            myList
                .Sort(delegate (
                    KeyValuePair<string, DriverScoreData> firstPair,
                    KeyValuePair<string, DriverScoreData> nextPair)
                {
                    return firstPair.Value.ScoreData.BestPAX.CompareTo(nextPair.Value.ScoreData.BestPAX);
                });

            foreach (KeyValuePair<int, PaxInfo> classInfo in sortedClassList)
            {
                PaxInfo curClass = classInfo.Value;
                string curClassGroup = curClass.Group;
                int rank = 1;
                int rookieRank = 1;
                double bestTime = 0.0;
                double rookieBestTime = 0.0;
                int trophies = 0;
                int rookieTrophies = 0;
                int carsInClass = 0;
                int rookieCarsInClass = 0;

                // First go through and get a total count of cars in this class so we can allocate trophies
                foreach (KeyValuePair<string, DriverScoreData> driver in myList)
                {
                    string origXgrps = driver.Value.CarXGroup;
                    origXgrps += ";" + driver.Value.CarGroup;

                    string sval = configData.GetField("ClassAsGroup", "Value");
                    sval = sval.ToUpperInvariant();
                    if (sval.Contains("Y"))
                    {
                        origXgrps += ";" + driver.Value.CarClass;
                    }
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

                    if (driver.Value.Rookie)
                    {
                        rookieCarsInClass += 1;
                    }
                    carsInClass += 1;
                }
                trophies = TrophyCount(carsInClass);
                rookieTrophies = TrophyCount(rookieCarsInClass);


                foreach (KeyValuePair<string, DriverScoreData> driver in myList)
                {
                    string origXgrps = driver.Value.CarXGroup;
                    origXgrps += ";" + driver.Value.CarGroup;

                    string sval = configData.GetField("ClassAsGroup", "Value");
                    sval = sval.ToUpperInvariant();
                    if (sval.Contains("Y"))
                    {
                        origXgrps += ";" + driver.Value.CarClass;
                    }
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
                    // Clear out all the existing scores for this driver and group
                    for (int index = 0; index < driver.Value.ScoreData.GroupScores.Count; index++)
                    {
                        if (driver.Value.ScoreData.GroupScores[index].GroupName == curClass.CarClass)
                        {
                            driver.Value.ScoreData.GroupScores.RemoveAt(index);
                            break;
                        }
                    }
                    var grp = new Groupscore();
                    grp.GroupName = curClass.CarClass;
                    driver.Value.ScoreData.GroupScores.Add(grp);

                    if (rank == 1)
                    {
                        grp.GroupScore = 100.0;
                        grp.GroupRank = rank;
                        bestTime = driver.Value.ScoreData.BestPAX;
                    }
                    if ((rookieRank == 1) && driver.Value.Rookie)
                    {
                        grp.GroupRookieScore = 100.0;
                        grp.GroupRookieRank = rookieRank;
                        rookieBestTime = driver.Value.ScoreData.BestPAX;
                    }
                    if (rank != 1)
                    {
                        if (driver.Value.Rookie)
                        {
                            grp.GroupRookieScore = rookieBestTime / driver.Value.ScoreData.BestPAX * 100.00;
                            if (driver.Value.ScoreData.BestPAX >= DNFvalue)
                            {
                                grp.GroupRookieScore = 0.0;
                            }
                            grp.GroupRookieRank = rookieRank;
                        }
                        // rookies are included in the overall scores
                        grp.GroupScore = bestTime / driver.Value.ScoreData.BestPAX * 100.00;
                        if (driver.Value.ScoreData.BestPAX >= DNFvalue)
                        {
                            grp.GroupScore = 0.0;
                        }
                        grp.GroupRank = rank;
                    }

                    if (trophies-- <= 0)
                    {
                        grp.GroupTrophy = false;
                    }
                    else
                    {
                        grp.GroupTrophy = true;
                    }
                    rank += 1;
                    if (driver.Value.Rookie)
                    {
                        if (rookieTrophies-- <= 0)
                        {
                            grp.GroupRookieTrophy = false;
                        }
                        else
                        {
                            grp.GroupRookieTrophy = true;
                        }
                        rookieRank += 1;
                    }
                }
            }
            return "";
        }

        public static List<TeamData> TeamTimes(ScoreArgs args)
        {
            var teams = new List<TeamData>();

            // go through and collate into teams (based on team name)
            var myList = new List<KeyValuePair<string, DriverScoreData>>(scores);
            myList
                .Sort(delegate (
                    KeyValuePair<string, DriverScoreData> firstPair,
                    KeyValuePair<string, DriverScoreData> nextPair)
                {
                    string firstTeam = firstPair.Value.Team;
                    string secondTeam = nextPair.Value.Team;
                    if (secondTeam == null)
                        return 1;
                    if (firstTeam == null)
                        return -1;
                    return firstTeam.CompareTo(nextPair.Value.Team);
                });

            string sval = configData.GetField("teamNumScores", "Value");
            if (!int.TryParse(sval, out int teamNumScores))
            {
                teamNumScores = 4;
            }

            TeamData curTeam = null;
            foreach (KeyValuePair<string, DriverScoreData> driver in myList)
            {
                if (driver.Value.Team == null)
                {
                    continue;
                }
                if ((curTeam == null) || (driver.Value.Team != curTeam.Team))
                {
                    // Not the same team, so create a new one
                    curTeam = new TeamData();
                    curTeam.Team = driver.Value.Team;
                    curTeam.TeamType = driver.Value.CarGroup;   // Only works for RAW/PAX
                    teams.Add(curTeam);
                }
                // add driver to team
                curTeam.TeamDrivers.Add(driver.Value);

                // Everybody gets to add to the cone count
                curTeam.ConeTotal += driver.Value.ConeCount;
            }

            // Go through teams (twice) and assign a PAX score and a RAW score
            // Add all drivers and sort by raw time, then use best n
            foreach (var team in teams)
            {
                // first sort each driver by his by RAW time
                team
                    .TeamDrivers
                    .Sort(delegate (
                        DriverScoreData first,
                        DriverScoreData next)
                    {
                        var firstRaw = first.ScoreData.BestRAW;
                        var nextRaw = next.ScoreData.BestRAW;
                        return firstRaw.CompareTo(nextRaw);
                    });

                // Add driver's RAW totals to team score, but only the best <n> drivers are used
                team.DriversTotalled = 0;
                foreach (DriverScoreData driver in team.TeamDrivers)
                {
                    if (team.DriversTotalled < teamNumScores)
                    {
                        team.DriversTotalled += 1;
                        team.RawTotal += driver.ScoreData.BestRAW;
                    }
                }
                // now sort each driver by his by PAX time
                team
                    .TeamDrivers
                    .Sort(delegate (
                        DriverScoreData first,
                        DriverScoreData next)
                    {
                        var firstPax = first.ScoreData.BestPAX;
                        var nextPax = next.ScoreData.BestPAX;
                        return firstPax.CompareTo(nextPax);
                    });

                // Add driver's PAX totals to team score, but only the best <n> drivers are used
                team.DriversTotalled = 0;
                foreach (DriverScoreData driver in team.TeamDrivers)
                {
                    if (team.DriversTotalled < teamNumScores)
                    {
                        team.DriversTotalled += 1;
                        team.PaxTotal += driver.ScoreData.BestPAX;
                    }
                }
            }

            // First sort by RAW
            teams
                .Sort(delegate (
                    TeamData firstTeam,
                    TeamData nextTeam)
                {
                    return firstTeam.RawTotal.CompareTo(nextTeam.RawTotal);
                });

            double bestTime = 0;
            int rank = 1;
            foreach (var team in teams)
            {
                if (rank == 1)
                {
                    bestTime = team.RawTotal;
                    team.RawScore = 100.00;
                    team.RawRank = rank;
                }
                else
                {
                    team.RawScore = bestTime / team.RawTotal * 100.00;
                    team.RawRank = rank;
                }
                rank += 1;
            }
            // Now sort by PAX totals
            teams
                .Sort(delegate (
                    TeamData firstTeam,
                    TeamData nextTeam)
                {
                    return firstTeam.PaxTotal.CompareTo(nextTeam.PaxTotal);
                });

            rank = 1;
            foreach (var team in teams)
            {
                if (rank == 1)
                {
                    bestTime = team.PaxTotal;
                    team.PaxScore = 100.00;
                    team.PaxRank = rank;
                }
                else
                {
                    team.PaxScore = bestTime / team.PaxTotal * 100.00;
                    team.PaxRank = rank;
                }
                rank += 1;
            }
            return teams;
        }

        /// <summary>
        /// Return CSV format data -- won't work unless raw and pax calcs have been done previously
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string DoCSV(ScoreArgs args)
        {
            bool wasrookie = args.ShowRookie;
            args.ShowRookie = true; // force rookie calcs
            RawTimes(args);
            PaxTimes(args);
            ClassTimes(args);
            args.ShowRookie = wasrookie;
            ClassTimes(args);

            string results = "";
            var myList = new List<KeyValuePair<string, DriverScoreData>>(scores);
            // Sort by car number
            myList
                .Sort(delegate (
                    KeyValuePair<string, DriverScoreData> firstPair,
                    KeyValuePair<string, DriverScoreData> nextPair)
                {
                    int d1Number, d2Number;
                    d1Number = GetLeadingInt(firstPair.Value.Number);
                    d2Number = GetLeadingInt(nextPair.Value.Number);
                    return d1Number.CompareTo(d2Number);
                });

            // see http://blog.stevex.net/string-formatting-in-csharp/ for a C# formatting info

            // Go through the xgroup lists for each driver and create a new list containing all of the xgroup names
            // We also calc the max xgroups for any driver

            var maxXgroups = 0;
            foreach (KeyValuePair<string, DriverScoreData> driver in myList)
            {
                string origXgrps = driver.Value.CarXGroup;
                if (string.IsNullOrEmpty(origXgrps))
                {
                    origXgrps = driver.Value.CarGroup;
                }
                else
                {
                    origXgrps += ";" + driver.Value.CarGroup;
                }
                string sval = configData.GetField("ClassAsGroup", "Value");
                sval = sval.ToUpperInvariant();
                if (sval.Contains("Y"))
                {
                    if (string.IsNullOrEmpty(origXgrps))
                    {
                        origXgrps = driver.Value.CarClass;
                    }
                    else
                    {
                        origXgrps += ";" + driver.Value.CarClass;
                    }
                }
                string[] xgrps = origXgrps.Split(';');
                if (xgrps.Length > maxXgroups)
                {
                    maxXgroups = xgrps.Length;
                }
            }

            // We can do xgroup headers now that we know how many xgroups exist
            results += string.Format("Car#,Mbr,Rky,Group,XGroup,Class,Team,PAX,Driver,First Name,Last Name,Car,Sponsor,RAW time,PAX time,PAX Score,Rookie PAX Score,RAW Score,Rookie RAW Score,Cones,Day1Set1 Runs,Day1Set2 Runs,Day1Set3 Runs");
            for (int xgcount = 1; xgcount <= maxXgroups; xgcount++)
            {
                results += string.Format(",Xgroup-{0}:Name,Xgroup-{1}:Score,Xgroup-{2}:Rookie Score", xgcount, xgcount, xgcount);
            }
            results += "\r\n";

            foreach (KeyValuePair<string, DriverScoreData> driver in myList)
            {
                string driverName = driver.Value.FirstName + " " + driver.Value.LastName.Substring(0, 1);
                if (configData.GetField("ShowLastName", "Value").Contains("Y"))
                {
                    driverName = driver.Value.FirstName + " " + driver.Value.LastName;
                }
                string line = string
                    .Format("{0},{1},{2},{3},{4},{5},{6},{7,8:#.000},{8},{9},{10},{11},{12,8:#.000},{13,8:#.000},{14,8:#.000},{15,8:#.000},{16,8:#.000},{17,8:#.000},{18,8:#.000},{19},{20},{21},{22}",
                        driver.Value.Number,
                        driver.Value.Member,
                        driver.Value.Rookie ? "Y" : "N",
                        driver.Value.CarGroup,
                        driver.Value.CarXGroup,
                        driver.Value.CarClass,
                        driver.Value.Team,
                        driver.Value.Pax,
                        driverName,
                        driver.Value.FirstName,
                        driver.Value.LastName,
                        driver.Value.CarDescription,
                        driver.Value.Sponsor,
                        driver.Value.ScoreData.BestRAW,
                        driver.Value.ScoreData.BestPAX,
                        driver.Value.ScoreData.PAXscore,
                        driver.Value.ScoreData.PAXrookieScore,
                        driver.Value.ScoreData.RAWscore,
                        driver.Value.ScoreData.RAWrookieScore,
                        driver.Value.ConeCount.ToString(),
                        driver.Value.Day1Set1Info,
                        driver.Value.Day1Set2Info,
                        driver.Value.Day1Set3Info);

                // emit all the xgroup scores (we know we have enough xgroup headers to hold the list)
                foreach (var grp in driver.Value.ScoreData.GroupScores)
                {
                    line += string.Format(",{0},{1},{2}",
                                          grp.GroupName,
                                          grp.GroupScore,
                                          grp.GroupRookieScore
                                         );
                }
                line += "\r\n";
                results += string.Format(line);
            }
            return results;
        }

        /// <summary>
        /// Parse up the run data and get it into our dictionary
        /// Do a few calcs on the way
        /// </summary>
        /// <param name="args"></param>
        /// <param name="rdata"></param>
        /// <param name="dayNumber"></param>
        public static void ParseRunData(ScoreArgs args, CSVData rdata, int dayNumber)
        {

            // Refresh statistics
            stats = new StatsDataClass();

            for (int i = 0; i < rdata.Length(); i++)
            {
                DriverScoreData drv;
                var run = new SingleRunData();
                double fval;
                int ival;
                string sval;
                string indexS = i.ToString();

                // we key everything by car number
                string number = rdata.GetField(indexS, "car_number");
                if (string.IsNullOrEmpty(number))
                {
                    continue;
                }
                string penalty = rdata.GetField(indexS, "penalty");
                if (penalty == "RRN")
                {
                    continue;
                }
                if (scores.ContainsKey(number))
                {
                    drv = scores[number];
                }
                else
                {
                    drv = new DriverScoreData();
                }
                drv.Number = number;
                if (drv.FirstName == null)
                {
                    drv.FirstName = driverData.GetField(number, "First Name");
                    if (drv.FirstName == "")
                    {
                        drv.FirstName = "Unknown";
                        drv.LastName = "Driver";
                        drv.CarDescription = "Unknown car";
                        drv.Member = "No";
                        drv.Rookie = false;
                        drv.CarClass = "AM";
                        drv.CarGroup = "AM";
                        drv.CarXGroup = "";
                        drv.CarGroupDescription = "Unregistered";
                        drv.Pax = 1.0;
                        //Console.WriteLine("Missing data for car #" + number.ToString());
                    }
                }
                if (drv.LastName == null)
                {
                    drv.LastName = driverData.GetField(number, "Last Name");
                    if (drv.LastName == "")
                    {
                        drv.LastName = "Unknown";
                    }
                }
                if (drv.CarDescription == null)
                {
                    drv.CarDescription = driverData.GetField(number, "Car Model");
                }
                if (drv.Member == null)
                {
                    drv.Member = driverData.GetField(number, "Member");
                    if (drv.Member.ToUpperInvariant().Contains("Y"))
                    {
                        drv.Member = "Yes";
                    }
                    else
                    {
                        drv.Member = "No";
                    }
                }
                if (drv.Sponsor == null)
                {
                    drv.Sponsor = driverData.GetField(number, "Sponsor");
                }
                if (drv.Team == null)
                {
                    drv.Team = driverData.GetField(number, "Team");
                }

                if (drv.CarDescription.Length > 20)
                {
                    drv.CarDescription = drv.CarDescription.Substring(0, 20);
                }

                string rookie = driverData.GetField(number, "Rookie");
                if (rookie.ToUpperInvariant().Contains("Y"))
                {
                    drv.Rookie = true;
                }
                else
                {
                    drv.Rookie = false;
                }
                if (drv.CarClass == null)
                {
                    drv.CarClass = driverData.GetField(number, "Class");
                    string realClass = drv.CarClass;
                    sval = classData.GetField(realClass, "PAX");
                    if (double.TryParse(sval, out fval))
                    {
                        drv.Pax = fval;
                    }
                    else
                    {
                        drv.Pax = 1.0;
                    }
                }
                if (drv.CarGroup == null)
                {
                    drv.CarGroup = driverData.GetField(number, "Group");
                    if (string.IsNullOrEmpty(drv.CarGroup))
                    {
                        drv.CarGroup = drv.CarClass;
                    }
                    drv.CarGroupDescription = classData.GetField(drv.CarGroup, "Description");
                    // Use group PAX as a max official runs value (when it is > 2)
                    drv.MaxOfficialRuns = args.MaxOfficialRuns;
                    sval = classData.GetField(drv.CarGroup, "PAX");
                    if (int.TryParse(sval, out int intVal))
                    {
                        if (intVal > 2)
                        {
                            drv.MaxOfficialRuns = intVal;
                        }
                    }
                }
                if (drv.CarXGroup == null)
                {
                    drv.CarXGroup = driverData.GetField(number, "XGroup");
                    if (string.IsNullOrEmpty(drv.CarXGroup))
                    {
                        drv.CarXGroup = "";
                        drv.CarXGroupDescription = "";
                    }
                    else
                    {
                        drv.CarXGroupDescription = classData.GetField(drv.CarXGroup, "Description");
                        // Use group PAX as a max official runs value (when it is > 2)
                        // If both group and xgroup specify a value, use the lowest one
                        sval = classData.GetField(drv.CarXGroup, "PAX");
                        if (int.TryParse(sval, out int intVal))
                        {
                            if ((intVal > 2) && (intVal < drv.MaxOfficialRuns))
                            {
                                drv.MaxOfficialRuns = intVal;
                            }
                        }
                    }
                }
                string stopstr = rdata.GetField(indexS, "stop_time");
                string startstr = rdata.GetField(indexS, "start_time");
                double runt = 0.0;
                if ((stopstr != null) &&
                    double.TryParse(stopstr, out double stopint) &&
                    double.TryParse(startstr, out double startint))
                {
                    if (stopint > startint)
                    {
                        runt = unchecked(stopint - startint);
                    }
                    else
                    {
                        runt = unchecked(1000000 - startint + stopint);
                    }
                    runt = runt / 1000;
                }
                sval = rdata.GetField(indexS, "run_time");

                if (double.TryParse(sval, out fval))
                {
                    run.Time = fval;
                    if ((fval != runt) && (runt != 0.0))
                    {
                        run.Time = runt;
                    }
                    run.Penalty = rdata.GetField(indexS, "penalty");
                    run.AdjustedRawTime = AdjustTime(run.Time, run.Penalty, 1.0);
                    run.ConeCount = CalcCones(run.Penalty);
                    if (run.AdjustedRawTime == DNFvalue)
                    {
                        run.PAXtime = DNFvalue;
                    }
                    else
                    {
                        run.PAXtime = AdjustTime(run.Time, run.Penalty, drv.Pax);
                    }
                    sval = rdata.GetField(indexS, "run_number");
                    run.RunNumber = 0;
                    if (int.TryParse(sval, out ival))
                    {
                        run.RunNumber = ival;
                    }
                    // Handle old style "day" instead of "set"
                    string setFlag = rdata.GetField(indexS, "set");
                    if (setFlag == "")
                    {
                        setFlag = rdata.GetField(indexS, "day");
                    }
                    int set = 3;
                    if (int.TryParse(setFlag, out ival))
                    {
                        set = ival;
                    }
                    if (set > 3)
                    {
                        set = 3;    // Make it a fun run
                    }
                    run.StartTime = rdata.GetField(indexS, "datetime");
                    if (run.StartTime.Length > 3)
                    {
                        run.StartTime = run.StartTime.Substring(3);
                    }
                    run.StopTime = rdata.GetField(indexS, "datetime_stop");
                    if (run.StopTime.Length > 3)
                    {
                        run.StopTime = run.StopTime.Substring(3);
                    }
                    // update this driver's raw FTD
                    if (drv.ScoreData.BestFTD > run.AdjustedRawTime)
                    {
                        drv.ScoreData.BestFTD = run.AdjustedRawTime;
                    }
                    if (set == 1)
                    {
                        if (dayNumber == 1)
                        {
                            if (drv.Day1Set1.BestSUM == DNFvalue)
                                drv.Day1Set1.BestSUM = 0.0; // Actually exists, so reset from DNF

                            // some interesting stats
                            if (stats.Day1.Set1TimeOfFirstRun == "")
                            {
                                stats.Day1.Set1TimeOfFirstRun = run.StartTime;
                            }
                            stats.Day1.Set1TimeOfLastRun = run.StartTime;
                            stats.Day1.Set1NumberOfRuns += 1;
                            stats.Day1.Set1TotalTime += run.Time;
                            drv.Day1Set1Info += string.Format("{0,5:#.000}", run.Time);
                            if (!string.IsNullOrEmpty(run.Penalty))
                            {
                                drv.Day1Set1Info += "+" + run.Penalty;
                            }
                            drv.Day1Set1Info += ";";

                            drv.Day1Set1.Runs.Add(run); // Add this run to the driver's list

                            if ((drv.Day1Set1.Runs.Count <= drv.MaxOfficialRuns) && (run.AdjustedRawTime < drv.Day1Set1.BestRAW))
                            {
                                drv.Day1Set1.BestRAW = run.AdjustedRawTime;
                                drv.Day1Set1.BestRun = run;
                            }
                            if ((drv.Day1Set1.Runs.Count <= drv.MaxOfficialRuns) && (run.PAXtime < drv.Day1Set1.BestPAX))
                            {
                                drv.Day1Set1.BestPAX = run.PAXtime;
                            }

                            drv.ConeCount += run.ConeCount;
                            if (run.AdjustedRawTime >= DNFvalue)
                            {
                                string dnfSeconds = configData.GetField("secondsForDNF", "Value");
                                if (!int.TryParse(dnfSeconds, out int DNFpenalty))
                                {
                                    DNFpenalty = 200;
                                }
                                drv.Day1Set1.BestSUM += DNFpenalty; // We count a DNF as 200 seconds
                            }
                            else
                            {
                                drv.Day1Set1.BestSUM += run.AdjustedRawTime;
                            }
                        }
                        else    // day 2
                        {
                            if (drv.Day2Set1.BestSUM == DNFvalue)
                                drv.Day2Set1.BestSUM = 0.0; // Actually exists, so reset from DNF

                            // some interesting stats
                            if (stats.Day2.Set1TimeOfFirstRun == "")
                            {
                                stats.Day2.Set1TimeOfFirstRun = run.StartTime;
                            }
                            stats.Day2.Set1TimeOfLastRun = run.StartTime;
                            stats.Day2.Set1NumberOfRuns += 1;
                            stats.Day2.Set1TotalTime += run.Time;
                            drv.Day2Set1Info += string.Format("{0,5:#.000}", run.Time);
                            if (!string.IsNullOrEmpty(run.Penalty))
                            {
                                drv.Day2Set1Info += "+" + run.Penalty;
                            }
                            drv.Day2Set1Info += ";";

                            drv.Day2Set1.Runs.Add(run); // Add this run to the driver's list

                            if ((drv.Day2Set1.Runs.Count <= drv.MaxOfficialRuns) && (run.AdjustedRawTime < drv.Day2Set1.BestRAW))
                            {
                                drv.Day2Set1.BestRAW = run.AdjustedRawTime;
                                drv.Day2Set1.BestRun = run;
                            }
                            if ((drv.Day2Set1.Runs.Count <= drv.MaxOfficialRuns) && (run.PAXtime < drv.Day2Set1.BestPAX))
                            {
                                drv.Day2Set1.BestPAX = run.PAXtime;
                            }
                            drv.ConeCount += run.ConeCount;
                            if (run.AdjustedRawTime >= DNFvalue)
                            {
                                string dnfSeconds = configData.GetField("secondsForDNF", "Value");
                                if (!int.TryParse(dnfSeconds, out int DNFpenalty))
                                {
                                    DNFpenalty = 200;
                                }
                                drv.Day2Set1.BestSUM += DNFpenalty; // We count a DNF as 200 seconds
                            }
                            else
                            {
                                drv.Day2Set1.BestSUM += run.AdjustedRawTime;
                            }
                        }
                    }
                    else if (set == 2)
                    {
                        if (dayNumber == 1)
                        {
                            if (drv.Day1Set2.BestSUM == DNFvalue)
                                drv.Day1Set2.BestSUM = 0.0; // Actually exists, so reset from DNF

                            // some interesting stats
                            if (stats.Day1.Set2TimeOfFirstRun == "")
                            {
                                stats.Day1.Set2TimeOfFirstRun = run.StartTime;
                            }
                            stats.Day1.Set2TimeOfLastRun = run.StartTime;
                            stats.Day1.Set2NumberOfRuns += 1;
                            stats.Day1.Set2TotalTime += run.Time;
                            drv.Day1Set2Info += string.Format("{0,5:#.000}", run.Time);
                            if (!string.IsNullOrEmpty(run.Penalty))
                            {
                                drv.Day1Set2Info += "+" + run.Penalty;
                            }
                            drv.Day1Set2Info += ";";

                            drv.Day1Set2.Runs.Add(run); // Add this run to the driver's list

                            if ((drv.Day1Set2.Runs.Count <= drv.MaxOfficialRuns) && (run.AdjustedRawTime < drv.Day1Set2.BestRAW))
                            {
                                drv.Day1Set2.BestRAW = run.AdjustedRawTime;
                                drv.Day1Set2.BestRun = run;
                            }
                            if ((drv.Day1Set2.Runs.Count <= drv.MaxOfficialRuns) && (run.PAXtime < drv.Day1Set2.BestPAX))
                            {
                                drv.Day1Set2.BestPAX = run.PAXtime;
                            }
                            drv.ConeCount += run.ConeCount;
                            if (run.AdjustedRawTime >= DNFvalue)
                            {
                                string dnfSeconds = configData.GetField("secondsForDNF", "Value");
                                if (!int.TryParse(dnfSeconds, out int DNFpenalty))
                                {
                                    DNFpenalty = 200;
                                }
                                drv.Day1Set2.BestSUM += DNFpenalty; // We count a DNF as 200 seconds
                            }
                            else
                            {
                                drv.Day1Set2.BestSUM += run.AdjustedRawTime;
                            }
                        }
                        else    // day 2
                        {
                            if (drv.Day2Set2.BestSUM == DNFvalue)
                                drv.Day2Set2.BestSUM = 0.0; // Actually exists, so reset from DNF

                            // some interesting stats
                            if (stats.Day2.Set2TimeOfFirstRun == "")
                            {
                                stats.Day2.Set2TimeOfFirstRun = run.StartTime;
                            }
                            stats.Day2.Set2TimeOfLastRun = run.StartTime;
                            stats.Day2.Set2NumberOfRuns += 1;
                            stats.Day2.Set2TotalTime += run.Time;
                            drv.Day2Set2Info += string.Format("{0,5:#.000}", run.Time);
                            if (!string.IsNullOrEmpty(run.Penalty))
                            {
                                drv.Day2Set2Info += "+" + run.Penalty;
                            }
                            drv.Day2Set2Info += ";";

                            drv.Day2Set2.Runs.Add(run); // Add this run to the driver's list

                            if ((drv.Day2Set2.Runs.Count <= drv.MaxOfficialRuns) && (run.AdjustedRawTime < drv.Day2Set2.BestRAW))
                            {
                                drv.Day2Set2.BestRAW = run.AdjustedRawTime;
                                drv.Day2Set2.BestRun = run;
                            }
                            if ((drv.Day2Set2.Runs.Count <= drv.MaxOfficialRuns) && (run.PAXtime < drv.Day2Set2.BestPAX))
                            {
                                drv.Day2Set2.BestPAX = run.PAXtime;
                            }
                            drv.ConeCount += run.ConeCount;
                            if (run.AdjustedRawTime >= DNFvalue)
                            {
                                string dnfSeconds = configData.GetField("secondsForDNF", "Value");
                                if (!int.TryParse(dnfSeconds, out int DNFpenalty))
                                {
                                    DNFpenalty = 200;
                                }
                                drv.Day2Set2.BestSUM += DNFpenalty; // We count a DNF as 200 seconds
                            }
                            else
                            {
                                drv.Day2Set2.BestSUM += run.AdjustedRawTime;
                            }
                        }
                    }
                    else    // set 3 or higher
                    {
                        if (dayNumber == 1)
                        {
                            if (drv.Day1Set3.BestSUM == DNFvalue)
                                drv.Day1Set3.BestSUM = 0.0; // Actually exists, so reset from DNF

                            // some interesting stats
                            if (stats.Day1.Set3TimeOfFirstRun == "")
                            {
                                stats.Day1.Set3TimeOfFirstRun = run.StartTime;
                            }
                            stats.Day1.Set3TimeOfLastRun = run.StartTime;
                            stats.Day1.Set3NumberOfRuns += 1;
                            stats.Day1.Set3TotalTime += run.Time;
                            drv.Day1Set3Info += string.Format("{0,5:#.000}", run.Time);
                            if (!string.IsNullOrEmpty(run.Penalty))
                            {
                                drv.Day1Set3Info += "+" + run.Penalty;
                            }
                            drv.Day1Set3Info += ";";

                            drv.Day1Set3.Runs.Add(run); // Add this run to the driver's list

                            if ((drv.Day1Set3.Runs.Count <= drv.MaxOfficialRuns) && (run.AdjustedRawTime < drv.Day1Set3.BestRAW))
                            {
                                drv.Day1Set3.BestRAW = run.AdjustedRawTime;
                                drv.Day1Set3.BestRun = run;
                            }
                            if ((drv.Day1Set3.Runs.Count <= drv.MaxOfficialRuns) && (run.PAXtime < drv.Day1Set3.BestPAX))
                            {
                                drv.Day1Set3.BestPAX = run.PAXtime;
                            }
                            drv.ConeCount += run.ConeCount;
                            if (run.AdjustedRawTime >= DNFvalue)
                            {
                                string dnfSeconds = configData.GetField("secondsForDNF", "Value");
                                if (!int.TryParse(dnfSeconds, out int DNFpenalty))
                                {
                                    DNFpenalty = 200;
                                }
                                drv.Day1Set3.BestSUM += DNFpenalty; // We count a DNF as 200 seconds
                            }
                            else
                            {
                                drv.Day1Set3.BestSUM += run.AdjustedRawTime;
                            }
                        }
                        else    // day 2
                        {
                            if (drv.Day2Set3.BestSUM == DNFvalue)
                                drv.Day2Set3.BestSUM = 0.0; // Actually exists, so reset from DNF

                            // some interesting stats
                            if (stats.Day2.Set3TimeOfFirstRun == "")
                            {
                                stats.Day2.Set3TimeOfFirstRun = run.StartTime;
                            }
                            stats.Day2.Set3TimeOfLastRun = run.StartTime;
                            stats.Day2.Set3NumberOfRuns += 1;
                            stats.Day2.Set3TotalTime += run.Time;
                            drv.Day2Set3Info += string.Format("{0,5:#.000}", run.Time);
                            if (!string.IsNullOrEmpty(run.Penalty))
                            {
                                drv.Day2Set3Info += "+" + run.Penalty;
                            }
                            drv.Day2Set3Info += ";";

                            drv.Day2Set3.Runs.Add(run); // Add this run to the driver's list

                            if ((drv.Day2Set3.Runs.Count <= drv.MaxOfficialRuns) && (run.AdjustedRawTime < drv.Day2Set3.BestRAW))
                            {
                                drv.Day2Set3.BestRAW = run.AdjustedRawTime;
                                drv.Day1Set3.BestRun = run;
                            }
                            if ((drv.Day2Set3.Runs.Count <= drv.MaxOfficialRuns) && (run.PAXtime < drv.Day2Set3.BestPAX))
                            {
                                drv.Day2Set3.BestPAX = run.PAXtime;
                            }
                            drv.ConeCount += run.ConeCount;
                            if (run.AdjustedRawTime >= DNFvalue)
                            {
                                string dnfSeconds = configData.GetField("secondsForDNF", "Value");
                                if (!int.TryParse(dnfSeconds, out int DNFpenalty))
                                {
                                    DNFpenalty = 200;
                                }
                                drv.Day2Set3.BestSUM += DNFpenalty; // We count a DNF as 200 seconds
                            }
                            else
                            {
                                drv.Day2Set3.BestSUM += run.AdjustedRawTime;
                            }
                        }
                    }
                }
                // Add to the dictionary
                if (scores.ContainsKey(number))
                {
                    scores[number] = drv;
                }
                else
                {
                    scores.Add(number, drv);
                }
            }
        }

        /// <summary>
        /// Fix up run data
        /// </summary>
        /// <param name="args"></param>
        public static void CalcRunTimes(ScoreArgs args)
        {
            var myList = new List<KeyValuePair<string, DriverScoreData>>(scores);

            foreach (KeyValuePair<string, DriverScoreData> driver in myList)
            {
                if (driver.Value.LastName == "")
                    driver.Value.LastName = "Unknown";
                if (driver.Value.CarDescription.Length > 20)
                {
                    driver.Value.CarDescription = driver.Value.CarDescription.Substring(0, 20);
                }

                if (args.Set1Only)
                {
                    // We use both days if they exist
                    if ((driver.Value.Day2Set1.Runs.Count <= 0) && (string.IsNullOrEmpty(args.Day2)))
                    {
                        driver.Value.ScoreData.BestRAW = driver.Value.Day1Set1.BestRAW;
                        driver.Value.ScoreData.BestPAX = driver.Value.Day1Set1.BestPAX;
                        driver.Value.ScoreData.BestSUM = driver.Value.Day1Set1.BestRAW;
                    }
                    else
                    {
                        driver.Value.ScoreData.BestRAW = driver.Value.Day1Set1.BestRAW + driver.Value.Day2Set1.BestRAW;
                        driver.Value.ScoreData.BestPAX = driver.Value.Day1Set1.BestPAX + driver.Value.Day2Set1.BestPAX;
                        driver.Value.ScoreData.BestSUM = driver.Value.Day1Set1.BestRAW + driver.Value.Day2Set1.BestRAW;
                    }
                }
                else if (args.Set2Only)
                {
                    // We use both days if they exist
                    if ((driver.Value.Day2Set2.Runs.Count <= 0) && (string.IsNullOrEmpty(args.Day2)))
                    {
                        driver.Value.ScoreData.BestRAW = driver.Value.Day1Set2.BestRAW;
                        driver.Value.ScoreData.BestPAX = driver.Value.Day1Set2.BestPAX;
                        driver.Value.ScoreData.BestSUM = driver.Value.Day1Set2.BestRAW;
                    }
                    else
                    {
                        driver.Value.ScoreData.BestRAW = driver.Value.Day1Set2.BestRAW + driver.Value.Day2Set2.BestRAW;
                        driver.Value.ScoreData.BestPAX = driver.Value.Day1Set2.BestPAX + driver.Value.Day2Set2.BestPAX;
                        driver.Value.ScoreData.BestSUM = driver.Value.Day1Set2.BestRAW + driver.Value.Day2Set2.BestRAW;
                    }
                }
                else if (args.Set1PlusSet2)
                {
                    // both sets added together
                    // We use both days if they exist
                    if ((driver.Value.Day2Set1.Runs.Count <= 0) && (string.IsNullOrEmpty(args.Day2)))
                    {
                        driver.Value.ScoreData.BestRAW = driver.Value.Day1Set1.BestRAW + driver.Value.Day1Set2.BestRAW;
                        driver.Value.ScoreData.BestPAX = driver.Value.Day1Set1.BestPAX + driver.Value.Day1Set2.BestPAX;
                        driver.Value.ScoreData.BestSUM = driver.Value.Day1Set1.BestRAW + driver.Value.Day1Set2.BestRAW;
                    }
                    else
                    {
                        // two days, 4 sets
                        driver.Value.ScoreData.BestRAW = driver.Value.Day1Set1.BestRAW + driver.Value.Day1Set2.BestRAW +
                            driver.Value.Day2Set1.BestRAW + driver.Value.Day2Set2.BestRAW;
                        driver.Value.ScoreData.BestPAX = driver.Value.Day1Set1.BestPAX + driver.Value.Day1Set2.BestPAX +
                            driver.Value.Day2Set1.BestPAX + driver.Value.Day2Set2.BestPAX;
                        driver.Value.ScoreData.BestSUM = driver.Value.Day1Set1.BestRAW + driver.Value.Day1Set2.BestRAW +
                            driver.Value.Day2Set1.BestRAW + driver.Value.Day2Set2.BestRAW;
                    }
                }
                else if (args.BestSingleRun)
                {
                    // We use the best run of the official sets
                    if ((driver.Value.Day2Set1.Runs.Count <= 0) && (string.IsNullOrEmpty(args.Day2)))
                    {
                        // best run of the two sets
                        if (driver.Value.Day1Set1.BestPAX < driver.Value.Day1Set2.BestPAX)
                        {
                            driver.Value.ScoreData.BestPAX = driver.Value.Day1Set1.BestPAX;
                        }
                        else
                        {
                            driver.Value.ScoreData.BestPAX = driver.Value.Day1Set2.BestPAX;
                        }
                        if (driver.Value.Day1Set1.BestRAW < driver.Value.Day1Set2.BestRAW)
                        {
                            driver.Value.ScoreData.BestRAW = driver.Value.Day1Set1.BestRAW;
                        }
                        else
                        {
                            driver.Value.ScoreData.BestRAW = driver.Value.Day1Set2.BestRAW;
                        }
                        if (driver.Value.Day1Set1.BestSUM < driver.Value.Day1Set2.BestSUM)
                        {
                            driver.Value.ScoreData.BestSUM = driver.Value.Day1Set1.BestSUM;
                        }
                        else
                        {
                            driver.Value.ScoreData.BestSUM = driver.Value.Day1Set2.BestSUM;
                        }
                    }
                    else
                    {
                        // 2 day event, so we total the best run from each day
                        // best run of the two sets
                        if (driver.Value.Day1Set1.BestPAX < driver.Value.Day1Set2.BestPAX)
                        {
                            driver.Value.ScoreData.BestPAX = driver.Value.Day1Set1.BestPAX;
                            if (driver.Value.Day2Set1.BestPAX < driver.Value.Day2Set2.BestPAX)
                            {
                                driver.Value.ScoreData.BestPAX += driver.Value.Day2Set1.BestPAX;
                            }
                            else
                            {
                                driver.Value.ScoreData.BestPAX += driver.Value.Day2Set2.BestPAX;
                            }
                        }
                        else
                        {
                            driver.Value.ScoreData.BestPAX = driver.Value.Day1Set2.BestPAX;
                            if (driver.Value.Day2Set1.BestPAX < driver.Value.Day2Set2.BestPAX)
                            {
                                driver.Value.ScoreData.BestPAX += driver.Value.Day2Set1.BestPAX;
                            }
                            else
                            {
                                driver.Value.ScoreData.BestPAX += driver.Value.Day2Set2.BestPAX;
                            }
                        }
                        if (driver.Value.Day1Set1.BestRAW < driver.Value.Day1Set2.BestRAW)
                        {
                            driver.Value.ScoreData.BestRAW = driver.Value.Day1Set1.BestRAW;
                            if (driver.Value.Day2Set1.BestRAW < driver.Value.Day2Set2.BestRAW)
                            {
                                driver.Value.ScoreData.BestRAW += driver.Value.Day2Set1.BestRAW;
                            }
                            else
                            {
                                driver.Value.ScoreData.BestRAW += driver.Value.Day2Set2.BestRAW;
                            }
                        }
                        else
                        {
                            driver.Value.ScoreData.BestRAW = driver.Value.Day1Set2.BestRAW;
                            if (driver.Value.Day2Set1.BestRAW < driver.Value.Day2Set2.BestRAW)
                            {
                                driver.Value.ScoreData.BestRAW += driver.Value.Day2Set1.BestRAW;
                            }
                            else
                            {
                                driver.Value.ScoreData.BestRAW += driver.Value.Day2Set2.BestRAW;
                            }
                        }
                    }
                }
                // Adjust PAX for DNS values
                if (driver.Value.ScoreData.BestRAW >= DNFvalue)
                {
                    driver.Value.ScoreData.BestRAW = DNFvalue;
                }
            }
        }

        /// <summary>
        /// Add penalties and return adjusted time
        /// DNF is 1000
        /// Use PAX of 1.0 to get adjusted raw time
        /// </summary>
        /// <param name="rawtime"></param>
        /// <param name="penalty"></param>
        /// <param name="PAX"></param>
        /// <returns></returns>
        public static double AdjustTime(double rawtime, string penalty, double PAX)
        {
            if (penalty == "DNF")
            {
                return DNFvalue;
            }
            string conePen = configData.GetField("ConePenalySeconds", "Value");
            if (!int.TryParse(conePen, out int conePenalty))
            {
                conePenalty = 2;
            }

            double totalTime = rawtime * PAX;
            if (int.TryParse(penalty, out int cones))
            {
                if (ConesGetPAXed)
                {
                    totalTime = (rawtime + (cones * conePenalty)) * PAX;
                }
                else
                {
                    totalTime = (rawtime * PAX) + (cones * conePenalty);
                }
            }
            return totalTime;
        }

        /// <summary>
        /// Calculate the number of trophy spots in a class
        /// </summary>
        /// <param name="numDrivers"></param>
        /// <returns></returns>
        public static int TrophyCount(int numDrivers)
        {
            if (numDrivers == 0) return 0;
            if (numDrivers <= 3) return 1;
            if (numDrivers <= 6) return 2;
            if (numDrivers <= 9) return 3;
            // one more trophy for every additional four competitors
            if (numDrivers <= 13) return 4;
            if (numDrivers <= 17) return 5;
            if (numDrivers <= 21) return 6;
            if (numDrivers <= 25) return 7;
            if (numDrivers <= 29) return 8;
            if (numDrivers <= 33) return 9;
            if (numDrivers <= 37) return 10;
            return 11; // 41 in a class should do...

        }

        /// <summary>
        /// Calculate the number of points given the place (1..<n>)
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static int CalcClassPoints(int position)
        {
            string pointAwards = configData.GetField("PointAwards", "Value");
            if (pointAwards.Length > 0)
            {
                string[] points = pointAwards.Split(';');
                position -= 1; // make it 0 based
                if (position >= points.Length)
                {
                    return 1;
                }
                string pointString = points[position];
                if (!int.TryParse(pointString, out int pointValue))
                {
                    return 1;
                }
                return pointValue;
            }
            // No config -- just use these commonly used values
            if (position == 1) return 20;
            if (position == 2) return 16;
            if (position == 3) return 14;
            if (position == 4) return 12;
            if (position == 5) return 11;
            if (position == 6) return 10;
            if (position == 7) return 9;
            if (position == 8) return 8;
            if (position == 9) return 7;
            if (position == 10) return 6;
            if (position == 11) return 5;
            if (position == 12) return 4;
            if (position == 13) return 3;
            if (position == 14) return 2;
            if (position == 15) return 1;
            return 0;
        }

        #endregion

        #region Public Static Helpers

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

        public static int CalcCones(string penalty)
        {
            if (int.TryParse(penalty, out int cones))
            {
                return cones;
            }
            return 0;
        }

        #endregion
    }
}