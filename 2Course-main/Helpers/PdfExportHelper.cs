using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace _2course.Helpers
{
    public static class PdfExportHelper
    {
        public static void ExportToPdf(string filePath, string articleTitle, string articleText, bool isOfficial = false)
        {
            // Лицензия Community (бесплатно для учебных и некоммерческих проектов)
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40, Unit.Point);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Segoe UI"));

                    // === ЗАГОЛОВОК ===
                    page.Header().Column(col =>
                    {
                        // Название статьи
                        col.Item().Text(articleTitle)
                            .Bold()
                            .FontSize(20)
                            .FontColor(Colors.Blue.Darken2)
                            .AlignCenter();

                        // Разделительная линия
                        col.Item().PaddingVertical(10).BorderBottom(2).BorderColor(Colors.Blue.Lighten2);

                        // Дисклеймер (только для не-официальных версий)
                        if (!isOfficial)
                        {
                            col.Item().PaddingBottom(5).Text("⚠ Документ содержит пользовательские изменения и не является официальной публикацией.")
                                .FontSize(9)
                                .FontColor(Colors.Red.Darken1)
                                .Italic()
                                .AlignCenter();
                        }
                    });

                    // === ТЕКСТ СТАТЬИ ===
                    page.Content()
                        .PaddingTop(10)
                        .Text(articleText);

                    // === ПОДВАЛ (ФУТЕР) ===
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium));

                        text.Span(isOfficial ? "📘 Официальная выписка • " : "📝 Рабочая версия • ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }
    }
}