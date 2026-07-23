using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HandballIntegration.Services
{
    public class TimePlayersSheetImportService
    {
        public List<string> ReadMatchTeamNames(string workbookPath)
        {
            using var workbook = new XLWorkbook(workbookPath);
            var worksheet = workbook.Worksheet(1);

            return worksheet.RowsUsed()
                .Select(row => row.Cell("F").GetString().Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value) && !IsMatchTeamHeader(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(2)
                .ToList();
        }

        public List<TimePlayerImportRow> ReadTimeRows(string workbookPath)
        {
            using var workbook = new XLWorkbook(workbookPath);
            var worksheet = workbook.TryGetWorksheet("Feuil1", out var namedWorksheet)
                ? namedWorksheet
                : workbook.Worksheet(2);

            var rows = new List<TimePlayerImportRow>();
            string currentTeamLabel = string.Empty;

            foreach (var row in worksheet.RowsUsed())
            {
                string playerOrHeader = row.Cell("A").GetString().Trim();
                string timeHeader = row.Cell("Y").GetString().Trim();

                if (IsTimeSectionHeader(playerOrHeader, timeHeader))
                {
                    currentTeamLabel = playerOrHeader;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(currentTeamLabel)
                    || string.IsNullOrWhiteSpace(playerOrHeader)
                    || IsIgnoredPlayerLabel(playerOrHeader))
                {
                    continue;
                }

                if (!TryReadMatchTime(row.Cell("Y"), out var matchTime))
                {
                    continue;
                }

                rows.Add(new TimePlayerImportRow
                {
                    RowNumber = row.RowNumber(),
                    TeamLabel = currentTeamLabel,
                    PlayerName = playerOrHeader,
                    MatchTime = matchTime
                });
            }

            return rows;
        }

        private static bool IsTimeSectionHeader(string columnAValue, string columnYValue)
        {
            return !string.IsNullOrWhiteSpace(columnAValue)
                && NormalizeLabel(columnYValue) == "TEMPSDEJEU";
        }

        private static bool TryReadMatchTime(IXLCell cell, out TimeSpan matchTime)
        {
            matchTime = TimeSpan.Zero;

            if (cell.IsEmpty())
            {
                return false;
            }

            if (cell.TryGetValue<double>(out var numericValue))
            {
                matchTime = TimeSpan.FromDays(numericValue);
                return true;
            }

            var raw = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (TimeSpan.TryParseExact(
                raw,
                new[] { @"h\:mm\:ss", @"hh\:mm\:ss", @"m\:ss", @"mm\:ss", "c", "g", "G" },
                CultureInfo.InvariantCulture,
                out var parsed))
            {
                matchTime = parsed;
                return true;
            }

            return TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out matchTime);
        }

        private static bool IsMatchTeamHeader(string value)
            => NormalizeLabel(value) == "NOMDELEQUIPE";

        private static bool IsIgnoredPlayerLabel(string value)
        {
            var normalized = NormalizeLabel(value);
            return normalized is "TOTAL" or "TEMPSDEJEU";
        }

        private static string NormalizeLabel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
            var buffer = new System.Text.StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    buffer.Append(char.ToUpperInvariant(character));
                }
            }

            return buffer.ToString();
        }
    }

    public sealed class TimePlayerImportRow
    {
        public int RowNumber { get; set; }
        public string TeamLabel { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public TimeSpan MatchTime { get; set; }
    }
}
