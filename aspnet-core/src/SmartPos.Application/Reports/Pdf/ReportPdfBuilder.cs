using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SmartPos.Reports.Pdf
{
    public class ReportPdfBuilder : IReportPdfBuilder
    {
        public byte[] Build(ReportPdfModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily(Fonts.Lato));

                    page.Header().Element(c => ComposeHeader(c, model));
                    page.Content().Element(c => ComposeContent(c, model));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return doc.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, ReportPdfModel model)
        {
            container.PaddingBottom(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(titleCol =>
                    {
                        titleCol.Item().Text(model.Title ?? "Report")
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);

                        var metaParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(model.DateRange))
                        {
                            metaParts.Add($"Period: {model.DateRange}");
                        }
                        if (!string.IsNullOrWhiteSpace(model.BranchName))
                        {
                            metaParts.Add($"Branch: {model.BranchName}");
                        }
                        if (!string.IsNullOrWhiteSpace(model.SearchKeyword))
                        {
                            metaParts.Add($"Filter: \"{model.SearchKeyword}\"");
                        }

                        if (metaParts.Count > 0)
                        {
                            titleCol.Item().PaddingTop(2).Text(string.Join("  |  ", metaParts))
                                .FontSize(8.5f)
                                .FontColor(Colors.Grey.Darken1);
                        }
                    });

                    row.ConstantItem(180).AlignRight().Column(timeCol =>
                    {
                        timeCol.Item().Text($"Generated: {model.GeneratedAt:yyyy-MM-dd HH:mm}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                    });
                });

                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private void ComposeContent(IContainer container, ReportPdfModel model)
        {
            container.Column(col =>
            {
                // Status banner if present (e.g. accounting equation)
                if (!string.IsNullOrWhiteSpace(model.StatusBanner))
                {
                    col.Item().PaddingBottom(8).Background(Colors.Grey.Lighten4).Padding(6).Row(r =>
                    {
                        r.RelativeItem().Text(model.StatusBanner).Bold().FontSize(8.5f).FontColor(Colors.Blue.Darken2);
                    });
                }

                // Summary cards / items if present
                if (model.SummaryItems != null && model.SummaryItems.Count > 0)
                {
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        foreach (var item in model.SummaryItems)
                        {
                            row.RelativeItem().PaddingRight(6).Background(Colors.Grey.Lighten4).Padding(6).Column(stat =>
                            {
                                stat.Item().Text(item.Key).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                                stat.Item().Text(item.Value).Bold().FontSize(10).FontColor(Colors.Grey.Darken3);
                            });
                        }
                    });
                }

                // If sections are defined (e.g., Balance Sheet)
                if (model.Sections != null && model.Sections.Count > 0)
                {
                    foreach (var section in model.Sections)
                    {
                        col.Item().PaddingTop(6).PaddingBottom(4).Text(section.Title)
                            .Bold()
                            .FontSize(11)
                            .FontColor(Colors.Blue.Darken3);

                        col.Item().Element(c => RenderTable(c, section.Columns, section.Rows));

                        if (!string.IsNullOrWhiteSpace(section.TotalLabel) || !string.IsNullOrWhiteSpace(section.TotalValue))
                        {
                            col.Item().PaddingTop(3).PaddingBottom(6).AlignRight().Text(
                                $"{section.TotalLabel ?? "Total"}: {section.TotalValue ?? "0.00"}"
                            ).Bold().FontSize(9.5f).FontColor(Colors.Blue.Darken3);
                        }
                    }
                }
                else if (model.Columns != null && model.Columns.Count > 0)
                {
                    // Main table
                    col.Item().Element(c => RenderTable(c, model.Columns, model.Rows, model.FooterRows));
                }
            });
        }

        private void RenderTable(IContainer container, List<ReportPdfColumn> columns, List<string[]> rows, List<string[]> footerRows = null)
        {
            if (columns == null || columns.Count == 0)
            {
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(def =>
                {
                    foreach (var col in columns)
                    {
                        def.RelativeColumn(col.RelativeWidth > 0 ? col.RelativeWidth : 1f);
                    }
                });

                // Header repeats on every page
                table.Header(header =>
                {
                    foreach (var col in columns)
                    {
                        var cell = header.Cell()
                            .Background(Colors.Blue.Darken3)
                            .PaddingVertical(4)
                            .PaddingHorizontal(4)
                            .BorderBottom(1)
                            .BorderColor(Colors.Blue.Darken4);

                        if (col.Alignment == ReportColumnAlignment.Right)
                        {
                            cell.AlignRight().Text(col.Header ?? "").Bold().FontSize(8f).FontColor(Colors.White);
                        }
                        else if (col.Alignment == ReportColumnAlignment.Center)
                        {
                            cell.AlignCenter().Text(col.Header ?? "").Bold().FontSize(8f).FontColor(Colors.White);
                        }
                        else
                        {
                            cell.AlignLeft().Text(col.Header ?? "").Bold().FontSize(8f).FontColor(Colors.White);
                        }
                    }
                });

                // Rows
                if (rows != null && rows.Count > 0)
                {
                    for (int r = 0; r < rows.Count; r++)
                    {
                        var row = rows[r];
                        var bg = r % 2 == 1 ? Colors.Grey.Lighten4 : Colors.White;

                        for (int c = 0; c < columns.Count; c++)
                        {
                            var col = columns[c];
                            var textVal = (row != null && c < row.Length) ? (row[c] ?? "—") : "—";

                            var cell = table.Cell()
                                .Background(bg)
                                .PaddingVertical(3)
                                .PaddingHorizontal(4)
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2);

                            if (col.Alignment == ReportColumnAlignment.Right)
                            {
                                cell.AlignRight().Text(textVal).FontSize(8f);
                            }
                            else if (col.Alignment == ReportColumnAlignment.Center)
                            {
                                cell.AlignCenter().Text(textVal).FontSize(8f);
                            }
                            else
                            {
                                cell.AlignLeft().Text(textVal).FontSize(8f);
                            }
                        }
                    }
                }
                else
                {
                    table.Cell().ColumnSpan((uint)columns.Count).Padding(12).AlignCenter().Text("No records found").Italic().FontSize(8.5f).FontColor(Colors.Grey.Medium);
                }

                // Footer rows if present
                if (footerRows != null && footerRows.Count > 0)
                {
                    foreach (var footRow in footerRows)
                    {
                        for (int c = 0; c < columns.Count; c++)
                        {
                            var col = columns[c];
                            var textVal = (footRow != null && c < footRow.Length) ? (footRow[c] ?? "") : "";

                            var cell = table.Cell()
                                .Background(Colors.Grey.Lighten3)
                                .PaddingVertical(4)
                                .PaddingHorizontal(4)
                                .BorderTop(1)
                                .BorderColor(Colors.Grey.Medium);

                            if (col.Alignment == ReportColumnAlignment.Right)
                            {
                                cell.AlignRight().Text(textVal).Bold().FontSize(8.5f);
                            }
                            else if (col.Alignment == ReportColumnAlignment.Center)
                            {
                                cell.AlignCenter().Text(textVal).Bold().FontSize(8.5f);
                            }
                            else
                            {
                                cell.AlignLeft().Text(textVal).Bold().FontSize(8.5f);
                            }
                        }
                    }
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(8).Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("SmartPos System").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        text.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        text.Span(" of ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        text.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        }
    }
}
