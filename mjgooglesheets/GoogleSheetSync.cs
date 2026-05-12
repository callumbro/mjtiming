using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

namespace RaceBeam
{
    internal static class GoogleSheetSync
    {
        private const int MaxRangesPerBatch = 90;

        private static readonly object SnapshotLock = new object();
        private static string lastSnapshotSpreadsheetId = "";
        private static readonly Dictionary<string, List<List<string>>> PushSnapshots =
            new Dictionary<string, List<List<string>>>(StringComparer.Ordinal);

        /// <summary>
        /// Clears in-memory cell snapshots for <paramref name="spreadsheetId"/> so the next push
        /// rewrites every tab (full ranges). Pass null/empty to clear all spreadsheets.
        /// </summary>
        internal static void ClearPushSnapshotsForSpreadsheet(string spreadsheetId)
        {
            lock (SnapshotLock)
            {
                if (string.IsNullOrWhiteSpace(spreadsheetId))
                {
                    PushSnapshots.Clear();
                    lastSnapshotSpreadsheetId = "";
                    return;
                }

                string id = spreadsheetId.Trim();
                foreach (string key in PushSnapshots.Keys.ToList())
                {
                    if (key.StartsWith(id + "\t", StringComparison.Ordinal))
                    {
                        PushSnapshots.Remove(key);
                    }
                }
            }
        }

        internal static string TokenStorePath =>
            Path.Combine(SheetFormSettings.InstallConfigDirectory, "GoogleAuth");

        internal static string[] SheetsScopes => new[] { SheetsService.Scope.Spreadsheets };

        internal static async Task<UserCredential> AuthorizeAsync(
            string clientId,
            string clientSecret,
            string applicationName,
            IProgress<string> log,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new InvalidOperationException("Enter OAuth Client ID and Client Secret from Google Cloud Console (Desktop app).");
            }

            Directory.CreateDirectory(TokenStorePath);
            var secrets = new ClientSecrets
            {
                ClientId = clientId.Trim(),
                ClientSecret = clientSecret.Trim()
            };

