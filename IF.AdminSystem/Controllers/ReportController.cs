using DBMonoUtility;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YYLog.ClassLibrary;

using OfficeOpenXml;
using OfficeOpenXml.Table;
using OfficeOpenXml.Style;
using System.Drawing;
using NF.AdminSystem.Models;
using NF.AdminSystem.Providers;

namespace NF.AdminSystem.Controllers
{
    public class ReportController : Controller
    {
        private void setCellHeaderStyle(ExcelRangeBase cell)
        {
            setCellHeaderStyle(cell, 11);
        }

        private void setCellHeaderStyle(ExcelRangeBase cell, int fontSize)
        {
            setCellHeaderStyle(cell, fontSize, ExcelHorizontalAlignment.Center);
        }

        private void setCellHeaderStyle(ExcelRangeBase cell, int fontSize, ExcelHorizontalAlignment align)
        {
            setCellHeaderStyle(cell, fontSize, align, ExcelVerticalAlignment.Center);
        }

        private void setCellHeaderStyle(ExcelRangeBase cell, int fontSize, ExcelHorizontalAlignment align, ExcelVerticalAlignment vertalign)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Font.Size = fontSize;
            cell.Style.VerticalAlignment = vertalign;
            cell.Style.HorizontalAlignment = align;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Border.Bottom.Style = cell.Style.Border.Top.Style
            = cell.Style.Border.Left.Style = cell.Style.Border.Right.Style
            = ExcelBorderStyle.Thin;

            cell.Style.Border.Bottom.Color.SetColor(Color.Black);
            cell.Style.Border.Top.Color.SetColor(Color.Black);
            cell.Style.Border.Right.Color.SetColor(Color.Black);
            cell.Style.Border.Left.Color.SetColor(Color.Black);


            cell.Style.Fill.BackgroundColor.SetColor(Color.DarkGray);
        }

        private void setCellBodyStyle(ExcelRangeBase cell, int fontSize, ExcelHorizontalAlignment align, ExcelVerticalAlignment vertalign)
        {
            //cell.Style.Font.Bold = true;
            cell.Style.Font.Size = fontSize;
            cell.Style.VerticalAlignment = vertalign;
            cell.Style.HorizontalAlignment = align;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Border.Bottom.Style = cell.Style.Border.Top.Style
            = cell.Style.Border.Left.Style = cell.Style.Border.Right.Style
            = ExcelBorderStyle.Thin;

            cell.Style.Border.Bottom.Color.SetColor(Color.Black);
            cell.Style.Border.Top.Color.SetColor(Color.Black);
            cell.Style.Border.Right.Color.SetColor(Color.Black);
            cell.Style.Border.Left.Color.SetColor(Color.Black);

            cell.Style.Fill.BackgroundColor.SetColor(Color.White);
        }

        private void setReleaseLoanHeader(ExcelWorksheet worksheet)
        {
            worksheet.Cells["A1:B1"].Value = "PT. ANUGERAH DIGITAL NIAGA";
            worksheet.Cells["A1:B1"].Merge = true;
            setCellHeaderStyle(worksheet.Cells["A1"]);

            worksheet.Cells["A4:K5"].Merge = true;
            worksheet.Cells["A4:K5"].Value = "DAILY REPORT";
            setCellHeaderStyle(worksheet.Cells["A4:K5"], 20);

            worksheet.Cells["A7:A7"].Value = "Periode :";
            worksheet.Cells["A7:A7"].Merge = true;
            setCellHeaderStyle(worksheet.Cells["A7:A7"]);
            worksheet.Cells["B7:K7"].Merge = true;
            setCellHeaderStyle(worksheet.Cells["B7:K7"], 11, ExcelHorizontalAlignment.Right, ExcelVerticalAlignment.Bottom);

            worksheet.Cells["A8:A8"].Value = "Daily Distribution";
            worksheet.Cells["A8:A8"].Merge = true;
            setCellHeaderStyle(worksheet.Cells["A8:A8"]);
            worksheet.Cells["B8:K8"].Merge = true;
            setCellHeaderStyle(worksheet.Cells["B8:K8"], 11, ExcelHorizontalAlignment.Right, ExcelVerticalAlignment.Bottom);

            worksheet.Cells["A9:A10"].Value = "Loan ID";
            worksheet.Cells["A9:A10"].Merge = true;
            setCellHeaderStyle(worksheet.Cells["A9:A10"]);

            worksheet.Cells["B9:B10"].Value = "Name";
            worksheet.Cells["B9:B10"].Merge = true;
            setCellHeaderStyle(worksheet.Cells["B9:B10"]);

            worksheet.Cells["C9:E9"].Value = "Distribution Fund";
            worksheet.Cells["C9:E9"].Merge = true;
            setCellHeaderStyle(worksheet.Cells["C9:E9"]);

            worksheet.Cells["C10"].Value = "Transfer Date";
            setCellHeaderStyle(worksheet.Cells["C10"]);

            worksheet.Cells["D10"].Value = "Distribution Adm";
            setCellHeaderStyle(worksheet.Cells["D10"]);

            worksheet.Cells["E10"].Value = "Principal";
            setCellHeaderStyle(worksheet.Cells["E10"]);
        }

