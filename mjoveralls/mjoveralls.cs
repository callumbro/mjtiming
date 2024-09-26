/*
 * 
 * Created by Murray Peterson
 * 
 */

// reverted back to text instead of html

// ASN trophy rules:
// 1-3 competitors = 1 trophy
// 4-6 competitors = 2 trophies
// 7-9 competitors = 3 trophies
// 1 trophy for every four additional competitors
// Basically, 3,3,3,4,4,4,4,...
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// disable CompareOfFloatsByEqualityOperator
namespace RaceBeam
{
    class Champ
    {
        /// <summary>
        /// All the scores for a given event
        /// </summary>
        public class EventScore
        {
            public string eventName = "";
            public double RAWScore;
            public double rookieRAWScore;
            public double PAXScore;
            public double rookiePAXScore;
            public double groupScore;
            public double groupRookieScore;
            public int coneCount;
        }

        /// <summary>
        /// A group and the event scores withion that group
        /// </summary>
        public class GroupScore
        {
            public string groupName = "";
            public double totalPAX;
            public double totalRookiePAX;
            public double bestPAX;
            public double bestRookiePAX;
            public Dictionary<string, EventScore> eventScores = new Dictionary<string, EventScore>();
        }

        /// <summary>
        /// Info about a single driver (we use first name + last name as key) and overall raw/pax scores
        /// </summary>
        private class DriverData
        {
            public string number;
            public string driver;   // first name + last name
            public string firstName;
            public string lastName;
            public string member;
            public string rookie;
            public double totalPAX;
            public double totalRookiePAX;
            public double totalRAW;
            public double totalRookieRAW;
            public double bestPAX;
            public double bestRookiePAX;
            public double bestRAW;
            public double bestRookieRAW;
            public int totalCones;
            public int totalEvents;
            public int totalDNF;
            // dictionary of overall pax and raw scores for each attended event
            public Dictionary<string, EventScore> overallScores = new Dictionary<string, EventScore>();
            // dictionary of groups in which this driver competed.  Each group contains a list of events where that group was used
            public Dictionary<string, GroupScore> groupScores = new Dictionary<string, GroupScore>();
        }

        public class PaxInfo
        {
            public string carClass;
            public string pax;
            public string description;
            public string group;
            public string displayOrder;
        }

        private const string separator = "--------------------------------------------------------------------------------\r\n";
        private static string eventFolder = "";
        private static string classfile = "";
        private static string title = "";
        private static string year = DateTime.Now.Year.ToString();
        private static int numDays = 8;
        private static int numRookieDays = 7;
        private static bool doRookie = true;
        private static bool membersOnly = false;
        private static bool doAttendance = false;
        private static bool showLastName = false;
        private static string htmlStyle = "";
        private static string htmlHeader = "<head><style>%{STYLE}%</style><script type=\"text/javascript\">function showResults(id) { let allResults = document.getElementsByClassName('results'); for (const element of allResults) { element.setAttribute('style', 'display: none;'); } document.getElementById(id).setAttribute('style', 'display: block;'); }</script></head>";
        private static string htmlBody = "<body><h1>%{TITLE}%</h1>%{BUTTONS}%%{RESULTS}%</body>";

        private static readonly Dictionary<string, DriverData> scores = new Dictionary<string, DriverData>();
        private static List<string> eventNames = new List<string>();
        private static readonly CSVData classData = new CSVData();
        private static readonly CSVData configData = new CSVData();

        public static void Usage()
        {
            Console.WriteLine("Usage: overalls -title <title string> -year <season year (defaults to current year)> -norookie -membersonly -attendance -best <num> -rookiebest <num> -path <path to event data folder> -classfile <path to class.csv file>");
            Environment.Exit(0);
        }

        public static void Main(string[] args)
        {
            // parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "-h") | (args[i] == "-help") | (args[i] == "-?"))
                {
                    Usage();
                }
                else if (args[i] == "-path")
                {
                    i += 1;
                    eventFolder = args[i];
                }
                else if (args[i] == "-classfile")
                {
                    i += 1;
                    classfile = args[i];
                }
                else if (args[i] == "-best")
                {
                    i += 1;
                    if (int.TryParse(args[i], out numDays) == false)
                    {
                        Usage();
                    }
                }
                else if (args[i] == "-rookiebest")
                {
                    i += 1;
                    if (int.TryParse(args[i], out numRookieDays) == false)
                    {
                        Usage();
                    }
                }
                else if (args[i] == "-title")
                {
                    i += 1;
                    title = args[i];
                    Console.WriteLine(title);
                }
                else if (args[i] == "-year")
                {
                    i += 1;
                    year = args[i];
                }
                else if (args[i] == "-norookie")
                {
                    doRookie = false;       // no rookie scores
                }
                else if (args[i] == "-membersonly")
                {
                    membersOnly = true;     // only display members
                }
                else if (args[i] == "-attendance")
                {
                    doAttendance = true;
                }
                else
                {
                    Usage();
                }
            }
            if (eventFolder == "")
            {
                Usage();
            }

            string configFolder = Environment.ProcessPath;
            configFolder = Path.GetDirectoryName(configFolder);
            configFolder += "\\..\\config";

