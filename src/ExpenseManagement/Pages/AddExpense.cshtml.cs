using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<AddExpenseModel> _logger;

    public AddExpenseModel(DatabaseService dbService, ILogger<AddExpenseModel> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public List<ExpenseCategory> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _dbService.GetCategoriesAsync();
        ErrorMessage = _dbService.GetLastError();
    }

    public async Task<IActionResult> OnPostAsync(decimal amount, DateTime expenseDate, int categoryId, string? description)
    {
        var request = new CreateExpenseRequest
        {
            UserId = 1, // Hardcoded for demo
            Amount = amount,
            ExpenseDate = expenseDate,
            CategoryId = categoryId,
            Description = description
        };

        var expenseId = await _dbService.CreateExpenseAsync(request);
        
        if (expenseId > 0)
        {
            return RedirectToPage("/Index");
        }

        Categories = await _dbService.GetCategoriesAsync();
        ErrorMessage = _dbService.GetLastError() ?? "Failed to create expense";
        return Page();
    }
}