        private void setPaybackLoanHeader(string title, ExcelWorksheet worksheet, ref int row)
        {
            row += 2;
            worksheet.Cells[String.Format("A{0}:A{0}", row)].Value = title;
            worksheet.Cells[String.Format("A{0}:A{0}", row)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("A{0}:A{0}", row)]);
            worksheet.Cells[String.Format("B{0}:K{0}", row)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("B{0}:K{0}", row)], 11, ExcelHorizontalAlignment.Right, ExcelVerticalAlignment.Bottom);

            //还款数据报表的头
            row += 1;
            worksheet.Cells[String.Format("A{0}:A{1}", row, row + 1)].Value = "Loan ID";
            worksheet.Cells[String.Format("A{0}:A{1}", row, row + 1)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("A{0}:A{0}", row)]);

            worksheet.Cells[String.Format("B{0}:B{1}", row, row + 1)].Value = "Name";
            worksheet.Cells[String.Format("B{0}:B{1}", row, row + 1)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("B{0}:B{0}", row, row + 1)]);

            worksheet.Cells[String.Format("C{0}:E{0}", row)].Value = "Distribution Fund";
            worksheet.Cells[String.Format("C{0}:E{0}", row)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("C{0}:E{0}", row)]);

            worksheet.Cells[String.Format("C{0}", row + 1)].Value = "Transfer Date";
            setCellHeaderStyle(worksheet.Cells[String.Format("C{0}", row + 1)]);

            worksheet.Cells[String.Format("D{0}", row + 1)].Value = "Distribution Adm";
            setCellHeaderStyle(worksheet.Cells[String.Format("D{0}", row + 1)]);

            worksheet.Cells[String.Format("E{0}", row + 1)].Value = "Principal";
            setCellHeaderStyle(worksheet.Cells[String.Format("E{0}", row + 1)]);

            worksheet.Cells[String.Format("F{0}:K{0}", row)].Value = "Collection Payment";
            worksheet.Cells[String.Format("F{0}:K{0}", row)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("F{0}:K{0}", row)]);


            worksheet.Cells[String.Format("F{0}", row + 1)].Value = "Adm";
            setCellHeaderStyle(worksheet.Cells[String.Format("F{0}", row + 1)]);

            worksheet.Cells[String.Format("G{0}", row + 1)].Value = "Extend";
            setCellHeaderStyle(worksheet.Cells[String.Format("G{0}", row + 1)]);

            worksheet.Cells[String.Format("H{0}", row + 1)].Value = "Over Due";
            setCellHeaderStyle(worksheet.Cells[String.Format("H{0}", row + 1)]);

            worksheet.Cells[String.Format("I{0}", row + 1)].Value = "Principal";
            setCellHeaderStyle(worksheet.Cells[String.Format("I{0}", row + 1)]);

            worksheet.Cells[String.Format("J{0}", row + 1)].Value = "Total";
            setCellHeaderStyle(worksheet.Cells[String.Format("J{0}", row + 1)]);

            worksheet.Cells[String.Format("K{0}", row + 1)].Value = "Payment Date";
            setCellHeaderStyle(worksheet.Cells[String.Format("K{0}", row + 1)]);
        }

        private void setBadLoanHeader(string title, ExcelWorksheet worksheet, ref int row)
        {
            row += 2;
            worksheet.Cells[String.Format("A{0}:A{0}", row)].Value = title;
            worksheet.Cells[String.Format("A{0}:A{0}", row)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("A{0}:A{0}", row)]);
            worksheet.Cells[String.Format("B{0}:F{0}", row)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("B{0}:F{0}", row)], 11, ExcelHorizontalAlignment.Right, ExcelVerticalAlignment.Bottom);

            //坏帐数据报表的头
            row += 1;
            worksheet.Cells[String.Format("A{0}:A{1}", row, row + 1)].Value = "Loan ID";
            worksheet.Cells[String.Format("A{0}:A{1}", row, row + 1)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("A{0}:A{0}", row)]);

            worksheet.Cells[String.Format("B{0}:B{1}", row, row + 1)].Value = "Name";
            worksheet.Cells[String.Format("B{0}:B{1}", row, row + 1)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("B{0}:B{0}", row, row + 1)]);

            worksheet.Cells[String.Format("C{0}:F{0}", row)].Value = "Distribution Fund";
            worksheet.Cells[String.Format("C{0}:F{0}", row)].Merge = true;
            setCellHeaderStyle(worksheet.Cells[String.Format("C{0}:F{0}", row)]);

            worksheet.Cells[String.Format("C{0}", row + 1)].Value = "Transfer Date";
            setCellHeaderStyle(worksheet.Cells[String.Format("C{0}", row + 1)]);

            worksheet.Cells[String.Format("D{0}", row + 1)].Value = "Distribution Adm";
            setCellHeaderStyle(worksheet.Cells[String.Format("D{0}", row + 1)]);

            worksheet.Cells[String.Format("E{0}", row + 1)].Value = "Principal";
            setCellHeaderStyle(worksheet.Cells[String.Format("E{0}", row + 1)]);

            worksheet.Cells[String.Format("F{0}", row + 1)].Value = "Overdue Fee";
            setCellHeaderStyle(worksheet.Cells[String.Format("F{0}", row + 1)]);
        }

        [AllowAnonymous]
        public IActionResult DailyReport(string date)
        {
            string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            DataProviderResultModel model = ReportProvider.GetDailyReport(date);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(String.Format("{0} DAILY REPORT", date));

                setReleaseLoanHeader(worksheet);

                //Fill release loan data 
                DataTable releaseLoan = model.data as DataTable;
                int row = 11;
                if (null != releaseLoan || releaseLoan.Rows.Count > 0)
                {
                    worksheet.Cells["A11"].LoadFromDataTable(releaseLoan, PrintHeaders: false);
                }
                row += releaseLoan.Rows.Count;
                setCellBodyStyle(worksheet.Cells[String.Format("A11:E{0}", row)], 11, ExcelHorizontalAlignment.Center, ExcelVerticalAlignment.Center);

                setPaybackLoanHeader("Daily Collection", worksheet, ref row);

                row += 2;
                model = ReportProvider.GetDailyPaybackReport(date);
                DataTable paybackLoan = model.data as DataTable;

                int bodyRow = row;
                if (null != paybackLoan && paybackLoan.Rows.Count > 0)
                {
                    worksheet.Cells[String.Format("A{0}", row)].LoadFromDataTable(paybackLoan, PrintHeaders: false);
                }
                row += paybackLoan.Rows.Count;
                setCellBodyStyle(worksheet.Cells[String.Format("A{0}:K{1}", bodyRow, row)], 11, ExcelHorizontalAlignment.Center, ExcelVerticalAlignment.Center);

                setPaybackLoanHeader("Extend Collection", worksheet, ref row);

                row += 2;
                model = ReportProvider.GetDailyExtendReport(date);
                DataTable extendLoan = model.data as DataTable;

                bodyRow = row;
                if (null != extendLoan && extendLoan.Rows.Count > 0)
                {
                    worksheet.Cells[String.Format("A{0}", row)].LoadFromDataTable(extendLoan, PrintHeaders: false);
                }
                row += extendLoan.Rows.Count;
                setCellBodyStyle(worksheet.Cells[String.Format("A{0}:K{1}", bodyRow, row)], 11, ExcelHorizontalAlignment.Center, ExcelVerticalAlignment.Center);

                setBadLoanHeader("Bad Debt Confirmed", worksheet, ref row);
                row += 2;

                model = ReportProvider.GetDailyBadReport(date);
                DataTable badLoan = model.data as DataTable;
                bodyRow = row;
                if (null != badLoan && badLoan.Rows.Count > 0)
                {
                    worksheet.Cells[String.Format("A{0}", row)].LoadFromDataTable(badLoan, PrintHeaders: false);
                }
                row += badLoan.Rows.Count;
                setCellBodyStyle(worksheet.Cells[String.Format("A{0}:F{1}", bodyRow, row)], 11, ExcelHorizontalAlignment.Center, ExcelVerticalAlignment.Center);

                row += 2;
                worksheet.Cells[String.Format("A{0}", row)].Value = "Prepared by,";
                worksheet.Cells[String.Format("C{0}", row)].Value = "Checked by,";
                worksheet.Cells[String.Format("F{0}", row)].Value = "Approved by,";
                worksheet.Cells.AutoFitColumns();

                return File(package.GetAsByteArray(), XlsxContentType, String.Format("{0}_report.xlsx", date.Replace("-", "")));
            }
        }
    }
}