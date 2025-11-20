using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(DatabaseService dbService, ILogger<IndexModel> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public List<Expense> Expenses { get; set; } = new();
    public string? Filter { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? filter)
    {
        Filter = filter;
        // Get all expenses for the current user (hardcoded to user 1 for demo)
        Expenses = await _dbService.GetExpensesAsync(userId: 1, filter: filter);
        ErrorMessage = _dbService.GetLastError();
    }

    public async Task<IActionResult> OnPostAsync(int expenseId, string action)
    {
        if (action == "submit")
        {
            var request = new UpdateExpenseStatusRequest
            {
                ExpenseId = expenseId,
                StatusId = 2 // Submitted
            };
            await _dbService.UpdateExpenseStatusAsync(request);
        }

        return RedirectToPage();
    }
}
