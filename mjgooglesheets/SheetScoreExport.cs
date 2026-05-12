/*
 * Google Sheets score grid builders (columns aligned with htmlScores rawTimes / classTimes tables).
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceBeam
{
    public static class SheetScoreExport
    {
    #region Sheet export (columns aligned with rawTimes / classTimes HTML tables)

    /// <summary>Values for one worksheet plus bold highlight on raw run cells (Google Sheets).</summary>
    public sealed class SheetScoreGrid
    {
        public SheetScoreGrid(
            IList<IList<object>> rows,
            IList<IList<bool>> cellBold,
            int rawValueStartColumn,
            int rawValueColumnCount)
        {
            Rows = rows;
            CellBold = cellBold;
            RawValueStartColumn = rawValueStartColumn;
            RawValueColumnCount = rawValueColumnCount;
        }

        public IList<IList<object>> Rows { get; }
        public IList<IList<bool>> CellBold { get; }
        public int RawValueStartColumn { get; }
        public int RawValueColumnCount { get; }
    }

    private static int SheetOfficialRunSlotCount(scoreArgs args)
    {
        int m = args.maxOfficialRuns;
        if (m < 1)
        {
            m = 1;
        }

        if (m > 24)
        {
            m = 24;
        }

        return m;
    }

    /// <summary>
    /// Highest 1-based official run slot index used in <paramref name="setData"/> (0 if no official runs).
    /// </summary>
    private static int CountOfficialSlotsUsedInSet(
        scoreArgs args,
        scoreCalcs.singleSetData setData,
        int slotCap)
    {
        if (setData.runs == null || setData.runs.Count == 0)
        {
            return 0;
        }

        int startRunNumber = setData.runs[0].runNumber;
        int maxIdx = -1;
        foreach (scoreCalcs.singleRunData run in setData.runs)
        {
            int idx = run.runNumber - startRunNumber;
            if (idx < 0 || idx >= args.maxOfficialRuns || idx >= slotCap)
            {
                continue;
            }

            maxIdx = Math.Max(maxIdx, idx);
        }

        return maxIdx < 0 ? 0 : maxIdx + 1;
    }

    private static int ComputeMaxOfficialSlotsUsedForSet(
        scoreArgs args,
        IEnumerable<scoreCalcs.driverScoreData> drivers,
        Func<scoreCalcs.driverScoreData, scoreCalcs.singleSetData> pickSet,
        int slotCap)
    {
        int m = 0;
        foreach (scoreCalcs.driverScoreData d in drivers)
        {
            if (d == null)
            {
                continue;
            }

            m = Math.Max(m, CountOfficialSlotsUsedInSet(args, pickSet(d), slotCap));
        }

        return m;
    }

    /// <summary>
    /// Per-set slot counts and total raw columns for the given drivers (matches appended raw cells).
    /// </summary>
    private static void ComputeRawSlotLayout(
        scoreArgs args,
        IEnumerable<scoreCalcs.driverScoreData> drivers,
        out int slotsSet1,
        out int slotsSet2,
        out int rawColumnCount)
    {
        int per = SheetOfficialRunSlotCount(args);
        int m1 = ComputeMaxOfficialSlotsUsedForSet(args, drivers, d => d.Day1Set1, per);
        int m2 = ComputeMaxOfficialSlotsUsedForSet(args, drivers, d => d.Day1Set2, per);
        if (args.set1PlusSet2)
        {
            slotsSet1 = m1;
            slotsSet2 = m2;
            if (slotsSet1 == 0 && slotsSet2 == 0)
            {
                slotsSet1 = 1;
            }

            rawColumnCount = slotsSet1 + slotsSet2;
        }
        else if (args.set2Only)
        {
            slotsSet1 = 0;
            slotsSet2 = Math.Max(1, m2);
            rawColumnCount = slotsSet2;
        }
        else
        {
            slotsSet1 = Math.Max(1, m1);
            slotsSet2 = 0;
            rawColumnCount = slotsSet1;
        }
    }

    private static int SheetFixedColumnCountBeforeRaw(scoreArgs args, bool includeScoreColumn)
    {
        int n = 4;
        if (args.set1PlusSet2)
        {
            n += 3;
        }
        else
        {
            n += 1;
        }

        n += 2;
        if (includeScoreColumn)
        {
            n += 1;
        }

        return n;
    }

    private static void AddMergedRawTimesHeaderPlaceholders(int rawColumnCount, IList<object> headerRow, IList<bool> headerBold)
    {
        if (rawColumnCount <= 0)
        {
            return;
        }

        headerRow.Add("Raw Times");
        headerBold.Add(false);
        for (int i = 1; i < rawColumnCount; i++)
        {
            headerRow.Add("");
            headerBold.Add(false);
        }
    }

    private static string FormatSingleRunForPlainSheet(scoreCalcs.singleRunData run)
    {
        if (run.penalty == "DNF")
        {
            if (run.time >= scoreCalcs.DNFvalue)
            {
                return "DNS";
            }

            return run.time.ToString("#0.000") + "+DNF";
        }

        if (run.penalty == "")
        {
            return run.time.ToString("#0.000");
        }

        return run.time.ToString("#0.000") + "+" + run.penalty;
    }

    private static void AppendSheetRawTimeCellsForOneSet(
        scoreArgs args,
        scoreCalcs.singleSetData setData,
        int slotCount,
        List<object> row,
        List<bool> boldRow)
    {
        var texts = new string[slotCount];
        var bolds = new bool[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            texts[i] = "";
            bolds[i] = false;
        }

        if (setData.runs == null || setData.runs.Count == 0)
        {
            texts[0] = "DNS";
        }
        else
        {
            int startRunNumber = setData.runs[0].runNumber;
            foreach (scoreCalcs.singleRunData run in setData.runs)
            {
                int idx = run.runNumber - startRunNumber;
                if (idx < 0 || idx >= args.maxOfficialRuns || idx >= slotCount)
                {
                    continue;
                }

                texts[idx] = FormatSingleRunForPlainSheet(run);
                bolds[idx] = setData.bestRun != null && ReferenceEquals(run, setData.bestRun);
            }
        }

        for (int i = 0; i < slotCount; i++)
        {
            row.Add(texts[i]);
            boldRow.Add(bolds[i]);
        }
    }

    private static void AppendSheetRawTimeCells(
        scoreArgs args,
        scoreCalcs.singleSetData set1,
        scoreCalcs.singleSetData set2,
        int slotsSet1,
        int slotsSet2,
        List<object> row,
        List<bool> boldRow)
    {
        if (args.set1PlusSet2)
        {
            AppendSheetRawTimeCellsForOneSet(args, set1, slotsSet1, row, boldRow);
            AppendSheetRawTimeCellsForOneSet(args, set2, slotsSet2, row, boldRow);
        }
        else if (args.set2Only)
        {
            AppendSheetRawTimeCellsForOneSet(args, set2, slotsSet2, row, boldRow);
        }
        else
        {
            AppendSheetRawTimeCellsForOneSet(args, set1, slotsSet1, row, boldRow);
        }
    }

    private static void FillOverallHeaderCells(scoreArgs args, int rawColumnCount, List<object> h, List<bool> bold)
    {
        h.Add("Rank");
        bold.Add(false);
        h.Add("Class");
        bold.Add(false);
        h.Add("Driver");
        bold.Add(false);
        h.Add("Car");
        bold.Add(false);
        if (args.set1PlusSet2)
        {
            h.Add("Set 1 Best Time");
            bold.Add(false);
            h.Add("Set 2 Best Time");
            bold.Add(false);
            h.Add("Total Time");
            bold.Add(false);
        }
        else
        {
            h.Add("Best Time");
            bold.Add(false);
        }

        h.Add("Diff.");
        bold.Add(false);
        h.Add("Diff. Prev.");
        bold.Add(false);
        AddMergedRawTimesHeaderPlaceholders(rawColumnCount, h, bold);
    }

    private static void FillClassHeaderCells(scoreArgs args, int rawColumnCount, List<object> h, List<bool> bold)
    {
        h.Add("Rank");
        bold.Add(false);
        h.Add("Class");
        bold.Add(false);
        h.Add("Driver");
        bold.Add(false);
        h.Add("Car");
        bold.Add(false);
        if (args.set1PlusSet2)
        {
            h.Add("Set 1 Best Time");
            bold.Add(false);
            h.Add("Set 2 Best Time");
            bold.Add(false);
            h.Add("Total Time");
            bold.Add(false);
        }
        else
        {
            h.Add("Best Time");
            bold.Add(false);
        }

        h.Add("Diff.");
        bold.Add(false);
        h.Add("Diff. Prev.");
        bold.Add(false);
        h.Add("Score");
        bold.Add(false);
        AddMergedRawTimesHeaderPlaceholders(rawColumnCount, h, bold);
    }

    private static void AppendPaddedSheetRow(
        List<IList<object>> rows,
        List<IList<bool>> bold,
        int padToColumns,
        IReadOnlyList<object> leadingCells)
    {
        var cells = new List<object>();
        var b = new List<bool>();
        if (leadingCells != null)
        {
            foreach (object o in leadingCells)
            {
                cells.Add(o ?? "");
                b.Add(false);
            }
        }

        while (cells.Count < padToColumns)
        {
            cells.Add("");
            b.Add(false);
        }

        rows.Add(cells);
        bold.Add(b);
    }

    /// <summary>
    /// One worksheet: event timing statistics (same content as the HTML footer <see cref="Statistics"/> block).
    /// </summary>
    public static SheetScoreGrid SheetStatisticsTab(scoreCalcs.statsDataClass stats)
    {
        const int pad = 10;
        var rows = new List<IList<object>>();
        var bold = new List<IList<bool>>();

        void addRow(IReadOnlyList<object> cells)
        {
            AppendPaddedSheetRow(rows, bold, pad, cells);
        }

        addRow(new List<object> { "Statistics" });
        addRow(null);

        if (stats == null)
        {
            addRow(new List<object> { "(Statistics not available.)" });
            return new SheetScoreGrid(rows, bold, 0, 0);
        }

        int blocks = 0;

        void appendSet(string setTitle, string firstRun, string lastRun, int numRuns, double totalTime)
        {
            blocks++;
            addRow(new List<object> { setTitle });
            object avg = numRuns > 0 ? (object)(totalTime / numRuns).ToString("#0.000") : "";
            addRow(
                new List<object>
                {
                    "First run:",
                    firstRun ?? "",
                    "Last run:",
                    lastRun ?? "",
                    "Number of runs:",
                    numRuns,
                    "Average run time:",
                    avg
                });
        }

        scoreCalcs.dailyStats d1 = stats.day1;
        if (d1 != null)
        {
            if (d1.set1NumberOfRuns > 0)
            {
                appendSet("Day 1, Set 1", d1.set1TimeOfFirstRun, d1.set1TimeOfLastRun, d1.set1NumberOfRuns, d1.set1TotalTime);
            }

            if (d1.set2NumberOfRuns > 0)
            {
                appendSet("Day 1, Set 2", d1.set2TimeOfFirstRun, d1.set2TimeOfLastRun, d1.set2NumberOfRuns, d1.set2TotalTime);
            }

            if (d1.set3NumberOfRuns > 0)
            {
                appendSet("Day 1, Fun Runs (Set 3)", d1.set3TimeOfFirstRun, d1.set3TimeOfLastRun, d1.set3NumberOfRuns, d1.set3TotalTime);
            }
        }

        scoreCalcs.dailyStats d2 = stats.day2;
        if (d2 != null)
        {
            if (d2.set1NumberOfRuns > 0)
            {
                appendSet("Day 2, Set 1", d2.set1TimeOfFirstRun, d2.set1TimeOfLastRun, d2.set1NumberOfRuns, d2.set1TotalTime);
            }

            if (d2.set2NumberOfRuns > 0)
            {
                appendSet("Day 2, Set 2", d2.set2TimeOfFirstRun, d2.set2TimeOfLastRun, d2.set2NumberOfRuns, d2.set2TotalTime);
            }

            if (d2.set3NumberOfRuns > 0)
            {
                appendSet("Day 2, Fun Runs (Set 3)", d2.set3TimeOfFirstRun, d2.set3TimeOfLastRun, d2.set3NumberOfRuns, d2.set3TotalTime);
            }
        }

        if (blocks == 0)
        {
            addRow(new List<object> { "No timing sets with runs recorded yet." });
        }

        return new SheetScoreGrid(rows, bold, 0, 0);
    }

    /// <summary>
    /// Overall ranking table: same columns as <see cref="rawTimes"/> (Rank … raw runs in separate columns).
    /// </summary>
    public static SheetScoreGrid SheetOverallRankingRows(
        scoreArgs args,
        Dictionary<string, scoreCalcs.driverScoreData> scores,
        bool showLastName)
    {
        int rawStart = SheetFixedColumnCountBeforeRaw(args, includeScoreColumn: false);
        ComputeRawSlotLayout(args, scores.Values, out int slotsS1, out int slotsS2, out int rawCount);
        var rows = new List<IList<object>>();
        var bold = new List<IList<bool>>();
        var headerRow = new List<object>();
        var headerBold = new List<bool>();
        FillOverallHeaderCells(args, rawCount, headerRow, headerBold);
        rows.Add(headerRow);
        bold.Add(headerBold);

        var myList = new List<KeyValuePair<string, scoreCalcs.driverScoreData>>(scores);
        myList.Sort(
            delegate (
                KeyValuePair<string, scoreCalcs.driverScoreData> firstPair,
                KeyValuePair<string, scoreCalcs.driverScoreData> nextPair)
            {
                if (args.set1Only)
                {
                    return firstPair.Value.Day1Set1.bestRAW.CompareTo(nextPair.Value.Day1Set1.bestRAW);
                }

                if (args.set2Only)
                {
                    return firstPair.Value.Day1Set2.bestRAW.CompareTo(nextPair.Value.Day1Set2.bestRAW);
                }

                if (args.set1PlusSet2)
                {
                    double bestSet1PlusSet2 = firstPair.Value.Day1Set1.bestRAW + firstPair.Value.Day1Set2.bestRAW;
                    double nextSet1PlusSet2 = nextPair.Value.Day1Set1.bestRAW + nextPair.Value.Day1Set2.bestRAW;
                    return bestSet1PlusSet2.CompareTo(nextSet1PlusSet2);
                }

                return firstPair.Value.scoreData.bestRAW.CompareTo(nextPair.Value.scoreData.bestRAW);
            });

        int rank = 1;
        double classLeadTime = 0.0;
        double classPrevTime = 0.0;
        foreach (KeyValuePair<string, scoreCalcs.driverScoreData> driver in myList)
        {
            if (rank == 1)
            {
                if (args.set1Only)
                {
                    classLeadTime = driver.Value.Day1Set1.bestRAW;
                    classPrevTime = driver.Value.Day1Set1.bestRAW;
                }
                else if (args.set2Only)
                {
                    classLeadTime = driver.Value.Day1Set2.bestRAW;
                    classPrevTime = driver.Value.Day1Set2.bestRAW;
                }
                else if (args.set1PlusSet2)
                {
                    classLeadTime = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                    classPrevTime = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                }
                else
                {
                    classLeadTime = driver.Value.scoreData.bestRAW;
                    classPrevTime = driver.Value.scoreData.bestRAW;
                }
            }

            string driverName = driver.Value.firstName + " " + driver.Value.lastName.Substring(0, 1);
            if (showLastName)
            {
                driverName = driver.Value.firstName + " " + driver.Value.lastName;
            }

            string driverBestRAW;
            string driverDiff = "";
            string driverDiffPrev = "";
            if (args.set1Only)
            {
                if (driver.Value.Day1Set1.bestRAW >= scoreCalcs.DNFvalue)
                {
                    driverBestRAW = "DNS";
                }
                else
                {
                    driverBestRAW = driver.Value.Day1Set1.bestRAW.ToString("#0.000");
                }

                driverDiff = (driver.Value.Day1Set1.bestRAW - classLeadTime).ToString("#0.000");
                driverDiffPrev = (driver.Value.Day1Set1.bestRAW - classPrevTime).ToString("#0.000");
            }
            else if (args.set2Only)
            {
                if (driver.Value.Day1Set2.bestRAW >= scoreCalcs.DNFvalue)
                {
                    driverBestRAW = "DNS";
                }
                else
                {
                    driverBestRAW = driver.Value.Day1Set2.bestRAW.ToString("#0.000");
                }

                driverDiff = (driver.Value.Day1Set2.bestRAW - classLeadTime).ToString("#0.000");
                driverDiffPrev = (driver.Value.Day1Set2.bestRAW - classPrevTime).ToString("#0.000");
            }
            else if (args.set1PlusSet2)
            {
                if (driver.Value.Day1Set1.bestRAW >= scoreCalcs.DNFvalue ||
                    driver.Value.Day1Set2.bestRAW >= scoreCalcs.DNFvalue)
                {
                    driverBestRAW = "DNS";
                }
                else
                {
                    driverBestRAW = (driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW).ToString("#0.000");
                }

                double bestSet1PlusSet2 = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                driverDiff = (bestSet1PlusSet2 - classLeadTime).ToString("#0.000");
                driverDiffPrev = (bestSet1PlusSet2 - classPrevTime).ToString("#0.000");
            }
            else
            {
                driverBestRAW = driver.Value.scoreData.bestRAW.ToString("#0.000");
                driverDiff = (driver.Value.scoreData.bestRAW - classLeadTime).ToString("#0.000");
                driverDiffPrev = (driver.Value.scoreData.bestRAW - classPrevTime).ToString("#0.000");
            }

            var row = new List<object> { rank, driver.Value.carClass, driverName, driver.Value.carDescription };
            if (args.set1PlusSet2)
            {
                row.Add(driver.Value.Day1Set1.bestRAW.ToString("#0.000"));
                row.Add(driver.Value.Day1Set2.bestRAW.ToString("#0.000"));
                row.Add(driverBestRAW);
            }
            else
            {
                row.Add(driverBestRAW);
            }

            row.Add(driverDiff);
            row.Add(driverDiffPrev);
            var boldRow = new List<bool>();
            for (int i = 0; i < row.Count; i++)
            {
                boldRow.Add(false);
            }

            AppendSheetRawTimeCells(args, driver.Value.Day1Set1, driver.Value.Day1Set2, slotsS1, slotsS2, row, boldRow);
            rows.Add(row);
            bold.Add(boldRow);

            if (args.set1Only)
            {
                classPrevTime = driver.Value.Day1Set1.bestRAW;
            }
            else if (args.set2Only)
            {
                classPrevTime = driver.Value.Day1Set2.bestRAW;
            }
            else if (args.set1PlusSet2)
            {
                classPrevTime = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
            }
            else
            {
                classPrevTime = driver.Value.scoreData.bestRAW;
            }

            rank++;
        }

        return new SheetScoreGrid(rows, bold, rawStart, rawCount);
    }

    /// <summary>
    /// One worksheet per class: same columns as <see cref="classTimes"/> (includes Score before raw run columns).
    /// </summary>
    public static List<(string TabTitle, SheetScoreGrid Grid)> SheetClassRankingTabs(
        scoreArgs args,
        Dictionary<string, scoreCalcs.driverScoreData> scores,
        SortedDictionary<int, scoreCalcs.paxInfo> sortedClassList,
        bool showLastName)
    {
        var tabs = new List<(string TabTitle, SheetScoreGrid Grid)>();
        if (sortedClassList == null)
        {
            return tabs;
        }

        int rawStart = SheetFixedColumnCountBeforeRaw(args, includeScoreColumn: true);

        var myList = new List<KeyValuePair<string, scoreCalcs.driverScoreData>>(scores);
        myList.Sort(
            delegate (
                KeyValuePair<string, scoreCalcs.driverScoreData> firstPair,
                KeyValuePair<string, scoreCalcs.driverScoreData> nextPair)
            {
                if (args.set1Only)
                {
                    return firstPair.Value.Day1Set1.bestRAW.CompareTo(nextPair.Value.Day1Set1.bestRAW);
                }

                if (args.set2Only)
                {
                    return firstPair.Value.Day1Set2.bestRAW.CompareTo(nextPair.Value.Day1Set2.bestRAW);
                }

                if (args.set1PlusSet2)
                {
                    double bestSet1PlusSet2 = firstPair.Value.Day1Set1.bestRAW + firstPair.Value.Day1Set2.bestRAW;
                    double nextSet1PlusSet2 = nextPair.Value.Day1Set1.bestRAW + nextPair.Value.Day1Set2.bestRAW;
                    return bestSet1PlusSet2.CompareTo(nextSet1PlusSet2);
                }

                return firstPair.Value.scoreData.bestRAW.CompareTo(nextPair.Value.scoreData.bestRAW);
            });

        foreach (KeyValuePair<int, scoreCalcs.paxInfo> classInfo in sortedClassList)
        {
            scoreCalcs.paxInfo curClass = classInfo.Value;
            if (!myList.Any(o => o.Value.carClass == curClass.carClass))
            {
                continue;
            }

            var classDrivers = new List<scoreCalcs.driverScoreData>();
            foreach (KeyValuePair<string, scoreCalcs.driverScoreData> kv in myList)
            {
                if (kv.Value.carClass == curClass.carClass)
                {
                    classDrivers.Add(kv.Value);
                }
            }

            ComputeRawSlotLayout(args, classDrivers, out int slotsS1, out int slotsS2, out int rawCount);

            var rows = new List<IList<object>>();
            var boldRows = new List<IList<bool>>();
            var headerRow = new List<object>();
            var headerBold = new List<bool>();
            FillClassHeaderCells(args, rawCount, headerRow, headerBold);
            rows.Add(headerRow);
            boldRows.Add(headerBold);

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
                    if (args.set1Only)
                    {
                        classLeadTime = driver.Value.Day1Set1.bestRAW;
                        classPrevTime = driver.Value.Day1Set1.bestRAW;
                    }
                    else if (args.set2Only)
                    {
                        classLeadTime = driver.Value.Day1Set2.bestRAW;
                        classPrevTime = driver.Value.Day1Set2.bestRAW;
                    }
                    else if (args.set1PlusSet2)
                    {
                        classLeadTime = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                        classPrevTime = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                    }
                    else
                    {
                        classLeadTime = driver.Value.scoreData.bestRAW;
                        classPrevTime = driver.Value.scoreData.bestRAW;
                    }
                }

                string driverName = driver.Value.firstName + " " + driver.Value.lastName.Substring(0, 1);
                if (showLastName)
                {
                    driverName = driver.Value.firstName + " " + driver.Value.lastName;
                }

                string driverBestRAW;
                string driverDiff = "";
                string driverDiffPrev = "";
                if (args.set1Only)
                {
                    if (driver.Value.Day1Set1.bestRAW >= scoreCalcs.DNFvalue)
                    {
                        driverBestRAW = "DNS";
                    }
                    else
                    {
                        driverBestRAW = driver.Value.Day1Set1.bestRAW.ToString("#0.000");
                    }

                    driverDiff = (driver.Value.Day1Set1.bestRAW - classLeadTime).ToString("#0.000");
                    driverDiffPrev = (driver.Value.Day1Set1.bestRAW - classPrevTime).ToString("#0.000");
                }
                else if (args.set2Only)
                {
                    if (driver.Value.Day1Set2.bestRAW >= scoreCalcs.DNFvalue)
                    {
                        driverBestRAW = "DNS";
                    }
                    else
                    {
                        driverBestRAW = driver.Value.Day1Set2.bestRAW.ToString("#0.000");
                    }

                    driverDiff = (driver.Value.Day1Set2.bestRAW - classLeadTime).ToString("#0.000");
                    driverDiffPrev = (driver.Value.Day1Set2.bestRAW - classPrevTime).ToString("#0.000");
                }
                else if (args.set1PlusSet2)
                {
                    if (driver.Value.Day1Set1.bestRAW >= scoreCalcs.DNFvalue ||
                        driver.Value.Day1Set2.bestRAW >= scoreCalcs.DNFvalue)
                    {
                        driverBestRAW = "DNS";
                    }
                    else
                    {
                        driverBestRAW = (driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW).ToString("#0.000");
                    }

                    double bestSet1PlusSet2 = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                    driverDiff = (bestSet1PlusSet2 - classLeadTime).ToString("#0.000");
                    driverDiffPrev = (bestSet1PlusSet2 - classPrevTime).ToString("#0.000");
                }
                else
                {
                    driverBestRAW = driver.Value.scoreData.bestRAW.ToString("#0.000");
                    driverDiff = (driver.Value.scoreData.bestRAW - classLeadTime).ToString("#0.000");
                    driverDiffPrev = (driver.Value.scoreData.bestRAW - classPrevTime).ToString("#0.000");
                }

                var row = new List<object>
                {
                    grpPtr.groupRank,
                    driver.Value.carClass,
                    driverName,
                    driver.Value.carDescription
                };
                if (args.set1PlusSet2)
                {
                    row.Add(driver.Value.Day1Set1.bestRAW.ToString("#0.000"));
                    row.Add(driver.Value.Day1Set2.bestRAW.ToString("#0.000"));
                    row.Add(driverBestRAW);
                }
                else
                {
                    row.Add(driverBestRAW);
                }

                row.Add(driverDiff);
                row.Add(driverDiffPrev);
                row.Add(grpPtr.groupScore.ToString("#0.000"));
                var boldRow = new List<bool>();
                for (int i = 0; i < row.Count; i++)
                {
                    boldRow.Add(false);
                }

                AppendSheetRawTimeCells(args, driver.Value.Day1Set1, driver.Value.Day1Set2, slotsS1, slotsS2, row, boldRow);
                rows.Add(row);
                boldRows.Add(boldRow);

                if (args.set1Only)
                {
                    classPrevTime = driver.Value.Day1Set1.bestRAW;
                }
                else if (args.set2Only)
                {
                    classPrevTime = driver.Value.Day1Set2.bestRAW;
                }
                else if (args.set1PlusSet2)
                {
                    classPrevTime = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                }
                else
                {
                    classPrevTime = driver.Value.scoreData.bestRAW;
                }
            }

            string tabTitle = SheetTabTitleForClass(curClass.carClass, curClass.description);
            tabs.Add((tabTitle, new SheetScoreGrid(rows, boldRows, rawStart, rawCount)));
        }

        AppendUnknownClassSheetTab(args, myList, sortedClassList, showLastName, rawStart, tabs);

        return tabs;
    }

    /// <summary>
    /// Drivers whose <see cref="scoreCalcs.driverScoreData.carClass"/> is not listed in the pax/class file
    /// (typos, missing class rows, or blank class). Same columns as other class sheets.
    /// </summary>
    private static void AppendUnknownClassSheetTab(
        scoreArgs args,
        List<KeyValuePair<string, scoreCalcs.driverScoreData>> myListSortedByRaw,
        SortedDictionary<int, scoreCalcs.paxInfo> sortedClassList,
        bool showLastName,
        int rawStart,
        List<(string TabTitle, SheetScoreGrid Grid)> tabs)
    {
        if (sortedClassList == null || sortedClassList.Count == 0)
        {
            return;
        }

        var unknownDrivers = new List<KeyValuePair<string, scoreCalcs.driverScoreData>>();
        foreach (KeyValuePair<string, scoreCalcs.driverScoreData> kv in myListSortedByRaw)
        {
            if (IsCarClassInPaxList(kv.Value.carClass, sortedClassList) == false)
            {
                unknownDrivers.Add(kv);
            }
        }

        if (unknownDrivers.Count == 0)
        {
            return;
        }

        List<scoreCalcs.driverScoreData> unknownData = unknownDrivers.Select(kv => kv.Value).ToList();
        ComputeRawSlotLayout(args, unknownData, out int slotsS1, out int slotsS2, out int rawCount);

        var rows = new List<IList<object>>();
        var boldRows = new List<IList<bool>>();
        var headerRow = new List<object>();
        var headerBold = new List<bool>();
        FillClassHeaderCells(args, rawCount, headerRow, headerBold);
        rows.Add(headerRow);
        boldRows.Add(headerBold);

        double classLeadTime = 0.0;
        double classPrevTime = 0.0;
        int rank = 1;
        foreach (KeyValuePair<string, scoreCalcs.driverScoreData> driver in unknownDrivers)
        {
            if (rank == 1)
            {
                if (args.set1Only)
                {
                    classLeadTime = driver.Value.Day1Set1.bestRAW;
                    classPrevTime = driver.Value.Day1Set1.bestRAW;
                }
                else if (args.set2Only)
                {
                    classLeadTime = driver.Value.Day1Set2.bestRAW;
                    classPrevTime = driver.Value.Day1Set2.bestRAW;
                }
                else if (args.set1PlusSet2)
                {
                    classLeadTime = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                    classPrevTime = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                }
                else
                {
                    classLeadTime = driver.Value.scoreData.bestRAW;
                    classPrevTime = driver.Value.scoreData.bestRAW;
                }
            }

            string driverName = driver.Value.firstName + " " + driver.Value.lastName.Substring(0, 1);
            if (showLastName)
            {
                driverName = driver.Value.firstName + " " + driver.Value.lastName;
            }

            string driverBestRAW;
            string driverDiff = "";
            string driverDiffPrev = "";
            if (args.set1Only)
            {
                if (driver.Value.Day1Set1.bestRAW >= scoreCalcs.DNFvalue)
                {
                    driverBestRAW = "DNS";
                }
                else
                {
                    driverBestRAW = driver.Value.Day1Set1.bestRAW.ToString("#0.000");
                }

                driverDiff = (driver.Value.Day1Set1.bestRAW - classLeadTime).ToString("#0.000");
                driverDiffPrev = (driver.Value.Day1Set1.bestRAW - classPrevTime).ToString("#0.000");
            }
            else if (args.set2Only)
            {
                if (driver.Value.Day1Set2.bestRAW >= scoreCalcs.DNFvalue)
                {
                    driverBestRAW = "DNS";
                }
                else
                {
                    driverBestRAW = driver.Value.Day1Set2.bestRAW.ToString("#0.000");
                }

                driverDiff = (driver.Value.Day1Set2.bestRAW - classLeadTime).ToString("#0.000");
                driverDiffPrev = (driver.Value.Day1Set2.bestRAW - classPrevTime).ToString("#0.000");
            }
            else if (args.set1PlusSet2)
            {
                if (driver.Value.Day1Set1.bestRAW >= scoreCalcs.DNFvalue ||
                    driver.Value.Day1Set2.bestRAW >= scoreCalcs.DNFvalue)
                {
                    driverBestRAW = "DNS";
                }
                else
                {
                    driverBestRAW = (driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW).ToString("#0.000");
                }

                double bestSet1PlusSet2 = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
                driverDiff = (bestSet1PlusSet2 - classLeadTime).ToString("#0.000");
                driverDiffPrev = (bestSet1PlusSet2 - classPrevTime).ToString("#0.000");
            }
            else
            {
                driverBestRAW = driver.Value.scoreData.bestRAW.ToString("#0.000");
                driverDiff = (driver.Value.scoreData.bestRAW - classLeadTime).ToString("#0.000");
                driverDiffPrev = (driver.Value.scoreData.bestRAW - classPrevTime).ToString("#0.000");
            }

            string scoreCell;
            if (driver.Value.scoreData.bestPAX >= scoreCalcs.DNFvalue)
            {
                scoreCell = "";
            }
            else
            {
                scoreCell = driver.Value.scoreData.PAXscore.ToString("#0.000");
            }

            var row = new List<object>
            {
                rank,
                string.IsNullOrWhiteSpace(driver.Value.carClass) ? "(blank)" : driver.Value.carClass,
                driverName,
                driver.Value.carDescription
            };
            if (args.set1PlusSet2)
            {
                row.Add(driver.Value.Day1Set1.bestRAW.ToString("#0.000"));
                row.Add(driver.Value.Day1Set2.bestRAW.ToString("#0.000"));
                row.Add(driverBestRAW);
            }
            else
            {
                row.Add(driverBestRAW);
            }

            row.Add(driverDiff);
            row.Add(driverDiffPrev);
            row.Add(scoreCell);
            var boldRow = new List<bool>();
            for (int i = 0; i < row.Count; i++)
            {
                boldRow.Add(false);
            }

            AppendSheetRawTimeCells(args, driver.Value.Day1Set1, driver.Value.Day1Set2, slotsS1, slotsS2, row, boldRow);
            rows.Add(row);
            boldRows.Add(boldRow);

            if (args.set1Only)
            {
                classPrevTime = driver.Value.Day1Set1.bestRAW;
            }
            else if (args.set2Only)
            {
                classPrevTime = driver.Value.Day1Set2.bestRAW;
            }
            else if (args.set1PlusSet2)
            {
                classPrevTime = driver.Value.Day1Set1.bestRAW + driver.Value.Day1Set2.bestRAW;
            }
            else
            {
                classPrevTime = driver.Value.scoreData.bestRAW;
            }

            rank++;
        }

        string unknownTabTitle = SheetTabTitleForClass("Unknown", "Not in class list");
        tabs.Add((unknownTabTitle, new SheetScoreGrid(rows, boldRows, rawStart, rawCount)));
    }

    private static bool IsCarClassInPaxList(string carClass, SortedDictionary<int, scoreCalcs.paxInfo> sortedClassList)
    {
        if (string.IsNullOrWhiteSpace(carClass))
        {
            return false;
        }

        string c = carClass.Trim();
        foreach (scoreCalcs.paxInfo pi in sortedClassList.Values)
        {
            if (string.Equals(pi.carClass, c, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string SheetTabTitleForClass(string carClass, string description)
    {
        string d = string.IsNullOrEmpty(description) ? "" : " — " + description;
        string s = carClass + d;
        if (s.Length > 99)
        {
            s = s.Substring(0, 99);
        }

        return s;
    }

    #endregion

    }
}
