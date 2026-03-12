using MealPreparationService.API.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/revenuereports")]
[Authorize]
public class RevenueReportController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevenueReportController> _logger;

    public RevenueReportController(IUnitOfWork unitOfWork, ILogger<RevenueReportController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<RevenueReport>>>> GetReports(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        try
        {
            var query = _unitOfWork.RevenueReports.GetAllQueryable();

            if (year.HasValue)
            {
                query = query.Where(r => r.Year == year.Value);
            }

            if (month.HasValue)
            {
                query = query.Where(r => r.Month == month.Value);
            }

            var reports = await query
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .ToListAsync();

            return Ok(new ApiResponse<IEnumerable<RevenueReport>>
            {
                Success = true,
                Data = reports,
                Message = "Revenue reports retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving revenue reports");
            return StatusCode(500, new ApiResponse<IEnumerable<RevenueReport>>
            {
                Success = false,
                Message = "An error occurred while retrieving revenue reports"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RevenueReport>>> GetById(string id)
    {
        try
        {
            var report = await _unitOfWork.RevenueReports.GetByIdAsync(id);

            if (report == null)
            {
                return NotFound(new ApiResponse<RevenueReport>
                {
                    Success = false,
                    Message = "Revenue report not found"
                });
            }

            return Ok(new ApiResponse<RevenueReport>
            {
                Success = true,
                Data = report,
                Message = "Revenue report retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving revenue report {Id}", id);
            return StatusCode(500, new ApiResponse<RevenueReport>
            {
                Success = false,
                Message = "An error occurred while retrieving the revenue report"
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RevenueReport>>> Create([FromBody] RevenueReport report)
    {
        try
        {
            report.Id = Guid.NewGuid().ToString();
            report.CreatedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.RevenueReports.AddAsync(report);

            return CreatedAtAction(nameof(GetById), new { id = report.Id }, new ApiResponse<RevenueReport>
            {
                Success = true,
                Data = report,
                Message = "Revenue report created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating revenue report");
            return StatusCode(500, new ApiResponse<RevenueReport>
            {
                Success = false,
                Message = "An error occurred while creating the revenue report"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RevenueReport>>> Update(string id, [FromBody] RevenueReport report)
    {
        try
        {
            var existing = await _unitOfWork.RevenueReports.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new ApiResponse<RevenueReport>
                {
                    Success = false,
                    Message = "Revenue report not found"
                });
            }

            existing.Month = report.Month;
            existing.Year = report.Year;
            existing.TotalSubscriptionRev = report.TotalSubscriptionRev;
            existing.TotalOrderRev = report.TotalOrderRev;
            existing.TotalAiCreditRev = report.TotalAiCreditRev;
            existing.TotalOrdersCount = report.TotalOrdersCount;
            existing.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.RevenueReports.UpdateAsync(existing);

            return Ok(new ApiResponse<RevenueReport>
            {
                Success = true,
                Data = existing,
                Message = "Revenue report updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating revenue report {Id}", id);
            return StatusCode(500, new ApiResponse<RevenueReport>
            {
                Success = false,
                Message = "An error occurred while updating the revenue report"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            var report = await _unitOfWork.RevenueReports.GetByIdAsync(id);
            if (report == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Revenue report not found"
                });
            }

            await _unitOfWork.RevenueReports.DeleteAsync(id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Revenue report deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting revenue report {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the revenue report"
            });
        }
    }
}



