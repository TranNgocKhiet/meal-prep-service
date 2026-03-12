using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Background service that runs dataset import on application startup
/// </summary>
public class DatasetImportHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatasetImportHostedService> _logger;

    public DatasetImportHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatasetImportHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatasetImportHostedService starting");

        try
        {
            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IDatasetImportService>();

            // Check if import should run
            var shouldImport = await importService.ShouldImportDataAsync();
            
            if (shouldImport)
            {
                _logger.LogInformation("Starting dataset import on first run");
                
                var result = await importService.ImportAllDatasetsAsync();
                
                if (result.Success)
                {
                    _logger.LogInformation(
                        "Dataset import completed successfully. Imported {Count} records from {FileCount} files",
                        result.TotalRecordsImported,
                        result.ImportedFiles.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "Dataset import completed with errors. Failed files: {FailedFiles}",
                        string.Join(", ", result.FailedFiles));
                    
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("Import error: {Error}", error);
                    }
                }

                if (result.Warnings.Any())
                {
                    foreach (var warning in result.Warnings)
                    {
                        _logger.LogWarning("Import warning: {Warning}", warning);
                    }
                }
            }
            else
            {
                _logger.LogInformation("Dataset import not required - data already exists");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dataset import startup process");
            // Don't throw - we don't want to prevent the application from starting
        }

        _logger.LogInformation("DatasetImportHostedService started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatasetImportHostedService stopping");
        return Task.CompletedTask;
    }
}
