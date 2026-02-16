using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Reviews;

namespace TechTaskReview.Infrastructure.Export;

public class QuestPdfExportService : IPdfExportService
{
    public Task<byte[]> ExportReviewAsync(CodeReview review, string candidateName, CancellationToken ct)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Text($"Code Review Report — {candidateName}")
                    .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Text($"Overall Score: {review.WeightedTotalScore}/10")
                        .FontSize(16).Bold();
                    col.Item().Text($"AI Provider: {review.AIProviderName} ({review.AIModelName})")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Version: {review.Version}")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Processing Time: {review.ProcessingDuration}")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(review.Summary))
                    {
                        col.Item().PaddingTop(15).Text("Summary").FontSize(14).Bold();
                        col.Item().Text(review.Summary).FontSize(10);
                    }

                    col.Item().PaddingTop(15).Text("Category Scores").FontSize(14).Bold();

                    foreach (var score in review.CategoryScores.OrderByDescending(s => s.Score))
                    {
                        col.Item().PaddingTop(8).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Column(scoreCol =>
                        {
                            scoreCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text(FormatCategory(score.Category.ToString())).FontSize(11).Bold();
                                row.ConstantItem(60).AlignRight().Text($"{score.Score}/10").FontSize(11).Bold()
                                    .FontColor(score.Score >= 7 ? Colors.Green.Darken2 :
                                               score.Score >= 5 ? Colors.Orange.Darken2 :
                                               Colors.Red.Darken2);
                            });
                            scoreCol.Item().Text(score.Justification).FontSize(9);

                            if (score.CriticalIssues.Length > 0)
                            {
                                scoreCol.Item().PaddingTop(4).Text("Critical Issues:").FontSize(9).Bold().FontColor(Colors.Red.Darken2);
                                foreach (var issue in score.CriticalIssues)
                                    scoreCol.Item().Text($"  - {issue}").FontSize(8);
                            }

                            if (score.RefactorSuggestions.Length > 0)
                            {
                                scoreCol.Item().PaddingTop(4).Text("Suggestions:").FontSize(9).Bold();
                                foreach (var sug in score.RefactorSuggestions)
                                    scoreCol.Item().Text($"  - {sug}").FontSize(8);
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("TechTaskReview — Generated ").FontSize(8);
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC")).FontSize(8);
                });
            });
        });

        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);
    }

    private static string FormatCategory(string name)
        => string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}
