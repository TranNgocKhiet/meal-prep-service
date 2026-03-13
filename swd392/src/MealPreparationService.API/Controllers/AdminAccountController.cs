using MealPreparationService.API.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace MealPreparationService.API.Controllers;

public class AccountDto
{
    public string? Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int CurrentCredits { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? GoogleAuthId { get; set; }
}

[ApiController]
[Route("api/admin/accounts")]
[Authorize]
public class AdminAccountController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminAccountController> _logger;

    public AdminAccountController(IUnitOfWork unitOfWork, ILogger<AdminAccountController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Account>>>> GetAccounts(
        [FromQuery] string? search = null,
        [FromQuery] int? roleId = null)
    {
        try
        {
            IQueryable<Account> query = _unitOfWork.Accounts.GetAllQueryable()
                .Include(a => a.Role);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => 
                    a.Email.Contains(search) || 
                    a.FullName.Contains(search) ||
                    a.PhoneNumber.Contains(search));
            }

            if (roleId.HasValue)
            {
                query = query.Where(a => a.RoleId == roleId.Value);
            }

            var accounts = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(new ApiResponse<IEnumerable<Account>>
            {
                Success = true,
                Data = accounts,
                Message = "Accounts retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts");
            return StatusCode(500, new ApiResponse<IEnumerable<Account>>
            {
                Success = false,
                Message = "An error occurred while retrieving accounts"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Account>>> GetAccount(string id)
    {
        try
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(id);

            if (account == null)
            {
                return NotFound(new ApiResponse<Account>
                {
                    Success = false,
                    Message = "Account not found"
                });
            }

            return Ok(new ApiResponse<Account>
            {
                Success = true,
                Data = account,
                Message = "Account retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account {Id}", id);
            return StatusCode(500, new ApiResponse<Account>
            {
                Success = false,
                Message = "An error occurred while retrieving the account"
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Account>>> CreateAccount([FromBody] AccountDto dto)
    {
        try
        {
            // Check if email already exists
            var existingAccount = await _unitOfWork.Accounts.GetByEmailAsync(dto.Email);
            if (existingAccount != null)
            {
                return BadRequest(new ApiResponse<Account>
                {
                    Success = false,
                    Message = "Email already exists"
                });
            }

            if (string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest(new ApiResponse<Account>
                {
                    Success = false,
                    Message = "Password is required"
                });
            }

            var account = new Account
            {
                Id = Guid.NewGuid().ToString(),
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                RoleId = dto.RoleId,
                CurrentCredits = dto.CurrentCredits,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                GoogleAuthId = dto.GoogleAuthId
            };

            await _unitOfWork.Accounts.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Accounts.GetByIdAsync(account.Id);

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, new ApiResponse<Account>
            {
                Success = true,
                Data = created,
                Message = "Account created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account");
            return StatusCode(500, new ApiResponse<Account>
            {
                Success = false,
                Message = "An error occurred while creating the account"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Account>>> UpdateAccount(string id, [FromBody] AccountDto dto)
    {
        try
        {
            var existing = await _unitOfWork.Accounts.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new ApiResponse<Account>
                {
                    Success = false,
                    Message = "Account not found"
                });
            }

            // Check if email is being changed and if it already exists
            if (existing.Email != dto.Email)
            {
                var emailExists = await _unitOfWork.Accounts.GetByEmailAsync(dto.Email);
                if (emailExists != null)
                {
                    return BadRequest(new ApiResponse<Account>
                    {
                        Success = false,
                        Message = "Email already exists"
                    });
                }
            }

            existing.Email = dto.Email;
            existing.FullName = dto.FullName;
            existing.PhoneNumber = dto.PhoneNumber ?? string.Empty;
            
            // Validate RoleId exists before updating
            if (dto.RoleId != existing.RoleId)
            {
                var roleExists = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId);
                if (roleExists == null)
                {
                    return BadRequest(new ApiResponse<Account>
                    {
                        Success = false,
                        Message = $"Role with ID {dto.RoleId} does not exist"
                    });
                }
                existing.RoleId = dto.RoleId;
            }
            
            existing.CurrentCredits = dto.CurrentCredits;
            existing.IsActive = dto.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.GoogleAuthId = dto.GoogleAuthId;

            // Only update password if provided
            if (!string.IsNullOrEmpty(dto.Password))
            {
                existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _unitOfWork.Accounts.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Accounts.GetByIdAsync(id);

            return Ok(new ApiResponse<Account>
            {
                Success = true,
                Data = updated,
                Message = "Account updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account {Id}", id);
            return StatusCode(500, new ApiResponse<Account>
            {
                Success = false,
                Message = "An error occurred while updating the account"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAccount(string id)
    {
        try
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Account not found"
                });
            }

            await _unitOfWork.Accounts.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Account deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the account"
            });
        }
    }

    [HttpPatch("{id}/toggle-status")]
    public async Task<ActionResult<ApiResponse<Account>>> ToggleAccountStatus(string id)
    {
        try
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound(new ApiResponse<Account>
                {
                    Success = false,
                    Message = "Account not found"
                });
            }

            // Toggle the IsActive status
            account.IsActive = !account.IsActive;
            account.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Accounts.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Accounts.GetByIdAsync(id);

            return Ok(new ApiResponse<Account>
            {
                Success = true,
                Data = updated,
                Message = $"Account {(account.IsActive ? "activated" : "deactivated")} successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling account status {Id}", id);
            return StatusCode(500, new ApiResponse<Account>
            {
                Success = false,
                Message = "An error occurred while updating the account status"
            });
        }
    }
}
