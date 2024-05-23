/*
 * Created by Murray Peterson
 * 
 * Compile command:
 * C:\Windows\Microsoft.NET\Framework\v3.5\Csc.exe /reference:c:\mjtiming\bin\mjcommon.dll /platform:x86 mjCmdlineScore.cs
 * You will need to copy the resulting mjCmdlineScore.exe into mjtiming\bin before it will work.
 */

// ASN trophy rules:
// 1-3 competitors = 1 trophy
// 4-6 competitors = 2 trophies
// 7-9 competitors = 3 trophies
// 1 trophy for every four additional competitors
// Basically, 3,3,3,4,4,4,4,...
using System;
using System.IO;
namespace RaceBeam
{
    class Mjscore
    {
        // ---------------------------------------------------------------------------
        public static void Usage()
        {
            Console.WriteLine("Usage: mjCmdLineScore -day1 <day1 date> -day2 <day2 date> -bestsinglerun -set1only -set2only -runtimes -rawtimes -paxtimes -teams -conecounts -classtimes -rookie -maxofficialruns <# runs> -classfile <path to class.csv file> -title <string> -path <path to event data folder> -out <file to write to>");
            Environment.Exit(0);
        }
        // ---------------------------------------------------------------------------
        public static void Main(string[] args)
        {
            // parse command line arguments
            // default to 1 day scoring, today's date
            var argblock = new ScoreArgs
            {
                EventFolder = "."  // default to current folder
            };

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
                    argblock.Title = args[i];
                }
                else if (args[i] == "-path")
                {
                    i += 1;
                    argblock.EventFolder = args[i];
                }
                else if (args[i] == "-oneday")
                {
                    argblock.BestSingleRun = true;  // for backwards compatibility
                }
                else if (args[i] == "-set1only")
                {
                    argblock.Set1Only = true;
                }
                else if (args[i] == "-set2only")
                {
                    argblock.Set2Only = true;
                }
                else if (args[i] == "-set1plusset2")
                {
                    argblock.Set1PlusSet2 = true;
                }
                else if (args[i] == "-bestsinglerun")
                {
                    argblock.BestSingleRun = true;
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
                else if (args[i] == "-out")
                {
                    i += 1;
                    argblock.OutFile = args[i];
                }
                else
                {
                    Usage();
                }
            }
            string results;
            if (argblock.Title != "")
            {
                results = argblock.Title + "\r\n";
            }
            else
            {
                if (argblock.Day2 == "")
                {
                    results = "Scores for event of " + argblock.Day1 + "\r\n";
                }
                else
                {
                    results = "Scores for two day event of " + argblock.Day1 + "and " + argblock.Day2 + "\r\n\r\n";
                }
            }
            argblock.WriteCSV = true;
            results += TextScores.TextScore(argblock);

            if (!string.IsNullOrEmpty(argblock.OutFile))
            {
                File.WriteAllText(argblock.OutFile, results);
                Console.WriteLine("Results written to %s", argblock.OutFile);
            }
            else
            {
                Console.WriteLine(results);
            }

            return;
        }
    }

}