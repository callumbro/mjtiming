// Compile command:
// C:\Windows\Microsoft.NET\Framework\v3.5\Csc.exe /reference:c:\mjtiming\bin\mjcommon.dll /platform:x86 sample.cs
// Note that the resulting sample.exe will not work unless it is copied into mjtiming\bin

using System;
using System.Collections.Generic;

namespace RaceBeam  // this is required to get easy reference to the datatypes
{
    class PaxScore
    {
        // returned list of all driver's score data
        public static Dictionary<string, ScoreCalcs.DriverScoreData> scores;
        // Returned list of team scores
        public static List<ScoreCalcs.TeamData> teamScores;
        // Returned list of all class data, sorted by sort order given in file
        public static SortedDictionary<int, ScoreCalcs.PaxInfo> sortedClassList;
        // Returned statistics
        public static ScoreCalcs.StatsDataClass stats;

        public static void Usage()
        {
            Console.WriteLine("Usage: score <event date>\n");
            Environment.Exit(0);
        }

        public static void Main(string[] args)
        {
            // default to 1 day scoring, today's date


            var argblock = new ScoreArgs();     // parameters passed to the scoring module
            string day1Name = DateTime.Now.ToString("yyyy_MM_dd");
            argblock.Day1 = day1Name;
            argblock.Set1Only = true;
            // Where the config file resides
            argblock.ConfigFolder = "C:\\mjtiming\\config";
            // this overrides the eventfolder specified in the config file
            argblock.EventFolder = "C:\\mjtiming\\eventdata";

            // Do the scoring calcs
            string err = ScoreCalcs
                .DoScore(
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
            string results = PaxTimes(argblock);
            Console.WriteLine(results);
        }

        /// <summary>
        /// Return a printable text string for PAX data
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string PaxTimes(ScoreArgs args)
        {
            string results = "";
            // List of all drivers and their score data
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
            string hdr = string.Format("{0,4} {1,4} {2,3} {3,3} {4,5} {5,-16} {6,-22} {7,8}  {8,5} {9,9} {10,7}\r\n",
                "Rank", "Car#", "Mbr", "Rky", "Class", "Driver", "Car", "Raw Time", "PAX #", "PAX Time", "Score");
            results += hdr;

            foreach (KeyValuePair<string, ScoreCalcs.DriverScoreData> driver in myList)
            {

                results += string.Format("{0,4} {1,4} {2,3} {3,3} {4,5} {5,-16} {6,-22} {7,8:#.000}  {8,5:#0.000} {9,9:#.000} {10,7:#0.000}\r\n",
                    driver.Value.ScoreData.PAXrank,
                    driver.Value.Number,
                    driver.Value.Member,
                    driver.Value.Rookie ? "Y" : "N",
                    driver.Value.CarClass,
                    driver.Value.FirstName + " " + driver.Value.LastName.Substring(0, 1),
                    driver.Value.CarDescription,
                    driver.Value.ScoreData.BestRAW < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestRAW.ToString("#.000") : "DNS",
                    driver.Value.Pax,
                    driver.Value.ScoreData.BestPAX < ScoreCalcs.DNFvalue ? driver.Value.ScoreData.BestPAX.ToString("#.000") : "DNS",
                    driver.Value.ScoreData.PAXscore
                );
            }
            return results;
        }
    }
}