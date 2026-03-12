using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace MealPreparationService.Business.Services;

public class ExcelReaderService : IExcelReaderService
{
    private readonly ILogger<ExcelReaderService> _logger;

    public ExcelReaderService(ILogger<ExcelReaderService> logger)
    {
        _logger = logger;
    }

    public async Task<List<Dictionary<string, object>>> ReadExcelFileAsync(string filePath, string? worksheetName = null)
    {
        var result = new List<Dictionary<string, object>>();

        try
        {
            if (!ValidateExcelFile(filePath))
            {
                _logger.LogError("Excel file validation failed: {FilePath}", filePath);
                throw new FileNotFoundException($"Excel file not found or invalid: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            
            using var package = new ExcelPackage(fileInfo);
            
            // Get the worksheet
            ExcelWorksheet? worksheet;
            if (!string.IsNullOrEmpty(worksheetName))
            {
                worksheet = package.Workbook.Worksheets[worksheetName];
                if (worksheet == null)
                {
                    _logger.LogError("Worksheet '{WorksheetName}' not found in file: {FilePath}", worksheetName, filePath);
                    throw new ArgumentException($"Worksheet '{worksheetName}' not found in file: {filePath}");
                }
            }
            else
            {
                worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogError("No worksheets found in file: {FilePath}", filePath);
                    throw new InvalidOperationException($"No worksheets found in file: {filePath}");
                }
            }

            // Get the dimensions of the worksheet
            if (worksheet.Dimension == null)
            {
                _logger.LogWarning("Worksheet is empty in file: {FilePath}", filePath);
                return await Task.FromResult(result);
            }

            var start = worksheet.Dimension.Start;
            var end = worksheet.Dimension.End;

            // Read header row (first row)
            var headers = new List<string>();
            for (int col = start.Column; col <= end.Column; col++)
            {
                var headerValue = worksheet.Cells[start.Row, col].Value?.ToString() ?? $"Column{col}";
                headers.Add(headerValue);
            }

            // Read data rows
            for (int row = start.Row + 1; row <= end.Row; row++)
            {
                var rowData = new Dictionary<string, object>();
                bool isEmptyRow = true;

                for (int col = start.Column; col <= end.Column; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    var columnName = headers[col - start.Column];
                    
                    if (cellValue != null)
                    {
                        isEmptyRow = false;
                        rowData[columnName] = cellValue;
                    }
                    else
                    {
                        rowData[columnName] = string.Empty;
                    }
                }

                // Skip empty rows
                if (!isEmptyRow)
                {
                    result.Add(rowData);
                }
            }

            _logger.LogInformation("Successfully read {RowCount} rows from Excel file: {FilePath}", result.Count, filePath);
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Excel file: {FilePath}", filePath);
            throw;
        }
    }

    public bool ValidateExcelFile(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("File path is null or empty");
                return false;
            }

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File does not exist: {FilePath}", filePath);
                return false;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                _logger.LogWarning("File is not an Excel file: {FilePath}", filePath);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Excel file: {FilePath}", filePath);
            return false;
        }
    }
}
