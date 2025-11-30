using CsvHelper;
using CsvHelper.Configuration;
using HanballManagerMaui.Services.CsvMappings;
using HandballManagerCore.DTO;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
namespace HanballManagerMaui.Services
{
    public class MatchFileImportService
    {
        public List<MatchFileDto> ImportFromCsv(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                Encoding = System.Text.Encoding.UTF8,
                HasHeaderRecord = true,

            });

            csv.Context.RegisterClassMap<MatchFileMap>();
            return csv.GetRecords<MatchFileDto>().ToList();
        }
    }
}