            string configFilename = configFolder + "\\configData.csv";
            string err = configData.LoadData(configFilename, ',', "Parameter");
            if (err != "")
            {
                Console.Error.WriteLine("Unable to load config file: " + err);
                Usage();
            }

            if (configData.GetField("ShowLastName", "Value").Contains("Y"))
            {
                showLastName = true;
            }

            string classDataFile = configData.GetField("classDataFile", "Value");
            if (classDataFile == "")
            {
                Console.Error.WriteLine("Class data file not defined in config file");
            }
            if (classfile != "")
            {
                classDataFile = classfile;
            }

            // Read in class data file
            err = classData.LoadData(classDataFile, ',', "Class");
            if (err != "")
            {
                Console.Error.WriteLine(err);
            }

            StringBuilder textFile = new();

            ReadData();
            eventNames
                .Sort(delegate (
                    string ev1,
                    string ev2)
                {
                    string ev1a = Regex.Replace(ev1, "_", "");
                    string ev2a = Regex.Replace(ev2, "_", "");
                    return ev1a.CompareTo(ev2a);
                });

            // raw data, everyone
            CalcData(false, false);
            string rawPrint = PrintData(false, false);
            textFile.AppendLine(rawPrint);

            // raw data, rookie
            if (doRookie == true)
            {
                CalcData(false, true);
                Console.WriteLine("");
                string rookieRawPrint = PrintData(false, true);
                textFile.AppendLine(rookieRawPrint);
            }

            //// pax data, everyone
            //Console.WriteLine();
            //CalcData(true, false);
            //Console.WriteLine();
            //string paxPrint = PrintData(true, false);
            //textFile.AppendLine(paxPrint);

            //// pax data, rookie
            //if (doRookie == true)
            //{
            //    CalcData(true, true);
            //    Console.WriteLine();
            //    string rookiePaxPrint = PrintData(true, true);
            //    textFile.AppendLine(rookiePaxPrint);
            //}

            textFile.AppendLine();

            // class scores
            CalcData(true, false);
            string classScoresPrint = ClassScores(false); // everyone
            textFile.AppendLine(classScoresPrint);
            if (doRookie == true)
            {
                CalcData(true, true);
                string rookieClassScoresPrint = ClassScores(true); // rookie
                textFile.AppendLine(rookieClassScoresPrint);
            }
            textFile.AppendLine();

            string conesPrint = PrintCones();
            textFile.AppendLine(conesPrint);

            string dnfPrint = PrintDNF();
            textFile.AppendLine(dnfPrint);

            if (doAttendance)
            {
                string attendancePrint = Attendance();
                textFile.AppendLine(attendancePrint);
            }

            Console.WriteLine(textFile.ToString());
            string textPath = eventFolder + $"\\{year}__seasonResults.txt";
            File.WriteAllText(textPath, textFile.ToString());


            // Generate HTML file
            CalcData(false, false);
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

            htmlButtons += "<button class=\"button\" onclick=\"showResults('class-ranking');\">Class</button>";
            htmlButtons += "<button class=\"button\" onclick=\"showResults('overall-ranking');\">Overall</button>";

            htmlResults += HtmlClassResults();
            htmlResults += HtmlOverallResults();

            htmlBody = htmlBody.Replace("%{TITLE}%", title);
            htmlBody = htmlBody.Replace("%{BUTTONS}%", htmlButtons);
            htmlBody = htmlBody.Replace("%{RESULTS}%", htmlResults);

            string htmlAllResults = $"<html><title>{title}</title>" + htmlHeader + htmlBody + "</html>";

            string path = eventFolder + $"\\{year}__seasonResults.html";
            File.WriteAllText(path, htmlAllResults);
        }

        /// <summary>
        /// Print out cone counts
        /// </summary>
        /// <returns>Formatted string</returns>
        public static string PrintCones()
        {
            StringBuilder cones = new();

            var myList = new List<KeyValuePair<string, DriverData>>(scores);
            // Sort drivers by cone count
            myList
                .Sort(delegate (
                    KeyValuePair<string, DriverData> firstPair,
                    KeyValuePair<string, DriverData> nextPair)
                {
                    return nextPair.Value.totalCones.CompareTo(firstPair.Value.totalCones);
                });

            // print header line
            cones.Append(separator);
            cones.AppendLine("Overall cone count");
            cones.Append("Pos Driver        ");
            cones.AppendLine("    Total cones");

            // Now print out drivers, their event scores and their total
            int position = 1;
            int totCones = 0;
            foreach (KeyValuePair<string, DriverData> driver in myList)
            {
                if (driver.Value.totalCones <= 0)
                {
                    break;
                }
                if ((membersOnly == true) && (driver.Value.member.ToUpperInvariant().Contains("Y") == false))
                {
                    continue;
                }
                string line = string.Format("{0,3} {1,-14}",
                                            position++,
                                            driver.Value.driver
                                           );
                cones.Append(line);
                line = string.Format("  {0,10:#0}", driver.Value.totalCones);
                totCones += driver.Value.totalCones;
                cones.AppendLine(line);
            }

            cones.AppendLine("Total cones: " + totCones);

            return cones.ToString();
        }

