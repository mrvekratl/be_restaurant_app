using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using RestaurantApp.Common.Models;
using RestaurantApp.Services.Interfaces;
using RestaurantApp.Web.Models;

namespace RestaurantApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportController : Controller
    {
        private readonly ISaleRepository _saleRepository;

        public ReportController(ISaleRepository saleRepository)
        {
            _saleRepository = saleRepository;
        }

        public async Task<IActionResult> DailyReport()
        {
            var sales = await _saleRepository.GetDailySalesAsync();
            var viewModel = new ReportViewModel
            {
                ReportTitle = "Daily Sales Report",
                ReportDate = DateTime.Today,
                Sales = sales.ToList()
            };
            return View("Report", viewModel);
        }

        public async Task<IActionResult> WeeklyReport()
        {
            var sales = await _saleRepository.GetWeeklySalesAsync();
            var viewModel = new ReportViewModel
            {
                ReportTitle = "Weekly Sales Report",
                ReportDate = DateTime.Today,
                Sales = sales.ToList()
            };
            return View("Report", viewModel);
        }

        public async Task<IActionResult> MonthlyReport()
        {
            var sales = await _saleRepository.GetMonthlySalesAsync();
            var viewModel = new ReportViewModel
            {
                ReportTitle = "Monthly Sales Report",
                ReportDate = DateTime.Today,
                Sales = sales.ToList()
            };
            return View("Report", viewModel);
        }

        public async Task<IActionResult> ExportToExcel(string reportType)
        {
            IEnumerable<Sale> sales = null;

            switch (reportType?.ToLower())
            {
                case "weekly":
                    sales = await _saleRepository.GetWeeklySalesAsync();
                    break;
                case "monthly":
                    sales = await _saleRepository.GetMonthlySalesAsync();
                    break;
                case "daily":
                default:
                    sales = await _saleRepository.GetDailySalesAsync();
                    break;
            }

            var stream = new MemoryStream();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add($"{reportType ?? "Daily"} Sales");

                worksheet.Cells[1, 1].Value = "Sale Date";
                worksheet.Cells[1, 2].Value = "Total Price";
                worksheet.Cells[1, 3].Value = "Product Name";
                worksheet.Cells[1, 4].Value = "Quantity";
                worksheet.Cells[1, 5].Value = "Product Price";
                worksheet.Cells[1, 6].Value = "Total Product Price";

                int row = 2;
                foreach (var sale in sales)
                {
                    int col = 1;
                    worksheet.Cells[row, col++].Value = sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells[row, col++].Value = sale.TotalPrice;
                    foreach (var item in sale.SaleProducts)
                    {
                        worksheet.Cells[row, col++].Value = item.Product.Name;
                        worksheet.Cells[row, col++].Value = item.Quantity;
                        worksheet.Cells[row, col++].Value = item.ProductPrice;
                        worksheet.Cells[row, col++].Value = item.ProductTotalPrice;
                        row++;
                        col = 3;
                    }
                }

                package.Save();
            }

            stream.Position = 0;
            string excelName = $"{reportType ?? "Daily"}Sales-{DateTime.Now:yyyyMMddHHmmssfff}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
        }

        public async Task<IActionResult> ExportToPdf(string reportType)
        {
            IEnumerable<Sale> sales = null;

            switch (reportType?.ToLower())
            {
                case "weekly":
                    sales = await _saleRepository.GetWeeklySalesAsync();
                    break;
                case "monthly":
                    sales = await _saleRepository.GetMonthlySalesAsync();
                    break;
                case "daily":
                default:
                    sales = await _saleRepository.GetDailySalesAsync();
                    break;
            }

            if (sales == null || !sales.Any())
            {
                TempData["ErrorMessage"] = "No sales data available for the selected period.";
                return RedirectToAction("DailyReport", new { area = "Admin" });
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    using (var document = new Document(PageSize.A4, 10f, 10f, 10f, 0f))
                    {
                        PdfWriter writer = PdfWriter.GetInstance(document, stream);
                        writer.CloseStream = false;

                        document.Open();

                        var titleFont = FontFactory.GetFont("Arial", 14, Font.BOLD);
                        var headerFont = FontFactory.GetFont("Arial", 12, Font.BOLD);
                        var cellFont = FontFactory.GetFont("Arial", 10);

                        foreach (var sale in sales)
                        {
                            var saleTitle = new Paragraph($"Sale Date: {sale.SaleDate:yyyy-MM-dd HH:mm:ss}, Total Price: {sale.TotalPrice}", titleFont)
                            {
                                SpacingAfter = 10,
                                SpacingBefore = 20
                            };
                            document.Add(saleTitle);

                            var table = new PdfPTable(4)
                            {
                                WidthPercentage = 100,
                                SpacingBefore = 10,
                                SpacingAfter = 10
                            };

                            table.AddCell(new PdfPCell(new Phrase("Product Name", headerFont))
                            {
                                BackgroundColor = BaseColor.LIGHT_GRAY,
                                HorizontalAlignment = Element.ALIGN_CENTER
                            });
                            table.AddCell(new PdfPCell(new Phrase("Quantity", headerFont))
                            {
                                BackgroundColor = BaseColor.LIGHT_GRAY,
                                HorizontalAlignment = Element.ALIGN_CENTER
                            });
                            table.AddCell(new PdfPCell(new Phrase("Product Price", headerFont))
                            {
                                BackgroundColor = BaseColor.LIGHT_GRAY,
                                HorizontalAlignment = Element.ALIGN_CENTER
                            });
                            table.AddCell(new PdfPCell(new Phrase("Total Product Price", headerFont))
                            {
                                BackgroundColor = BaseColor.LIGHT_GRAY,
                                HorizontalAlignment = Element.ALIGN_CENTER
                            });

                            foreach (var item in sale.SaleProducts)
                            {
                                table.AddCell(new PdfPCell(new Phrase(item.Product?.Name ?? "N/A", cellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER
                                });
                                table.AddCell(new PdfPCell(new Phrase(item.Quantity.ToString(), cellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER
                                });
                                table.AddCell(new PdfPCell(new Phrase(item.ProductPrice.ToString("0.00"), cellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER
                                });
                                table.AddCell(new PdfPCell(new Phrase(item.ProductTotalPrice.ToString("0.00"), cellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER
                                });
                            }

                            document.Add(table);
                        }

                        document.Close();
                    }

                    stream.Position = 0;
                    string pdfName = $"{reportType ?? "Daily"}Sales-{DateTime.Now:yyyyMMddHHmmssfff}.pdf";
                    return File(stream.ToArray(), "application/pdf", pdfName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while generating the PDF report: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"PDF Generation Error: {ex}");
                return RedirectToAction("DailyReport", new { area = "Admin" });
            }
        }



    }
}