            log?.Report("Opening browser to sign in with Google…");
            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                SheetsScopes,
                "user",
                cancellationToken,
                new FileDataStore(TokenStorePath, true));
        }

        internal static async Task PushGridAsync(
            UserCredential credential,
            string applicationName,
            string spreadsheetId,
            string sheetTabName,
            IList<IList<object>> rows,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(spreadsheetId))
            {
                throw new InvalidOperationException("Enter the spreadsheet ID (from the sheet URL).");
            }

            if (rows == null || rows.Count == 0)
            {
                throw new InvalidOperationException("No rows to send.");
            }

            var tab = string.IsNullOrWhiteSpace(sheetTabName) ? "Sheet1" : sheetTabName.Trim();
            int cols = 0;
            foreach (var row in rows)
            {
                if (row != null && row.Count > cols)
                {
                    cols = row.Count;
                }
            }

            if (cols == 0)
            {
                throw new InvalidOperationException("Rows have no columns.");
            }

            string endCol = ColumnLettersFromIndex(cols);
            string range = $"{EscapeSheetNameForRange(tab)}!A1:{endCol}{rows.Count}";

            var service = new SheetsService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = string.IsNullOrWhiteSpace(applicationName) ? "MJ Timing Google Sheets" : applicationName.Trim()
                });

            var body = new ValueRange { Values = rows };

            var request = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
            request.ValueInputOption =
                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes "Overall" (overall raw ranking) plus one tab per class, then an optional Statistics tab, creating tabs if missing.
        /// </summary>
        /// <param name="wipeTargetTabsBeforePush">When true, deletes every worksheet in the spreadsheet and
        /// recreates only the result tabs (used for "Force full refresh" when reusing a spreadsheet for a new event).</param>
        /// <param name="statisticsTab">Optional last worksheet with event timing statistics (same data as HTML footer).</param>
        /// <returns>Number of value ranges written; 0 if nothing was sent.</returns>
        internal static async Task<int> PushOverallAndClassSheetsAsync(
            UserCredential credential,
            string applicationName,
            string spreadsheetId,
            SheetScoreExport.SheetScoreGrid overallGrid,
            IReadOnlyList<(string Title, SheetScoreExport.SheetScoreGrid Grid)> classSheets,
            IProgress<string> log,
            CancellationToken cancellationToken,
            SheetScoreExport.SheetScoreGrid statisticsTab = null,
            bool wipeTargetTabsBeforePush = false)
        {
            if (string.IsNullOrWhiteSpace(spreadsheetId))
            {
                throw new InvalidOperationException("Enter the spreadsheet ID (from the sheet URL).");
            }

            if (overallGrid?.Rows == null || overallGrid.Rows.Count == 0)
            {
                throw new InvalidOperationException("No overall rows to send.");
            }

            var service = new SheetsService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = string.IsNullOrWhiteSpace(applicationName) ? "MJ Timing Google Sheets" : applicationName.Trim()
                });

            var work = new List<(string Title, SheetScoreExport.SheetScoreGrid Grid)>
            {
                ("Overall", overallGrid)
            };

            if (classSheets != null)
            {
                foreach ((string title, SheetScoreExport.SheetScoreGrid grid) in classSheets)
                {
                    if (grid?.Rows != null && grid.Rows.Count > 0)
                    {
                        work.Add((title, grid));
                    }
                }
            }

            if (statisticsTab?.Rows != null && statisticsTab.Rows.Count > 0)
            {
                work.Add(("Statistics", statisticsTab));
            }

            var usedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolved = new List<(string Title, IList<IList<object>> Rows, IList<IList<bool>> Bold, int RawStart, int RawCount)>();
            foreach ((string title, SheetScoreExport.SheetScoreGrid grid) in work)
            {
                string baseTitle = SanitizeGoogleSheetTitle(title);
                string unique = EnsureUniqueSheetTitle(baseTitle, usedTitles);
                usedTitles.Add(unique);
                resolved.Add((unique, grid.Rows, grid.CellBold, grid.RawValueStartColumn, grid.RawValueColumnCount));
            }

            log?.Report("Reading spreadsheet…");
            if (wipeTargetTabsBeforePush)
            {
                log?.Report("Replacing all worksheets with fresh result tabs…");
                lock (SnapshotLock)
                {
                    foreach (string key in PushSnapshots.Keys.ToList())
                    {
                        if (key.StartsWith(spreadsheetId + "\t", StringComparison.Ordinal))
                        {
                            PushSnapshots.Remove(key);
                        }
                    }
                }

                var resultTitles = resolved.Select(r => r.Title).ToList();
                await ReplaceSpreadsheetWithResultTabsOnlyAsync(
                        service,
                        spreadsheetId,
                        resultTitles,
                        log,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                Spreadsheet ss = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
                var existingTitles = new HashSet<string>(
                    ss.Sheets.Select(s => s.Properties.Title),
                    StringComparer.OrdinalIgnoreCase);

                var addRequests = new List<Request>();
                foreach (var (title, _, _, _, _) in resolved)
                {
                    if (!existingTitles.Contains(title))
                    {
                        addRequests.Add(
                            new Request
                            {
                                AddSheet = new AddSheetRequest
                                {
                                    Properties = new SheetProperties { Title = title }
                                }
                            });
                    }
                }

                if (addRequests.Count > 0)
                {
                    log?.Report($"Creating {addRequests.Count} worksheet(s)…");
                    var addBody = new BatchUpdateSpreadsheetRequest { Requests = addRequests };
                    await service.Spreadsheets
                        .BatchUpdate(addBody, spreadsheetId)
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                    // New sheets: no snapshot yet — force full write for every tab we added.
                    lock (SnapshotLock)
                    {
                        if (lastSnapshotSpreadsheetId != spreadsheetId)
                        {
                            PushSnapshots.Clear();
                            lastSnapshotSpreadsheetId = spreadsheetId;
                        }

                        foreach (var (title, _, _, _, _) in resolved)
                        {
                            PushSnapshots.Remove(spreadsheetId + "\t" + title);
                        }
                    }
                }
                else
                {
                    lock (SnapshotLock)
                    {
                        if (lastSnapshotSpreadsheetId != spreadsheetId)
                        {
                            PushSnapshots.Clear();
                            lastSnapshotSpreadsheetId = spreadsheetId;
                        }
                    }
                }
            }

            var valueRanges = new List<ValueRange>();
            foreach (var (title, rows, _, _, _) in resolved)
            {
                int cols = MaxColumnCount(rows);
                if (cols == 0)
                {
                    continue;
                }

                string escaped = EscapeSheetNameForRange(title);
                string cacheKey = spreadsheetId + "\t" + title;
                List<List<string>> previous;
                lock (SnapshotLock)
                {
                    PushSnapshots.TryGetValue(cacheKey, out previous);
                }

                var patches = BuildChangedValueRanges(escaped, rows, previous);
                if (patches.Count == 0)
                {
                    continue;
                }

                if (patches.Count > MaxRangesPerBatch)
                {
                    patches = new List<ValueRange>
                    {
                        FullSheetValueRange(escaped, rows, cols)
                    };
                }

                valueRanges.AddRange(patches);
            }

            if (valueRanges.Count > MaxRangesPerBatch)
            {
                valueRanges.Clear();
                foreach (var (title, rows, _, _, _) in resolved)
                {
                    int colsAll = MaxColumnCount(rows);
                    if (colsAll == 0)
                    {
                        continue;
                    }

                    valueRanges.Add(FullSheetValueRange(EscapeSheetNameForRange(title), rows, colsAll));
                }

                log?.Report("Many separate changes — writing full worksheets this time.");
            }

            if (valueRanges.Count == 0)
            {
                log?.Report("Google Sheets already match — no cells to update.");
                return 0;
            }

            log?.Report($"Writing {valueRanges.Count} range(s)…");
            var batchValues = new BatchUpdateValuesRequest
            {
                ValueInputOption = "USER_ENTERED",
                Data = valueRanges
            };

            await service.Spreadsheets.Values
                .BatchUpdate(batchValues, spreadsheetId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            lock (SnapshotLock)
            {
                foreach (var (title, rows, _, _, _) in resolved)
                {
                    PushSnapshots[spreadsheetId + "\t" + title] = SnapshotGrid(rows);
                }
            }

            log?.Report("Applying raw-time highlights…");
            await ApplyRawTimeHighlightsAsync(service, spreadsheetId, resolved, cancellationToken).ConfigureAwait(false);

            log?.Report("Applying alternating row backgrounds…");
            await ApplyAlternatingRowBackgroundsAsync(service, spreadsheetId, resolved, cancellationToken)
                .ConfigureAwait(false);

            log?.Report("Applying table layout (freeze header, borders)…");
            await ApplyResultsTableFormattingAsync(service, spreadsheetId, resolved, cancellationToken)
                .ConfigureAwait(false);

            log?.Report("Merging 'Raw Times' header and sizing columns…");
            await ApplyRawTimesHeaderMergeAndColumnWidthsAsync(service, spreadsheetId, resolved, cancellationToken)
                .ConfigureAwait(false);

            return valueRanges.Count;
        }

        /// <summary>
        /// Clears any sheet filter (legacy basic filter / filter views), clears conditional-format rules
        /// (e.g. legacy zebra rules), freezes the header row, and draws a grid border around the data.
        /// </summary>
        private static async Task ApplyResultsTableFormattingAsync(
            SheetsService service,
            string spreadsheetId,
            IReadOnlyList<(string Title, IList<IList<object>> Rows, IList<IList<bool>> Bold, int RawStart, int RawCount)> resolved,
            CancellationToken cancellationToken)
        {
            if (resolved == null || resolved.Count == 0)
            {
                return;
            }

            Spreadsheet ss = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            var requests = new List<Request>();

            var gridBorder = new Border
            {
                Style = "SOLID",
                Width = 1,
                Color = new Color { Red = 0.72f, Green = 0.74f, Blue = 0.78f }
            };

            foreach ((string title, IList<IList<object>> rows, _, _, _) in resolved)
            {
                if (rows == null || rows.Count == 0)
                {
                    continue;
                }

                Sheet sheet = (ss.Sheets ?? Enumerable.Empty<Sheet>()).FirstOrDefault(
                    s => string.Equals(s.Properties?.Title, title, StringComparison.OrdinalIgnoreCase));
                if (sheet?.Properties?.SheetId == null)
                {
                    continue;
                }

                int sheetId = (int)sheet.Properties.SheetId;
                int dataRows = rows.Count;
                int dataCols = MaxColumnCount(rows);
                if (dataCols < 1)
                {
                    dataCols = 1;
                }

                var tableRange = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 0,
                    EndRowIndex = dataRows,
                    StartColumnIndex = 0,
                    EndColumnIndex = dataCols
                };

                // Remove any basic filter left from older syncs (SetBasicFilter is no longer used).
                requests.Add(
                    new Request
                    {
                        ClearBasicFilter = new ClearBasicFilterRequest { SheetId = sheetId }
                    });

                IList<FilterView> filterViews = sheet.FilterViews;
                if (filterViews != null)
                {
                    foreach (FilterView fv in filterViews)
                    {
                        if (fv.FilterViewId != null)
                        {
                            requests.Add(
                                new Request
                                {
                                    DeleteFilterView = new DeleteFilterViewRequest { FilterId = fv.FilterViewId.Value }
                                });
                        }
                    }
                }

                IList<ConditionalFormatRule> existingCf = sheet.ConditionalFormats;
                if (existingCf != null)
                {
                    for (int ri = existingCf.Count - 1; ri >= 0; ri--)
                    {
                        requests.Add(
                            new Request
                            {
                                DeleteConditionalFormatRule = new DeleteConditionalFormatRuleRequest
                                {
                                    SheetId = sheetId,
                                    Index = ri
                                }
                            });
                    }
                }

                requests.Add(
                    new Request
                    {
                        UpdateBorders = new UpdateBordersRequest
                        {
                            Range = tableRange,
                            Top = gridBorder,
                            Bottom = gridBorder,
                            Left = gridBorder,
                            Right = gridBorder,
                            InnerHorizontal = gridBorder,
                            InnerVertical = gridBorder
                        }
                    });

                requests.Add(
                    new Request
                    {
                        UpdateSheetProperties = new UpdateSheetPropertiesRequest
                        {
                            Properties = new SheetProperties
                            {
                                SheetId = sheetId,
                                GridProperties = new GridProperties { FrozenRowCount = 1 }
                            },
                            Fields = "gridProperties.frozenRowCount"
                        }
                    });

                requests.Add(
                    new Request
                    {
                        RepeatCell = new RepeatCellRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = 0,
                                EndRowIndex = 1,
                                StartColumnIndex = 0,
                                EndColumnIndex = dataCols
                            },
                            Cell = new CellData
                            {
                                UserEnteredFormat = new CellFormat
                                {
                                    TextFormat = new TextFormat { Bold = true },
                                    BackgroundColor = new Color { Red = 0.93f, Green = 0.94f, Blue = 0.96f },
                                    VerticalAlignment = "MIDDLE"
                                }
                            },
                            Fields = "userEnteredFormat(textFormat.bold,backgroundColor,verticalAlignment)"
                        }
                    });
            }

            if (requests.Count == 0)
            {
                return;
            }

            await ExecuteBatchedSpreadsheetUpdatesAsync(service, spreadsheetId, requests, cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task ExecuteBatchedSpreadsheetUpdatesAsync(
            SheetsService service,
            string spreadsheetId,
            List<Request> requests,
            CancellationToken cancellationToken)
        {
            const int batchSize = 400;
            for (int off = 0; off < requests.Count; off += batchSize)
            {
                int take = Math.Min(batchSize, requests.Count - off);
                await service.Spreadsheets
                    .BatchUpdate(
                        new BatchUpdateSpreadsheetRequest { Requests = requests.GetRange(off, take) },
                        spreadsheetId)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes every worksheet, then adds <paramref name="resultTabTitles"/> in order.
        /// Google requires at least one sheet, so one sheet is kept temporarily under a throwaway name.
        /// </summary>
        private static async Task ReplaceSpreadsheetWithResultTabsOnlyAsync(
            SheetsService service,
            string spreadsheetId,
            IReadOnlyList<string> resultTabTitles,
            IProgress<string> log,
            CancellationToken cancellationToken)
        {
            if (resultTabTitles == null || resultTabTitles.Count == 0)
            {
                return;
            }

            string tempTitle;
            do
            {
                tempTitle = SanitizeGoogleSheetTitle("_MJ Temp " + Guid.NewGuid().ToString("N").Substring(0, 10));
            }
            while (resultTabTitles.Any(t => string.Equals(t, tempTitle, StringComparison.OrdinalIgnoreCase)));

            Spreadsheet ss = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            List<Sheet> sheets = ss.Sheets?.ToList() ?? new List<Sheet>();
            if (sheets.Count == 0)
            {
                throw new InvalidOperationException("Spreadsheet has no worksheets.");
            }

            var batch = new List<Request>();
            for (int i = sheets.Count - 1; i >= 1; i--)
            {
                if (sheets[i].Properties?.SheetId != null)
                {
                    batch.Add(
                        new Request
                        {
                            DeleteSheet = new DeleteSheetRequest { SheetId = (int)sheets[i].Properties.SheetId }
                        });
                }
            }

            if (batch.Count > 0)
            {
                log?.Report($"Removing {batch.Count} old worksheet(s)…");
                await ExecuteBatchedSpreadsheetUpdatesAsync(service, spreadsheetId, batch, cancellationToken)
                    .ConfigureAwait(false);
            }

            ss = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            sheets = ss.Sheets?.ToList() ?? new List<Sheet>();
            if (sheets.Count != 1 || sheets[0].Properties?.SheetId == null)
            {
                throw new InvalidOperationException(
                    "Expected a single worksheet before recreating result tabs (spreadsheet may be in an inconsistent state).");
            }

            int holdSheetId = (int)sheets[0].Properties.SheetId;
            batch.Clear();
            batch.Add(
                new Request
                {
                    UpdateSheetProperties = new UpdateSheetPropertiesRequest
                    {
                        Properties = new SheetProperties { SheetId = holdSheetId, Title = tempTitle },
                        Fields = "title"
                    }
                });
            await ExecuteBatchedSpreadsheetUpdatesAsync(service, spreadsheetId, batch, cancellationToken).ConfigureAwait(false);

            batch.Clear();
            foreach (string title in resultTabTitles)
            {
                batch.Add(
                    new Request
                    {
                        AddSheet = new AddSheetRequest
                        {
                            Properties = new SheetProperties { Title = title }
                        }
                    });
            }

            log?.Report($"Creating {batch.Count} result worksheet(s)…");
            await ExecuteBatchedSpreadsheetUpdatesAsync(service, spreadsheetId, batch, cancellationToken).ConfigureAwait(false);

            batch.Clear();
            batch.Add(new Request { DeleteSheet = new DeleteSheetRequest { SheetId = holdSheetId } });
            log?.Report("Removing temporary worksheet…");
            await ExecuteBatchedSpreadsheetUpdatesAsync(service, spreadsheetId, batch, cancellationToken).ConfigureAwait(false);
        }

        private static bool CellBoldSafe(IList<IList<bool>> bold, int rowIndex, int colIndex)
        {
            if (bold == null || rowIndex < 0 || rowIndex >= bold.Count)
            {
                return false;
            }

            IList<bool> line = bold[rowIndex];
            if (line == null || colIndex < 0 || colIndex >= line.Count)
            {
                return false;
            }

            return line[colIndex];
        }

        /// <summary>
        /// Merges row 1 header cells across all raw-time columns (label "Raw Times") and sets column widths
        /// so the block grows with the number of run columns.
        /// </summary>
        private static async Task ApplyRawTimesHeaderMergeAndColumnWidthsAsync(
            SheetsService service,
            string spreadsheetId,
            IReadOnlyList<(string Title, IList<IList<object>> Rows, IList<IList<bool>> Bold, int RawStart, int RawCount)> sheets,
            CancellationToken cancellationToken)
        {
            if (sheets == null || sheets.Count == 0)
            {
                return;
            }

            Spreadsheet ss = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            var sheetIdByTitle = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Sheet sheet in ss.Sheets ?? Enumerable.Empty<Sheet>())
            {
                if (sheet.Properties?.Title != null && sheet.Properties.SheetId != null)
                {
                    sheetIdByTitle[sheet.Properties.Title] = (int)sheet.Properties.SheetId;
                }
            }

            const int headerUnmergeMaxColExclusive = 702;
            const int pixelsPerRawTimeColumn = 78;
            var requests = new List<Request>();

            foreach ((string title, IList<IList<object>> rows, _, int rawStart, int rawCount) in sheets)
            {
                if (rows == null || rows.Count == 0 || rawCount <= 0)
                {
                    continue;
                }

                if (!sheetIdByTitle.TryGetValue(title, out int sheetId))
                {
                    continue;
                }

                int rawEndExclusive = rawStart + rawCount;

                requests.Add(
                    new Request
                    {
                        UnmergeCells = new UnmergeCellsRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = 0,
                                EndRowIndex = 1,
                                StartColumnIndex = rawStart,
                                EndColumnIndex = headerUnmergeMaxColExclusive
                            }
                        }
                    });

                if (rawCount > 1)
                {
                    requests.Add(
                        new Request
                        {
                            MergeCells = new MergeCellsRequest
                            {
                                Range = new GridRange
                                {
                                    SheetId = sheetId,
                                    StartRowIndex = 0,
                                    EndRowIndex = 1,
                                    StartColumnIndex = rawStart,
                                    EndColumnIndex = rawEndExclusive
                                },
                                MergeType = "MERGE_ROWS"
                            }
                        });
                }

                requests.Add(
                    new Request
                    {
                        UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                        {
                            Range = new DimensionRange
                            {
                                SheetId = sheetId,
                                Dimension = "COLUMNS",
                                StartIndex = rawStart,
                                EndIndex = rawEndExclusive
                            },
                            Properties = new DimensionProperties { PixelSize = pixelsPerRawTimeColumn },
                            Fields = "pixelSize"
                        }
                    });

                requests.Add(
                    new Request
                    {
                        RepeatCell = new RepeatCellRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = 0,
                                EndRowIndex = 1,
                                StartColumnIndex = rawStart,
                                EndColumnIndex = rawEndExclusive
                            },
                            Cell = new CellData
                            {
                                UserEnteredFormat = new CellFormat
                                {
                                    HorizontalAlignment = "CENTER",
                                    VerticalAlignment = "MIDDLE"
                                }
                            },
                            Fields = "userEnteredFormat(horizontalAlignment,verticalAlignment)"
                        }
                    });
            }

            if (requests.Count == 0)
            {
                return;
            }

            await ExecuteBatchedSpreadsheetUpdatesAsync(service, spreadsheetId, requests, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Alternating row fill for every data cell except best-time (bold) raw cells, which keep the highlight color.
        /// Uses cell backgrounds only (not conditional formatting) so striping does not override highlights.
        /// </summary>
        private static async Task ApplyAlternatingRowBackgroundsAsync(
            SheetsService service,
            string spreadsheetId,
            IReadOnlyList<(string Title, IList<IList<object>> Rows, IList<IList<bool>> Bold, int RawStart, int RawCount)> resolved,
            CancellationToken cancellationToken)
        {
            Spreadsheet ss = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            var sheetIdByTitle = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Sheet sheet in ss.Sheets ?? Enumerable.Empty<Sheet>())
            {
                if (sheet.Properties?.Title != null && sheet.Properties.SheetId != null)
                {
                    sheetIdByTitle[sheet.Properties.Title] = (int)sheet.Properties.SheetId;
                }
            }

            const string bgFields = "userEnteredFormat.backgroundColor";
            var zebraBg = new Color { Red = 0.96f, Green = 0.97f, Blue = 0.99f };
            var whiteBg = new Color { Red = 1f, Green = 1f, Blue = 1f };
            var requests = new List<Request>();

            foreach ((string title, IList<IList<object>> rows, IList<IList<bool>> bold, int rawStart, int rawCount) in resolved)
            {
                if (rows == null || rows.Count <= 1 || !sheetIdByTitle.TryGetValue(title, out int sheetId))
                {
                    continue;
                }

                int dataCols = MaxColumnCount(rows);
                if (dataCols < 1)
                {
                    continue;
                }

                for (int r = 1; r < rows.Count; r++)
                {
                    bool stripeRow = (r % 2) == 1;
                    Color rowBg = stripeRow ? zebraBg : whiteBg;

                    if (rawCount <= 0)
                    {
                        requests.Add(
                            new Request
                            {
                                RepeatCell = new RepeatCellRequest
                                {
                                    Range = new GridRange
                                    {
                                        SheetId = sheetId,
                                        StartRowIndex = r,
                                        EndRowIndex = r + 1,
                                        StartColumnIndex = 0,
                                        EndColumnIndex = dataCols
                                    },
                                    Cell = new CellData
                                    {
                                        UserEnteredFormat = new CellFormat { BackgroundColor = rowBg }
                                    },
                                    Fields = bgFields
                                }
                            });
                        continue;
                    }

                    int rawEnd = rawStart + rawCount;

                    if (rawStart > 0)
                    {
                        requests.Add(
                            new Request
                            {
                                RepeatCell = new RepeatCellRequest
                                {
                                    Range = new GridRange
                                    {
                                        SheetId = sheetId,
                                        StartRowIndex = r,
                                        EndRowIndex = r + 1,
                                        StartColumnIndex = 0,
                                        EndColumnIndex = rawStart
                                    },
                                    Cell = new CellData
                                    {
                                        UserEnteredFormat = new CellFormat { BackgroundColor = rowBg }
                                    },
                                    Fields = bgFields
                                }
                            });
                    }

                    int cc = rawStart;
                    while (cc < rawEnd)
                    {
                        if (CellBoldSafe(bold, r, cc))
                        {
                            cc++;
                            continue;
                        }

                        int seg0 = cc;
                        while (cc < rawEnd && !CellBoldSafe(bold, r, cc))
                        {
                            cc++;
                        }

                        requests.Add(
                            new Request
                            {
                                RepeatCell = new RepeatCellRequest
                                {
                                    Range = new GridRange
                                    {
                                        SheetId = sheetId,
                                        StartRowIndex = r,
                                        EndRowIndex = r + 1,
                                        StartColumnIndex = seg0,
                                        EndColumnIndex = cc
                                    },
                                    Cell = new CellData
                                    {
                                        UserEnteredFormat = new CellFormat { BackgroundColor = rowBg }
                                    },
                                    Fields = bgFields
                                }
                            });
                    }

                    if (rawEnd < dataCols)
                    {
                        requests.Add(
                            new Request
                            {
                                RepeatCell = new RepeatCellRequest
                                {
                                    Range = new GridRange
                                    {
                                        SheetId = sheetId,
                                        StartRowIndex = r,
                                        EndRowIndex = r + 1,
                                        StartColumnIndex = rawEnd,
                                        EndColumnIndex = dataCols
                                    },
                                    Cell = new CellData
                                    {
                                        UserEnteredFormat = new CellFormat { BackgroundColor = rowBg }
                                    },
                                    Fields = bgFields
                                }
                            });
                    }
                }
            }

            if (requests.Count == 0)
            {
                return;
            }

            await ExecuteBatchedSpreadsheetUpdatesAsync(service, spreadsheetId, requests, cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task ApplyRawTimeHighlightsAsync(
            SheetsService service,
            string spreadsheetId,
            IReadOnlyList<(string Title, IList<IList<object>> Rows, IList<IList<bool>> Bold, int RawStart, int RawCount)> sheets,
            CancellationToken cancellationToken)
        {
            Spreadsheet ss = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            var sheetIdByTitle = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Sheet sheet in ss.Sheets ?? Enumerable.Empty<Sheet>())
            {
                if (sheet.Properties?.Title != null && sheet.Properties.SheetId != null)
                {
                    sheetIdByTitle[sheet.Properties.Title] = (int)sheet.Properties.SheetId;
                }
            }

            var requests = new List<Request>();
            foreach ((string title, IList<IList<object>> rows, IList<IList<bool>> bold, int rawStart, int rawCount) in sheets)
            {
                if (rows == null || rows.Count <= 1 || rawCount <= 0)
                {
                    continue;
                }

                if (!sheetIdByTitle.TryGetValue(title, out int sheetId))
                {
                    continue;
                }

                int lastRowExclusive = rows.Count;
                int rawEndExclusive = rawStart + rawCount;
                requests.Add(
                    new Request
                    {
                        RepeatCell = new RepeatCellRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = 1,
                                EndRowIndex = lastRowExclusive,
                                StartColumnIndex = rawStart,
                                EndColumnIndex = rawEndExclusive
                            },
                            Cell = new CellData
                            {
                                UserEnteredFormat = new CellFormat
                                {
                                    TextFormat = new TextFormat { Bold = false },
                                    BackgroundColor = new Color { Red = 1f, Green = 1f, Blue = 1f }
                                }
                            },
                            Fields = "userEnteredFormat(textFormat.bold,backgroundColor)"
                        }
                    });

                for (int r = 1; r < rows.Count; r++)
                {
                    for (int c = rawStart; c < rawEndExclusive; c++)
                    {
                        if (!CellBoldSafe(bold, r, c))
                        {
                            continue;
                        }

                        requests.Add(
                            new Request
                            {
                                RepeatCell = new RepeatCellRequest
                                {
                                    Range = new GridRange
                                    {
                                        SheetId = sheetId,
                                        StartRowIndex = r,
                                        EndRowIndex = r + 1,
                                        StartColumnIndex = c,
                                        EndColumnIndex = c + 1
                                    },
                                    Cell = new CellData
                                    {
                                        UserEnteredFormat = new CellFormat
                                        {
                                            TextFormat = new TextFormat { Bold = true },
                                            BackgroundColor = new Color { Red = 1f, Green = 0.95f, Blue = 0.55f }
                                        }
                                    },
                                    Fields = "userEnteredFormat(textFormat.bold,backgroundColor)"
                                }
                            });
                    }
                }
            }

            if (requests.Count == 0)
            {
                return;
            }

            const int maxReq = 400;
            for (int i = 0; i < requests.Count; i += maxReq)
            {
                int take = Math.Min(maxReq, requests.Count - i);
                var chunk = requests.GetRange(i, take);
                var body = new BatchUpdateSpreadsheetRequest { Requests = chunk };
                await service.Spreadsheets.BatchUpdate(body, spreadsheetId)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private static ValueRange FullSheetValueRange(string escapedTitle, IList<IList<object>> rows, int cols)
        {
            string endCol = ColumnLettersFromIndex(cols);
            string range = $"{escapedTitle}!A1:{endCol}{rows.Count}";
            return new ValueRange
            {
                Range = range,
                MajorDimension = "ROWS",
                Values = new List<IList<object>>(rows)
            };
        }

        /// <summary>
        /// Builds minimal A1 ranges that cover only cells whose values differ from <paramref name="previous"/>.
        /// </summary>
        private static List<ValueRange> BuildChangedValueRanges(
            string escapedTitle,
            IList<IList<object>> newRows,
            List<List<string>> previous)
        {
            int colsNew = MaxColumnCount(newRows);
            int nNew = newRows?.Count ?? 0;
            if (colsNew == 0 || nNew == 0)
            {
                return new List<ValueRange>();
            }

            if (previous == null || previous.Count == 0)
            {
                return new List<ValueRange> { FullSheetValueRange(escapedTitle, newRows, colsNew) };
            }

            int colsOld = 0;
            foreach (var row in previous)
            {
                if (row != null && row.Count > colsOld)
                {
                    colsOld = row.Count;
                }
            }

            int nOld = previous.Count;
            int cols = Math.Max(colsNew, colsOld);
            if (colsNew != colsOld)
            {
                return new List<ValueRange> { FullSheetValueRange(escapedTitle, newRows, colsNew) };
            }

            if (cols == 0)
            {
                cols = colsNew;
            }
            var changedRows = new Dictionary<int, (int C0, int C1)>();

            void markCell(int r, int c)
            {
                if (!changedRows.TryGetValue(r, out var span))
                {
                    changedRows[r] = (c, c);
                }
                else
                {
                    changedRows[r] = (Math.Min(span.C0, c), Math.Max(span.C1, c));
                }
            }

            int nMax = Math.Max(nNew, nOld);
            for (int r = 0; r < nMax; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    string oldS = GetSnapshotCell(previous, r, c, nOld, colsOld);
                    string newS = GetNewCellString(newRows, r, c, nNew, colsNew);
                    if (oldS != newS)
                    {
                        markCell(r, c);
                    }
                }
            }

            if (changedRows.Count == 0)
            {
                return new List<ValueRange>();
            }

            var orderedRows = changedRows.Keys.OrderBy(x => x).ToList();
            var rects = new List<(int R0, int R1, int C0, int C1)>();
            int idx = 0;
            while (idx < orderedRows.Count)
            {
                int r0 = orderedRows[idx];
                (int c0, int c1) = changedRows[r0];
                int r1 = r0;
                int j = idx + 1;
                while (j < orderedRows.Count &&
                       orderedRows[j] == r1 + 1 &&
                       changedRows[orderedRows[j]].C0 == c0 &&
                       changedRows[orderedRows[j]].C1 == c1)
                {
                    r1 = orderedRows[j];
                    j++;
                }

                rects.Add((r0, r1, c0, c1));
                idx = j;
            }

            var result = new List<ValueRange>();
            foreach (var (r0, r1, c0, c1) in rects)
            {
                var patch = new List<IList<object>>();
                for (int r = r0; r <= r1; r++)
                {
                    var line = new List<object>();
                    for (int c = c0; c <= c1; c++)
                    {
                        if (r < nNew && newRows[r] != null && c < newRows[r].Count)
                        {
                            line.Add(newRows[r][c] ?? "");
                        }
                        else
                        {
                            line.Add("");
                        }
                    }

                    patch.Add(line);
                }

                string c0Letters = ColumnLettersFromIndex(c0 + 1);
                string c1Letters = ColumnLettersFromIndex(c1 + 1);
                string range = $"{escapedTitle}!{c0Letters}{r0 + 1}:{c1Letters}{r1 + 1}";
                result.Add(
                    new ValueRange
                    {
                        Range = range,
                        MajorDimension = "ROWS",
                        Values = patch
                    });
            }

            return result;
        }

        private static string GetSnapshotCell(
            List<List<string>> previous,
            int r,
            int c,
            int nOld,
            int colsOld)
        {
            if (r < 0 || r >= nOld || previous[r] == null)
            {
                return "";
            }

            IList<string> row = previous[r];
            if (c < 0 || c >= row.Count)
            {
                return "";
            }

            return row[c] ?? "";
        }

        private static string GetNewCellString(
            IList<IList<object>> newRows,
            int r,
            int c,
            int nNew,
            int colsNew)
        {
            if (r < 0 || r >= nNew || newRows[r] == null)
            {
                return "";
            }

            IList<object> row = newRows[r];
            if (c < 0 || c >= row.Count)
            {
                return "";
            }

            return CellComparableString(row[c]);
        }

        private static List<List<string>> SnapshotGrid(IList<IList<object>> rows)
        {
            var snap = new List<List<string>>();
            if (rows == null)
            {
                return snap;
            }

            foreach (var row in rows)
            {
                var line = new List<string>();
                if (row != null)
                {
                    foreach (object cell in row)
                    {
                        line.Add(CellComparableString(cell));
                    }
                }

                snap.Add(line);
            }

            return snap;
        }

        private static string CellComparableString(object v)
        {
            if (v == null)
            {
                return "";
            }

            switch (v)
            {
                case double d:
                    return d.ToString("G17", CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString("G17", CultureInfo.InvariantCulture);
                case decimal m:
                    return m.ToString(CultureInfo.InvariantCulture);
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);
                default:
                    return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
            }
        }

        internal static string SanitizeGoogleSheetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return "Sheet";
            }

            var sb = new StringBuilder();
            foreach (char c in title.Trim())
            {
                if (c == '\\' || c == '/' || c == '?' || c == '*' || c == '[' || c == ']' || c == ':')
                {
                    sb.Append(' ');
                }
                else if (c < 32)
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(c);
                }
            }

            string s = sb.ToString().Trim();
            while (s.Contains("  ", StringComparison.Ordinal))
            {
                s = s.Replace("  ", " ", StringComparison.Ordinal);
            }

            if (s.Length > 100)
            {
                s = s.Substring(0, 100);
            }

            return s.Length == 0 ? "Sheet" : s;
        }

        private static string EnsureUniqueSheetTitle(string baseTitle, HashSet<string> used)
        {
            string t = baseTitle;
            int i = 2;
            while (used.Contains(t))
            {
                string suffix = " (" + i + ")";
                int maxBase = 100 - suffix.Length;
                if (maxBase < 1)
                {
                    maxBase = 1;
                }

                t = baseTitle.Length <= maxBase ? baseTitle.Substring(0, Math.Min(baseTitle.Length, maxBase)) + suffix : baseTitle.Substring(0, maxBase) + suffix;
                i++;
            }

            return t;
        }

        private static int MaxColumnCount(IList<IList<object>> rows)
        {
            int cols = 0;
            foreach (var row in rows)
            {
                if (row != null && row.Count > cols)
                {
                    cols = row.Count;
                }
            }

            return cols;
        }

        private static string EscapeSheetNameForRange(string name)
        {
            if (Regex.IsMatch(name, @"^[A-Za-z0-9_]+$"))
            {
                return name;
            }

            return "'" + name.Replace("'", "''", StringComparison.Ordinal) + "'";
        }

        /// <summary>1-based column index to A, B, … AA.</summary>
        private static string ColumnLettersFromIndex(int columnCount)
        {
            int n = columnCount;
            string s = "";
            while (n > 0)
            {
                n--;
                s = (char)('A' + (n % 26)) + s;
                n /= 26;
            }

            return s;
        }
    }
}