        /// <summary>
        /// print out DNF counts
        /// </summary>
        /// <returns>Formatted string</returns>
        public static string PrintDNF()
        {
            StringBuilder dnf = new();

            var myList = new List<KeyValuePair<string, DriverData>>(scores);
            // Sort drivers by DNF count
            myList
                .Sort(delegate (
                    KeyValuePair<string, DriverData> firstPair,
                    KeyValuePair<string, DriverData> nextPair)
                {
                    return nextPair.Value.totalDNF.CompareTo(firstPair.Value.totalDNF);
                });

            // print header line
            dnf.AppendLine(separator + "Overall DNF count");
            dnf.Append("Pos Driver        ");
            dnf.AppendLine("    Total DNF");

            // Now print out drivers, their event scores and their total
            int position = 1;
            int totalDNF = 0;
            foreach (KeyValuePair<string, DriverData> driver in myList)
            {
                if (driver.Value.totalDNF <= 0)
                {
                    break;
                }
                if ((membersOnly == true) && (driver.Value.member.ToUpperInvariant().Contains("Y") == false))
                {
                    continue;
                }
                string line = string.Format("{0,3} {1,-14}",
                                            position++,
                                            driver.Value.driver
                                           );
                dnf.Append(line);
                line = string.Format("  {0,10:#0}", driver.Value.totalDNF);
                totalDNF += driver.Value.totalDNF;
                dnf.AppendLine(line);
            }
            dnf.AppendLine("Total DNF: " + totalDNF);

            return dnf.ToString();
        }

        /// <summary>
        /// print out attendance stats
        /// </summary>
        /// <returns>Formatted string</returns>
        public static string Attendance()
        {
            StringBuilder attendance = new();

            var myList = new List<KeyValuePair<string, DriverData>>(scores);
            var attend = new SortedDictionary<int, int>();
            var rookieattend = new SortedDictionary<int, int>();

            // print header line
            attendance.Append(separator);
            attendance.AppendLine("Attendance");

            for (int i = 1; i <= eventNames.Count; i++)
            {
                attend.Add(i, 0);
                rookieattend.Add(i, 0);
            }

            // Go through driver list and add his events to the dict
            foreach (KeyValuePair<string, DriverData> driver in myList)
            {
                attend[driver.Value.totalEvents] += 1;
                if (driver.Value.rookie.Contains("Y"))
                {
                    rookieattend[driver.Value.totalEvents] += 1;
                }
            }

            // Print out results
            attendance.AppendLine(string.Format("{0,8} {1,8}", "# Events", "# Drivers"));
            int totalDrivers = 0;
            foreach (int n in attend.Keys)
            {
                string line = string.Format("{0,8} {1,8}",
                                            n,
                                            attend[n]
                                           );
                totalDrivers += attend[n];
                attendance.AppendLine(line);
            }
            attendance.AppendLine("Total drivers: " + totalDrivers.ToString());

            if (doRookie)
            {
                int totalRookieDrivers = 0;
                attendance.AppendLine();
                attendance.AppendLine("Rookie-only:");
                attendance.AppendLine(string.Format("{0,8} {1,8}", "Events", "Drivers"));
                foreach (int n in rookieattend.Keys)
                {
                    string line = string.Format("{0,8} {1,8}",
                                                n,
                                                rookieattend[n]
                                               );
                    totalRookieDrivers += rookieattend[n];
                    attendance.AppendLine(line);
                }
                attendance.AppendLine("Total rookie drivers: " + totalRookieDrivers.ToString());
            }

            return attendance.ToString();
        }

