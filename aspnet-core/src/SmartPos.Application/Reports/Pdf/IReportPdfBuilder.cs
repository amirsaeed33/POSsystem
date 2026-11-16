using Abp.Dependency;

namespace SmartPos.Reports.Pdf
{
    public interface IReportPdfBuilder : ITransientDependency
    {
        byte[] Build(ReportPdfModel model);
    }
}
