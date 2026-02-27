using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountApiController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountApiController> _logger;

    public AccountApiController(IAccountService accountService, ILogger<AccountApiController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new customer account
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountDto>> Register([FromBody] CreateAccountDto dto)
    {
        try
        {
            if (await _accountService.EmailExistsAsync(dto.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            var account = await _accountService.RegisterAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering account");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Authenticate user
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccountDto>> Login([FromBody] LoginDto dto)
    {
        try
        {
            var account = await _accountService.AuthenticateAsync(dto.Email, dto.Password);
            if (account == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }
            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Get account by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> GetById(Guid id)
    {
        try
        {
            var account = await _accountService.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound(new { message = $"Account with ID {id} not found" });
            }
            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account {AccountId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the account" });
        }
    }

    /// <summary>
    /// Check if email exists
    /// </summary>
    [HttpGet("email-exists/{email}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> EmailExists(string email)
    {
        try
        {
            var exists = await _accountService.EmailExistsAsync(email);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get all staff accounts (Admin only)
    /// </summary>
    [HttpGet("staff")]
    [ProducesResponseType(typeof(IEnumerable<AccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AccountDto>>> GetAllStaff()
    {
        try
        {
            var accounts = await _accountService.GetAllStaffAccountsAsync();
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving staff accounts");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get accounts by role
    /// </summary>
    [HttpGet("role/{role}")]
    [ProducesResponseType(typeof(IEnumerable<AccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AccountDto>>> GetByRole(string role)
    {
        try
        {
            var accounts = await _accountService.GetAccountsByRoleAsync(role);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts by role {Role}", role);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Create staff account (Admin only)
    /// </summary>
    [HttpPost("staff")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountDto>> CreateStaff([FromBody] CreateAccountDto dto, [FromQuery] string role)
    {
        try
        {
            var account = await _accountService.CreateStaffAccountAsync(dto, role);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating staff account");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update staff account (Admin only)
    /// </summary>
    [HttpPut("staff/{id}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> UpdateStaff(Guid id, [FromBody] UpdateAccountDto dto)
    {
        try
        {
            var account = await _accountService.UpdateStaffAccountAsync(id, dto);
            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating staff account {AccountId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Delete staff account (Admin only)
    /// </summary>
    [HttpDelete("staff/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStaff(Guid id)
    {
        try
        {
            var result = await _accountService.DeleteStaffAccountAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Account with ID {id} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting staff account {AccountId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
