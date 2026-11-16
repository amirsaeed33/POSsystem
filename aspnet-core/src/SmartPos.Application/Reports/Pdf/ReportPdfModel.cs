using System;
using System.Collections.Generic;

namespace SmartPos.Reports.Pdf
{
    public enum ReportColumnAlignment
    {
        Left,
        Center,
        Right
    }

    public class ReportPdfColumn
    {
        public string Header { get; set; }
        public float RelativeWidth { get; set; } = 1f;
        public ReportColumnAlignment Alignment { get; set; } = ReportColumnAlignment.Left;

        public ReportPdfColumn() { }

        public ReportPdfColumn(string header, float relativeWidth = 1f, ReportColumnAlignment alignment = ReportColumnAlignment.Left)
        {
            Header = header;
            RelativeWidth = relativeWidth;
            Alignment = alignment;
        }
    }

    public class ReportPdfSection
    {
        public string Title { get; set; }
        public List<ReportPdfColumn> Columns { get; set; } = new List<ReportPdfColumn>();
        public List<string[]> Rows { get; set; } = new List<string[]>();
        public string TotalLabel { get; set; }
        public string TotalValue { get; set; }
    }

    public class ReportPdfModel
    {
        public string Title { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string DateRange { get; set; }
        public string BranchName { get; set; }
        public string SearchKeyword { get; set; }
        public List<KeyValuePair<string, string>> SummaryItems { get; set; } = new List<KeyValuePair<string, string>>();
        public List<ReportPdfColumn> Columns { get; set; } = new List<ReportPdfColumn>();
        public List<string[]> Rows { get; set; } = new List<string[]>();
        public List<string[]> FooterRows { get; set; } = new List<string[]>();
        public List<ReportPdfSection> Sections { get; set; } = new List<ReportPdfSection>();
        public string StatusBanner { get; set; }
    }
}