        /// <summary>
        /// print out all driver data
        /// </summary>
        /// <param name="isPAX">true to print pax data, false to print raw data</param>
        /// <param name="isRookie">true to print rookie data</param>
        /// <returns>Formatted string</returns>
        public static string PrintData(bool isPAX, bool isRookie)
        {
            StringBuilder print = new();

            var myList = new List<KeyValuePair<string, DriverData>>(scores);

            // Sort drivers by PAX or RAW
            myList
                .Sort(delegate (
                    KeyValuePair<string, DriverData> firstPair,
                    KeyValuePair<string, DriverData> nextPair)
                {
                    if (isPAX == true)
                    {
                        if (isRookie == true)
                        {
                            return nextPair.Value.bestRookiePAX.CompareTo(firstPair.Value.bestRookiePAX);
                        }
                        else
                        {
                            return nextPair.Value.bestPAX.CompareTo(firstPair.Value.bestPAX);
                        }
                    }
                    else
                    {
                        if (isRookie == true)
                        {
                            return nextPair.Value.bestRookieRAW.CompareTo(firstPair.Value.bestRookieRAW);
                        }
                        else
                        {
                            return nextPair.Value.bestRAW.CompareTo(firstPair.Value.bestRAW);
                        }
                    }
                });

            // print header line
            print.Append(separator);
            if (isPAX == true)
            {
                if (isRookie == true)
                {
                    print.AppendLine("Rookie-only PAX scores");
                }
                else
                {
                    print.AppendLine("Overall PAX scores");
                }
            }
            else
            {
                if (isRookie == true)
                {
                    print.AppendLine("Rookie-only scores");
                }
                else
                {
                    print.AppendLine("Overall scores");
                }
            }
            print.Append("Pos Driver        ");

            foreach (string name in eventNames)
            {
                print.Append(string.Format("{0,10} ", name));
            }
            if (isRookie == true)
                print.AppendLine($"    Best {numRookieDays}");
            else
                print.AppendLine($"    Best {numDays}");

            // Now print out drivers, their event scores and their total
            int position = 1;
            foreach (KeyValuePair<string, DriverData> driver in myList)
            {
                string rookie = driver.Value.rookie.ToUpperInvariant();
                if ((isRookie == true) && (rookie.Contains("Y") == false))
                {
                    continue;
                }
                // don't print out people with 0 scores (say 1 event with a DNS)
                if (isPAX == true)
                {
                    if ((isRookie == true) && (driver.Value.totalRookiePAX <= 0.0))
                    {
                        break;
                    }
                    else if (driver.Value.totalPAX <= 0.0)
                    {
                        break;
                    }
                }
                else
                {
                    if (driver.Value.totalRAW <= 0.0)
                    {
                        break;
                    }
                }
                string member = driver.Value.member.ToUpperInvariant();
                if ((membersOnly == true) && (member.Contains("Y") == false))
                {
                    continue;
                }
                string line = string.Format("{0,3} {1,-13}",
                                            position++,
                                            driver.Value.driver
                                           );
                print.Append(line);

                foreach (string eventName in eventNames)
                {
                    if (driver.Value.overallScores.ContainsKey(eventName) == false)
                    {
                        // never attended
                        line = string.Format(" {0,10:#0.000}", 0.00);
                    }
                    else
                    {
                        if (isPAX == true)
                        {
                            if (isRookie == true)
                            {
                                line = string.Format(" {0,10:#0.000}", driver.Value.overallScores[eventName].rookiePAXScore);
                            }
                            else
                            {
                                line = string.Format(" {0,10:#0.000}", driver.Value.overallScores[eventName].PAXScore);
                            }
                        }
                        else
                        {
                            if (isRookie == true)
                            {
                                line = string.Format(" {0,10:#0.000}", driver.Value.overallScores[eventName].rookieRAWScore);
                            }
                            else
                            {
                                line = string.Format(" {0,10:#0.000}", driver.Value.overallScores[eventName].RAWScore);
                            }
                        }
                    }
                    print.Append(line);
                }
                if (isPAX == true)
                {
                    if (isRookie == true)
                    {
                        line = string.Format("  {0,10:#0.000}", driver.Value.bestRookiePAX);
                    }
                    else
                    {
                        line = string.Format("  {0,10:#0.000}", driver.Value.bestPAX);
                    }
                }
                else
                {
                    if (isRookie == true)
                    {
                        line = string.Format("  {0,10:#0.000}", driver.Value.bestRookieRAW);
                    }
                    else
                    {
                        line = string.Format("  {0,10:#0.000}", driver.Value.bestRAW);
                    }
                }

                print.AppendLine($"{line}");
            }

            return print.ToString();
        }

