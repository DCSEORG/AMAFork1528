using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(DatabaseService dbService, ILogger<ExpensesController> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses(
        [FromQuery] int? userId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] string? filter = null)
    {
        var expenses = await _dbService.GetExpensesAsync(userId, statusId, filter);
        return Ok(expenses);
    }

    /// <summary>
    /// Get a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var expense = await _dbService.GetExpenseByIdAsync(id);
        if (expense == null)
        {
            return NotFound();
        }
        return Ok(expense);
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var expenseId = await _dbService.CreateExpenseAsync(request);
        if (expenseId <= 0)
        {
            return StatusCode(500, "Failed to create expense");
        }
        return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, expenseId);
    }

    /// <summary>
    /// Update expense status (submit, approve, reject)
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateExpenseStatus(int id, [FromBody] UpdateExpenseStatusRequest request)
    {
        request.ExpenseId = id;
        var success = await _dbService.UpdateExpenseStatusAsync(request);
        if (!success)
        {
            return StatusCode(500, "Failed to update expense status");
        }
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public CategoriesController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all active expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetCategories()
    {
        var categories = await _dbService.GetCategoriesAsync();
        return Ok(categories);
    }
}

[ApiController]
[Route("api/[controller]")]
public class StatusesController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public StatusesController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseStatus>>> GetStatuses()
    {
        var statuses = await _dbService.GetStatusesAsync();
        return Ok(statuses);
    }
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public UsersController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all active users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers()
    {
        var users = await _dbService.GetUsersAsync();
        return Ok(users);
    }
}
