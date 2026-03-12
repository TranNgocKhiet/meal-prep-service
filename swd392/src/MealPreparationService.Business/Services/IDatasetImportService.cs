namespace MealPreparationService.Business.Services;

public interface IDatasetImportService
{
    /// <summary>
    /// Checks if dataset import should be performed (i.e., import has not been completed before)
    /// </summary>
    Task<bool> ShouldImportDataAsync();
    
    /// <summary>
    /// Imports all datasets from Excel files in the correct order
    /// </summary>
    Task<ImportResultDto> ImportAllDatasetsAsync();
    
    /// <summary>
    /// Validates data integrity after import
    /// </summary>
    Task<List<string>> ValidateDataIntegrityAsync();
}

public class ImportResultDto
{
    public bool Success { get; set; }
    public List<string> ImportedFiles { get; set; } = new();
    public List<string> FailedFiles { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int TotalRecordsImported { get; set; }
    public DateTime ImportedAt { get; set; }
}
