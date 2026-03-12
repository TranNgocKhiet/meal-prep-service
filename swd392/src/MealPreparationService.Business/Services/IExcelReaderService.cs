namespace MealPreparationService.Business.Services;

public interface IExcelReaderService
{
    /// <summary>
    /// Reads data from an Excel file and returns it as a list of dictionaries.
    /// Each dictionary represents a row with column names as keys.
    /// </summary>
    /// <param name="filePath">Path to the Excel file</param>
    /// <param name="worksheetName">Name of the worksheet to read (optional, defaults to first sheet)</param>
    /// <returns>List of rows as dictionaries</returns>
    Task<List<Dictionary<string, object>>> ReadExcelFileAsync(string filePath, string? worksheetName = null);
    
    /// <summary>
    /// Validates that an Excel file exists and is readable
    /// </summary>
    /// <param name="filePath">Path to the Excel file</param>
    /// <returns>True if file is valid, false otherwise</returns>
    bool ValidateExcelFile(string filePath);
}
