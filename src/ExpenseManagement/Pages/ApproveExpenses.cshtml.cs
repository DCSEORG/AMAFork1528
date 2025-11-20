using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<ApproveExpensesModel> _logger;

    public ApproveExpensesModel(DatabaseService dbService, ILogger<ApproveExpensesModel> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public string? Filter { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? filter)
    {
        Filter = filter;
        // Get submitted expenses (statusId = 2)
        PendingExpenses = await _dbService.GetExpensesAsync(statusId: 2, filter: filter);
        ErrorMessage = _dbService.GetLastError();
    }

    public async Task<IActionResult> OnPostAsync(int expenseId, string action)
    {
        var request = new UpdateExpenseStatusRequest
        {
            ExpenseId = expenseId,
            StatusId = action == "approve" ? 3 : 4, // 3 = Approved, 4 = Rejected
            ReviewedBy = 2 // Hardcoded manager user ID for demo
        };

        await _dbService.UpdateExpenseStatusAsync(request);
        return RedirectToPage();
    }
}
