using System.Text;

namespace Lib_Mgmt.Reporting.Exporters
{
    public class CsvReportExporter : IReportExporter
    {
        public string FormatName => "CSV";
        public string ContentType => "text/csv";
        public string FileExtension => "csv";

        public byte[] Export(IEnumerable<string> headers, IEnumerable<IEnumerable<object>> rows)
        {
            var sb = new StringBuilder();
            if (headers != null && headers.Any())
                sb.AppendLine(string.Join(",", headers.Select(Escape)));
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", row.Select(Escape)));
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string Escape(object value)
        {
            var s = value?.ToString() ?? "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                s = "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}