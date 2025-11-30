using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace HandballIntegration.Converters
{
    public class XlsxToCsvConverter
    {
        public string ConvertXlsxToCsv(string xlsxPath)
        {
            string csvPath = Path.ChangeExtension(xlsxPath, ".csv");

            using var workbook = new XLWorkbook(xlsxPath);
            var ws = workbook.Worksheet(1);

            using var writer = new StreamWriter(csvPath);

            bool firstRow = true;

            foreach (var row in ws.RowsUsed())
            {
                var values = row.Cells().Select(c => c.Value.ToString());

                string line = string.Join(";", values);
                writer.WriteLine(line);

                firstRow = false;
            }

            return csvPath;
        }
    }
}
