using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NimPulse.Core.Health;

/// <summary>
/// Rendert eine Report-Tabelle (dieselben Buckets/Spalten wie Reports.razor) als PDF — bewusst nur
/// eine Tabelle für v1, kein Chart-Rendering im PDF.
/// </summary>
public static class ReportPdfService
{
    public static byte[] Generate(string userDisplayName, string type, ReportPeriod period, List<ReportBucket> buckets)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("NimPulse — Bericht").FontSize(18).Bold();
                    column.Item().Text($"{HealthTypeCatalog.GetDisplayName(type)} · {PeriodLabel(period)} · {userDisplayName}").FontSize(11);
                    column.Item().PaddingTop(4).LineHorizontal(1);
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var title in new[] { "Zeitraum", "Anzahl", "Summe", "Ø", "Min", "Max" })
                        {
                            header.Cell().Element(HeaderCell).Text(title).Bold();
                        }
                    });

                    foreach (var bucket in buckets)
                    {
                        table.Cell().Element(BodyCell).Text(bucket.BucketStart.ToLocalTime().ToString("dd.MM.yyyy"));
                        table.Cell().Element(BodyCell).Text(bucket.Count.ToString());
                        table.Cell().Element(BodyCell).Text(bucket.Sum.ToString("0.##"));
                        table.Cell().Element(BodyCell).Text(bucket.Average.ToString("0.##"));
                        table.Cell().Element(BodyCell).Text(bucket.Min.ToString("0.##"));
                        table.Cell().Element(BodyCell).Text(bucket.Max.ToString("0.##"));
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generiert am ").FontSize(8);
                    text.Span(DateTimeOffset.Now.ToLocalTime().ToString("dd.MM.yyyy HH:mm")).FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1).PaddingVertical(4).PaddingHorizontal(2);

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).PaddingVertical(3).PaddingHorizontal(2);

    private static string PeriodLabel(ReportPeriod period) => period switch
    {
        ReportPeriod.Day => "Tag",
        ReportPeriod.Week => "Woche",
        ReportPeriod.Month => "Monat",
        _ => period.ToString(),
    };
}
