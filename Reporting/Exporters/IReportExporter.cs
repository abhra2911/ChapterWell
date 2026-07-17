namespace Lib_Mgmt.Reporting.Exporters
{
    public interface IReportExporter
    {
        string FormatName { get; }
        string ContentType { get; }
        string FileExtension { get; }
        byte[] Export(IEnumerable<string> headers, IEnumerable<IEnumerable<object>> rows);
    }
}