        /// <summary>
        /// print out results by class in html format
        /// </summary>
        /// <returns>Formatted string</returns>
        public static string HtmlClassResults()
        {
            StringBuilder classResults = new();

            classResults.Append("<div id=\"class-ranking\" class=\"results\">");
            classResults.Append("<h2>Class Ranking</h2>");

            // First build a sorted class list
            // Sort order is given in the csv file
            var sortedClassList = new SortedDictionary<int, PaxInfo>();
            List<string> classList = classData.getKeys();
            foreach (string className in classList)
            {
                var p = new PaxInfo
                {
                    carClass = className,
                    pax = classData.GetField(className, "PAX"),
                    description = classData.GetField(className, "Description"),
                    group = classData.GetField(className, "Group"),
                    displayOrder = classData.GetField(className, "Display Order")
                };
                if (int.TryParse(p.displayOrder, out int orderVal))
                {
                    try
                    {
                        sortedClassList.Add(orderVal, p);
                    }
                    catch { }
                }
            }

            // Each driver has a list of event scores, with each event possibly in a different class
            // For each class, we need to make a new driver list, with all drivers that ever ran in that class
            // If they didn't run in that class, then the event score (raw) is 0.0
            // Then sort by score totals for each driver

            foreach (KeyValuePair<int, PaxInfo> classInfo in sortedClassList)
            {
                PaxInfo curClass = classInfo.Value;
                int rank = 1;

                // make a new driver list, with all drivers that ever ran in this class
                var drvList = new List<DriverData>();
                foreach (string driverName in scores.Keys)
                {
                    DriverData driver = scores[driverName];
                    if (driver.groupScores.ContainsKey(curClass.carClass))
                    {
                        drvList.Add(driver);
                    }
                }
                // skip group if nobody competed there
                if (drvList.Count == 0)
                {
                    continue;
                }

                // Now sort drivers by their overall score in this group
                drvList
                    .Sort(delegate (
                        DriverData first,
                        DriverData next)
                    {
                        return next.groupScores[curClass.carClass].bestPAX.CompareTo(first.groupScores[curClass.carClass].bestPAX);
                    });

                classResults.Append("<table>");
                classResults.Append("<table>");
                classResults.Append("<thead>");
                classResults.Append("<tr>");
                classResults.Append($"<th colspan=\"{4 + eventNames.Count}\">{curClass.carClass} ({curClass.description})</th>");
                classResults.Append("</tr>");
                classResults.Append("<th>Rank</th>");
                classResults.Append("<th>Driver</th>");
                classResults.Append("<th>Total</th>");
                classResults.Append($"<th>Best {numDays}</th>");

                foreach (string eventName in eventNames)
                {
                    classResults.Append($"<th>{eventName}</th>");
                }

                classResults.Append("</tr>");
                classResults.Append("</thead>");
                classResults.Append("<tbody>");

                foreach (DriverData driver in drvList)
                {
                    string driverName = driver.firstName + " " + driver.lastName.Substring(0, 1);
                    if (configData.GetField("ShowLastName", "Value").Contains("Y"))
                    {
                        driverName = driver.firstName + " " + driver.lastName;
                    }

                    string total = driver.groupScores[curClass.carClass].totalPAX.ToString("#0.000");
                    string bestTotal = driver.groupScores[curClass.carClass].bestPAX.ToString("#0.000");

                    classResults.Append("<tr>");
                    classResults.Append($"<td>{rank}</td>");
                    classResults.Append($"<td>{driverName}</td>");
                    classResults.Append($"<td>{total}</td>");
                    classResults.Append($"<td>{bestTotal}</td>");

                    // Driver score dict has gaps -- not all events attended
                    foreach (string eventName in eventNames)
                    {
                        if (driver.groupScores[curClass.carClass].eventScores.ContainsKey(eventName) == false)
                        {
                            classResults.Append($"<td>0.000</td>");
                        }
                        else
                        {
                            classResults.Append($"<td>{driver.groupScores[curClass.carClass].eventScores[eventName].groupScore.ToString("#0.000")}</td>");
                        }
                    }

                    classResults.Append("</tr>");
                    rank++;
                }

                classResults.Append("</tbody>");
                classResults.Append("</table>");
            }

            classResults.Append("</div>");

            return classResults.ToString();
        }

        /// <summary>
        /// print out overall raw results in html format
        /// </summary>
        /// <returns>Formatted string</returns>
        public static string HtmlOverallResults()
        {
            StringBuilder overallResults = new();

            overallResults.Append("<div id=\"overall-ranking\" class=\"results\" style=\"display: none;\">");
            overallResults.Append("<h2>Overall Ranking</h2>");

            var myList = new List<KeyValuePair<string, DriverData>>(scores);

            // Sort drivers by RAW
            myList
                .Sort(delegate (
                    KeyValuePair<string, DriverData> firstPair,
                    KeyValuePair<string, DriverData> nextPair)
                {
                    return nextPair.Value.bestRAW.CompareTo(firstPair.Value.bestRAW);
                });

            overallResults.Append("<table>");
            overallResults.Append("<thead>");
            overallResults.Append("<tr>");
            overallResults.Append($"<th colspan=\"{4 + eventNames.Count}\">Overall Scores</th>");
            overallResults.Append("</tr>");
            overallResults.Append("<tr>");
            overallResults.Append("<th>Rank</th>");
            overallResults.Append("<th>Driver</th>");
            overallResults.Append("<th>Total</th>");
            overallResults.Append($"<th>Best {numDays}</th>");

            foreach (string eventName in eventNames)
            {
                overallResults.Append($"<th>{eventName}</th>");
            }

            overallResults.Append("</tr>");
            overallResults.Append("</thead>");
            overallResults.Append("<tbody>");

            // Now print out drivers, their event scores and their total
            int rank = 1;
            foreach (KeyValuePair<string, DriverData> driver in myList)
            {
                // don't print out people with 0 scores (say 1 event with a DNS)
                if (driver.Value.totalRAW <= 0.0)
                {
                    break;
                }

                string driverName = driver.Value.firstName + " " + driver.Value.lastName.Substring(0, 1);
                if (configData.GetField("ShowLastName", "Value").Contains("Y"))
                {
                    driverName = driver.Value.firstName + " " + driver.Value.lastName;
                }

                string total = driver.Value.totalPAX.ToString("#0.000");
                string bestTotal = driver.Value.bestPAX.ToString("#0.000");

                overallResults.Append("<tr>");
                overallResults.Append($"<td>{rank}</td>");
                overallResults.Append($"<td>{driverName}</td>");
                overallResults.Append($"<td>{total}</td>");
                overallResults.Append($"<td>{bestTotal}</td>");

                foreach (string eventName in eventNames)
                {
                    if (driver.Value.overallScores.ContainsKey(eventName) == false)
                    {
                        // never attended
                        overallResults.Append($"<td>0.000</td>");
                    }
                    else
                    {
                        overallResults.Append($"<td>{driver.Value.overallScores[eventName].RAWScore.ToString("#0.000")}</td>");
                    }
                }

                overallResults.Append("</tr>");
                rank++;
            }

            overallResults.Append("</tbody>");
            overallResults.Append("</table>");

            overallResults.Append("</div>");

            return overallResults.ToString();
        }

        /// <summary>
        /// Calculate the scores from the season data
        /// </summary>
        /// <param name="isPAX">true to print pax data, false to print raw data</param>
        /// <param name="isRookie">true to print rookie data</param>
        public static void CalcData(bool isPAX, bool isRookie)
        {
            // First go through and calculate best total scores
            foreach (string driverName in scores.Keys)
            {
                DriverData driver = scores[driverName];
                driver.totalPAX = 0.0;
                driver.totalRookiePAX = 0.0;
                driver.totalRAW = 0.0;
                driver.totalRookieRAW = 0.0;
                driver.bestPAX = 0.0;
                driver.bestRookiePAX = 0.0;
                driver.bestRAW = 0.0;
                driver.bestRookieRAW = 0.0;

                // Sort by PAX score, descending order
                var myList = new List<EventScore>(driver.overallScores.Values);

                // Sort drivers by PAX or RAW
                myList
                    .Sort(delegate (
                        EventScore first,
                        EventScore next)
                    {
                        if (isPAX == true)
                        {
                            if (isRookie == true)
                            {
                                return next.rookiePAXScore.CompareTo(first.rookiePAXScore);
                            }
                            else
                            {
                                return next.PAXScore.CompareTo(first.PAXScore);
                            }
                        }
                        else
                        {
                            if (isRookie == true)
                            {
                                return next.rookieRAWScore.CompareTo(first.rookieRAWScore);
                            }
                            else
                            {
                                return next.RAWScore.CompareTo(first.RAWScore);
                            }
                        }
                    });

                // Add up scores until we hit the "best N" count
                int bestCount = numDays;
                if (isRookie == true)
                {
                    bestCount = numRookieDays;
                }
                foreach (EventScore evScore in myList)
                {
                    driver.totalPAX += evScore.PAXScore;
                    driver.totalRAW += evScore.RAWScore;
                    driver.totalRookiePAX += evScore.rookiePAXScore;
                    driver.totalRookieRAW += evScore.rookieRAWScore;
                    bestCount -= 1;
                    if (bestCount >= 0)
                    {
                        driver.bestPAX += evScore.PAXScore;
                        driver.bestRAW += evScore.RAWScore;
                        driver.bestRookiePAX += evScore.rookiePAXScore;
                        driver.bestRookieRAW += evScore.rookieRAWScore;
                    }
                }

                // Deal with Group scores
                foreach (string grp in driver.groupScores.Keys)
                {
                    bestCount = numDays;
                    if (isRookie == true)
                    {
                        bestCount = numRookieDays;
                    }
                    // convert event dict into a list
                    myList = new List<EventScore>(driver.groupScores[grp].eventScores.Values);
                    // sort in descending order by group score
                    myList
                        .Sort(delegate (
                            EventScore first,
                            EventScore next)
                        {
                            if (isRookie == true)
                            {
                                return next.groupRookieScore.CompareTo(first.groupRookieScore);
                            }
                            else
                            {
                                return next.groupScore.CompareTo(first.groupScore);
                            }
                        });

                    if (isRookie == true)
                    {
                        bestCount = numRookieDays;
                        driver.groupScores[grp].totalRookiePAX = 0.0;
                    }
                    else
                    {
                        bestCount = numDays;
                        driver.groupScores[grp].totalPAX = 0.0;
                        driver.groupScores[grp].bestPAX = 0.0;
                    }
                    foreach (EventScore evScore in myList)
                    {
                        if (isRookie == true)
                        {
                            driver.groupScores[grp].totalRookiePAX += evScore.groupRookieScore;
                        }
                        else
                        {
                            driver.groupScores[grp].totalPAX += evScore.groupScore;
                        }

                        bestCount -= 1;
                        if (bestCount >= 0)
                        {
                            if (isRookie == true)
                            {
                                driver.groupScores[grp].bestRookiePAX += evScore.groupRookieScore;
                            }
                            else
                            {
                                driver.groupScores[grp].bestPAX += evScore.groupScore;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read in the data files from the season
        /// </summary>
        public static string ReadData()
        {
            string[] filePaths = Directory.GetFiles(eventFolder, "*_CSVData.csv");
            // Read in timing data
            foreach (string scorefile in filePaths)
            {
                //Console.Error.WriteLine("Adding data for " + scorefile);

                // parse out the event name from something like c:\blah\12_02_23_CSVData.csv
                //Regex reg = new Regex(@".*(event[0-9]+).*");
                var reg = new Regex(@"^.*[\\/](.*)_CSVData.csv$");
                Match m;
                m = reg.Match(scorefile);
                if (m.Success == false)
                {
                    Console.WriteLine("Unable to parse:" + scorefile);
                    return "";
                }
                GroupCollection g = m.Groups;
                string eventName = g[1].Value;
                if (eventNames.Contains(eventName) == false)
                {
                    eventNames.Add(eventName);
                }

                var ev = new CSVData();
                ev.LoadData(scorefile, ',', "Car#");
                foreach (string carnum in ev.getKeys())
                {
                    DriverData driver;
                    // Each event is keyed by car number, but we will key the overalls by the driver's full name
                    string driverName = ev.GetField(carnum, "First Name") + "_" + ev.GetField(carnum, "Last Name");
                    // Check for registered but didn't compete
                    string px = ev.GetField(carnum, "PAX Score");
                    _ = double.TryParse(px, out double pxScore);
                    if (pxScore == 0.0)
                    {
                        continue;
                    }
                    if (scores.ContainsKey(driverName) == false)
                    {
                        driver = new DriverData
                        {
                            number = carnum,
                            driver = ev.GetField(carnum, "Driver"),
                            firstName = ev.GetField(carnum, "First Name"),
                            lastName = ev.GetField(carnum, "Last Name"),
                            member = "N",
                            totalEvents = 1
                        };
                        if (ev.GetField(carnum, "Mbr").ToUpperInvariant().Contains("Y") == true)
                        {
                            driver.member = "Y";
                        }
                        driver.rookie = "N";
                        string rk = ev.GetField(carnum, "Rky").ToUpperInvariant();
                        if ((rk.Contains("Y") == true) || (rk.Contains("TRUE")))
                        {
                            driver.rookie = "Y";
                        }
                        scores.Add(driverName, driver);
                    }
                    else
                    {
                        // Complain about mis-spelled driver names
                        // If the first letter of the first name is different, assume it's just a re-used number
                        // by two completely different people
                        driver = scores[driverName];
                        driver.totalEvents += 1;
                        string mem = "N";
                        if (ev.GetField(carnum, "Mbr").ToUpperInvariant().Contains("Y") == true)
                        {
                            mem = "Y";
                        }
                        if (driver.member != mem)
                        {
                            TextWriter errorWriter = Console.Error;
                            errorWriter.WriteLine("Membership change for " + driverName +
                                                  " in event " + scorefile +
                                                  "( " + driver.member + " to " + mem + " )");
                        }
                        string rook = "N";
                        if (ev.GetField(carnum, "Rky").ToUpperInvariant().Contains("Y") == true)
                        {
                            rook = "Y";
                        }
                        if (driver.rookie != rook)
                        {
                            TextWriter errorWriter = Console.Error;
                            errorWriter.WriteLine("Rookie change for " + driverName +
                                                  " in event " + scorefile +
                                                  "( " + driver.rookie + " to " + rook + " )");
                        }
                    }
                    driver = scores[driverName];
                    // Copy overall scores into driver's data
                    var evScores = new EventScore();
                    string pax = ev.GetField(carnum, "PAX Score");
                    _ = double.TryParse(pax, out evScores.PAXScore);
                    string rookiepax = ev.GetField(carnum, "Rookie PAX Score");
                    _ = double.TryParse(rookiepax, out evScores.rookiePAXScore);
                    string raw = ev.GetField(carnum, "RAW Score");
                    _ = double.TryParse(raw, out evScores.RAWScore);
                    string rookieraw = ev.GetField(carnum, "Rookie RAW Score");
                    _ = double.TryParse(rookieraw, out evScores.rookieRAWScore);
                    string cones = ev.GetField(carnum, "Cones");
                    _ = int.TryParse(cones, out evScores.coneCount);

                    if (driver.overallScores.ContainsKey(eventName) == false)
                    {
                        driver.overallScores.Add(eventName, evScores);
                    }
                    // Add to total cone count for this driver
                    driver.totalCones += evScores.coneCount;

                    // Add to total DNF count for this driver
                    string runs = ev.GetField(carnum, "Day1Set1 Runs");
                    int dnfCount = Regex.Matches(runs, "DNF").Count;
                    driver.totalDNF += dnfCount;
                    runs = ev.GetField(carnum, "Day1Set2 Runs");
                    dnfCount = Regex.Matches(runs, "DNF").Count;
                    driver.totalDNF += dnfCount;


                    // multiple groups, so go through and parse each one
                    for (int grpnum = 1; grpnum < 1000; grpnum++)
                    {
                        string hdr = string.Format("Xgroup-{0}:Name", grpnum);

                        string grpname = ev.GetField(carnum, hdr);
                        if (string.IsNullOrEmpty(grpname))
                        {
                            break;
                        }
                        // Get groupScore ptr (or create one if this is the first occurrence)
                        GroupScore grpscore;
                        if (driver.groupScores.ContainsKey(grpname) == false)
                        {
                            grpscore = new GroupScore
                            {
                                groupName = grpname
                            };
                            driver.groupScores.Add(grpname, grpscore);
                        }
                        else
                        {
                            grpscore = driver.groupScores[grpname];
                        }
                        // Now create an eventScore for this event
                        var scoresThisEvent = new EventScore();
                        hdr = string.Format("Xgroup-{0}:Score", grpnum);
                        string tmpstr = ev.GetField(carnum, hdr);
                        _ = double.TryParse(tmpstr, out double tmp);
                        scoresThisEvent.groupScore = tmp;

                        hdr = string.Format("Xgroup-{0}:Rookie Score", grpnum);
                        tmpstr = ev.GetField(carnum, hdr);
                        _ = double.TryParse(tmpstr, out tmp);
                        scoresThisEvent.groupRookieScore = tmp;

                        grpscore.eventScores.Add(eventName, scoresThisEvent);

                    }
                }
            }

            return "";
        }

        /// <summary>
        /// print out the driver ranking based on each class
        /// </summary>
        /// <param name="isRookie">true to generate rookie results</param>
        /// <returns>Formatted string</returns>
        public static string ClassScores(bool isRookie)
        {
            string results = separator;

            if (isRookie == true)
            {
                results += "Rookie-only group/class scores\r\n";
            }
            else
            {
                results += "Overall group/class scores\r\n";
            }

            // First build a sorted class list
            // Sort order is given in the csv file
            var sortedClassList = new SortedDictionary<int, PaxInfo>();
            List<string> classList = classData.getKeys();
            foreach (string className in classList)
            {
                var p = new PaxInfo
                {
                    carClass = className,
                    pax = classData.GetField(className, "PAX"),
                    description = classData.GetField(className, "Description"),
                    group = classData.GetField(className, "Group"),
                    displayOrder = classData.GetField(className, "Display Order")
                };
                if (int.TryParse(p.displayOrder, out int orderVal) == false)
                {
                    results += string.Format("Display order value for class " + p.carClass + " is not a valid integer\r\n");
                }
                else
                {
                    try
                    {
                        sortedClassList.Add(orderVal, p);
                    }
                    catch
                    {
                        results += string.Format("Multiple entries for class " + p.carClass + "\r\n");
                    }
                }
            }

            // Each driver has a list of event scores, with each event possibly in a different class
            // For each class, we need to make a new driver list, with all drivers that ever ran in that class
            // If they didn't run in that class, then the event score (raw) is 0.0
            // Then sort by score totals for each driver

            foreach (KeyValuePair<int, PaxInfo> classInfo in sortedClassList)
            {
                PaxInfo curClass = classInfo.Value;
                int rank = 1;

                // make a new driver list, with all drivers that ever ran in this class
                var drvList = new List<DriverData>();
                foreach (string driverName in scores.Keys)
                {
                    DriverData driver = scores[driverName];
                    if ((isRookie == true) && (driver.rookie.ToUpperInvariant().Contains("Y") == false))
                    {
                        continue;
                    }
                    if (driver.groupScores.ContainsKey(curClass.carClass))
                    {
                        drvList.Add(driver);
                    }
                }
                // skip group if nobody competed there
                if (drvList.Count == 0)
                {
                    continue;
                }

                // Now sort drivers by their overall score in this group
                drvList
                    .Sort(delegate (
                        DriverData first,
                        DriverData next)
                    {
                        if (isRookie == true)
                        {
                            return next.groupScores[curClass.carClass].bestRookiePAX.CompareTo(first.groupScores[curClass.carClass].bestRookiePAX);
                        }
                        else
                        {
                            return next.groupScores[curClass.carClass].bestPAX.CompareTo(first.groupScores[curClass.carClass].bestPAX);
                        }
                    });

                foreach (DriverData driver in drvList)
                {
                    if ((isRookie == true) && (driver.rookie.ToUpperInvariant().Contains("Y") == false))
                    {
                        continue;
                    }
                    if ((membersOnly == true) && (driver.member.ToUpperInvariant().Contains("Y") == false))
                    {
                        continue;
                    }

                    if (rank == 1)
                    {
                        // OK -- print header
                        results += string.Format("\r\n");
                        results += string.Format("Group/Class: " + curClass.carClass + " (" + curClass.description + ")\r\n");
                        results += string.Format("{0,4} {1,-16}",
                                                 "Rank", "Driver");
                        foreach (string name in eventNames)
                        {
                            results += string.Format(" {0,10}", name);
                        }
                        results += "       Best " + numDays.ToString() + "\r\n";
                    }

                    string driverName = driver.firstName + " " + driver.lastName.Substring(0, 1);
                    if (configData.GetField("ShowLastName", "Value").Contains("Y"))
                    {
                        driverName = driver.firstName + " " + driver.lastName;
                    }

                    results += string.Format("{0,3} {1,-16}",
                                             rank++,
                                             driverName
                                            );

                    // Driver score dict has gaps -- not all events attended
                    foreach (string eventName in eventNames)
                    {
                        if (driver.groupScores[curClass.carClass].eventScores.ContainsKey(eventName) == false)
                        {
                            results += string.Format(" {0,10:#0.000}", 0.00);
                        }
                        else
                        {
                            if (isRookie == true)
                            {
                                results += string.Format(" {0,10:#0.000}", driver.groupScores[curClass.carClass].eventScores[eventName].groupRookieScore);
                            }
                            else
                            {
                                results += string.Format(" {0,10:#0.000}", driver.groupScores[curClass.carClass].eventScores[eventName].groupScore);
                            }
                        }
                    }
                    // Now the best N event total
                    if (isRookie == true)
                    {
                        results += string.Format(" {0,10:#0.000}", driver.groupScores[curClass.carClass].bestRookiePAX);
                    }
                    else
                    {
                        results += string.Format(" {0,10:#0.000}", driver.groupScores[curClass.carClass].bestPAX);
                    }
                    results += "\r\n";
                }
            }
            return results;
        }
    }
}