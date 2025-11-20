using Microsoft.Data.SqlClient;
using Azure.Identity;
using Azure.Core;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class DatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;
    private string? _lastError;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string? GetLastError() => _lastError;

    private async Task<SqlConnection> GetConnectionAsync()
    {
        var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
        var connectionString = _configuration.GetConnectionString("ExpenseDb");

        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Server=tcp:sql-expense-mgmt-xyz.database.windows.net,1433;" +
                             "Database=ExpenseManagementDB;" +
                             "Encrypt=True;" +
                             "TrustServerCertificate=False;" +
                             "Connection Timeout=30;";
        }

        // Add managed identity authentication if client ID is available
        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            connectionString += $"Authentication=Active Directory Managed Identity;User Id={managedIdentityClientId};";
        }

        var connection = new SqlConnection(connectionString);
        
        // If using managed identity, get token
        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            try
            {
                var credential = new DefaultAzureCredential();
                var tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
                var token = await credential.GetTokenAsync(tokenRequestContext);
                connection.AccessToken = token.Token;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get managed identity token, attempting connection without token");
            }
        }

        await connection.OpenAsync();
        return connection;
    }

    private async Task<T> ExecuteWithFallbackAsync<T>(Func<Task<T>> dbOperation, T fallbackValue, string operationName)
    {
        try
        {
            _lastError = null;
            return await dbOperation();
        }
        catch (Exception ex)
        {
            _lastError = $"Database error in {operationName}: {ex.Message} (DatabaseService.cs, ExecuteWithFallbackAsync)";
            _logger.LogError(ex, "Database operation failed: {Operation}", operationName);
            return fallbackValue;
        }
    }

    public Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null, string? filter = null)
    {
        return ExecuteWithFallbackAsync(async () =>
        {
            await using var connection = await GetConnectionAsync();
            var query = @"
                SELECT e.ExpenseId, e.UserId, e.CategoryId, e.StatusId, e.AmountMinor, e.Currency,
                       e.ExpenseDate, e.Description, e.ReceiptFile, e.SubmittedAt, e.ReviewedBy, 
                       e.ReviewedAt, e.CreatedAt,
                       u.UserName, c.CategoryName, s.StatusName, r.UserName as ReviewerName
                FROM dbo.Expenses e
                JOIN dbo.Users u ON e.UserId = u.UserId
                JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
                WHERE (@UserId IS NULL OR e.UserId = @UserId)
                  AND (@StatusId IS NULL OR e.StatusId = @StatusId)
                  AND (@Filter IS NULL OR 
                       u.UserName LIKE '%' + @Filter + '%' OR 
                       c.CategoryName LIKE '%' + @Filter + '%' OR 
                       e.Description LIKE '%' + @Filter + '%')
                ORDER BY e.ExpenseDate DESC";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Filter", (object?)filter ?? DBNull.Value);

            var expenses = new List<Expense>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            return expenses;
        }, GetDummyExpenses(), "GetExpenses");
    }

    public Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        return ExecuteWithFallbackAsync(async () =>
        {
            await using var connection = await GetConnectionAsync();
            var query = @"
                SELECT e.ExpenseId, e.UserId, e.CategoryId, e.StatusId, e.AmountMinor, e.Currency,
                       e.ExpenseDate, e.Description, e.ReceiptFile, e.SubmittedAt, e.ReviewedBy, 
                       e.ReviewedAt, e.CreatedAt,
                       u.UserName, c.CategoryName, s.StatusName, r.UserName as ReviewerName
                FROM dbo.Expenses e
                JOIN dbo.Users u ON e.UserId = u.UserId
                JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
                WHERE e.ExpenseId = @ExpenseId";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
            return null;
        }, null, "GetExpenseById");
    }

    public Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        return ExecuteWithFallbackAsync(async () =>
        {
            await using var connection = await GetConnectionAsync();
            var query = @"
                INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, CreatedAt)
                VALUES (@UserId, @CategoryId, 1, @AmountMinor, 'GBP', @ExpenseDate, @Description, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }, -1, "CreateExpense");
    }

    public Task<bool> UpdateExpenseStatusAsync(UpdateExpenseStatusRequest request)
    {
        return ExecuteWithFallbackAsync(async () =>
        {
            await using var connection = await GetConnectionAsync();
            var query = @"
                UPDATE dbo.Expenses 
                SET StatusId = @StatusId,
                    SubmittedAt = CASE WHEN @StatusId = 2 AND SubmittedAt IS NULL THEN SYSUTCDATETIME() ELSE SubmittedAt END,
                    ReviewedBy = @ReviewedBy,
                    ReviewedAt = CASE WHEN @StatusId IN (3, 4) THEN SYSUTCDATETIME() ELSE ReviewedAt END
                WHERE ExpenseId = @ExpenseId";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@StatusId", request.StatusId);
            command.Parameters.AddWithValue("@ReviewedBy", (object?)request.ReviewedBy ?? DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }, false, "UpdateExpenseStatus");
    }

    public Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        return ExecuteWithFallbackAsync(async () =>
        {
            await using var connection = await GetConnectionAsync();
            var query = "SELECT CategoryId, CategoryName, IsActive FROM dbo.ExpenseCategories WHERE IsActive = 1";

            await using var command = new SqlCommand(query, connection);
            var categories = new List<ExpenseCategory>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
            return categories;
        }, GetDummyCategories(), "GetCategories");
    }

    public Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        return ExecuteWithFallbackAsync(async () =>
        {
            await using var connection = await GetConnectionAsync();
            var query = "SELECT StatusId, StatusName FROM dbo.ExpenseStatus";

            await using var command = new SqlCommand(query, connection);
            var statuses = new List<ExpenseStatus>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }
            return statuses;
        }, GetDummyStatuses(), "GetStatuses");
    }

    public Task<List<User>> GetUsersAsync()
    {
        return ExecuteWithFallbackAsync(async () =>
        {
            await using var connection = await GetConnectionAsync();
            var query = @"
                SELECT u.UserId, u.UserName, u.Email, u.RoleId, u.ManagerId, u.IsActive, u.CreatedAt, r.RoleName
                FROM dbo.Users u
                JOIN dbo.Roles r ON u.RoleId = r.RoleId
                WHERE u.IsActive = 1";

            await using var command = new SqlCommand(query, connection);
            var users = new List<User>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleId = reader.GetInt32(3),
                    ManagerId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    IsActive = reader.GetBoolean(5),
                    CreatedAt = reader.GetDateTime(6),
                    RoleName = reader.GetString(7)
                });
            }
            return users;
        }, GetDummyUsers(), "GetUsers");
    }

    private Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            CategoryId = reader.GetInt32(2),
            StatusId = reader.GetInt32(3),
            AmountMinor = reader.GetInt32(4),
            Currency = reader.GetString(5),
            ExpenseDate = reader.GetDateTime(6),
            Description = reader.IsDBNull(7) ? null : reader.GetString(7),
            ReceiptFile = reader.IsDBNull(8) ? null : reader.GetString(8),
            SubmittedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            ReviewedBy = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            ReviewedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
            CreatedAt = reader.GetDateTime(12),
            UserName = reader.GetString(13),
            CategoryName = reader.GetString(14),
            StatusName = reader.GetString(15),
            ReviewerName = reader.IsDBNull(16) ? null : reader.GetString(16)
        };
    }

    // Dummy data for fallback
    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                CategoryId = 1,
                StatusId = 2,
                AmountMinor = 2540,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-5),
                Description = "Taxi from airport (dummy data)",
                UserName = "Alice Example",
                CategoryName = "Travel",
                StatusName = "Submitted",
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                CategoryId = 2,
                StatusId = 3,
                AmountMinor = 1425,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-10),
                Description = "Client lunch (dummy data)",
                UserName = "Alice Example",
                CategoryName = "Meals",
                StatusName = "Approved",
                ReviewerName = "Bob Manager",
                CreatedAt = DateTime.Now.AddDays(-10)
            }
        };
    }

    private List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new ExpenseCategory { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User
            {
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                RoleId = 1,
                RoleName = "Employee",
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-6)
            },
            new User
            {
                UserId = 2,
                UserName = "Bob Manager",
                Email = "bob.manager@example.co.uk",
                RoleId = 2,
                RoleName = "Manager",
                IsActive = true,
                CreatedAt = DateTime.Now.AddYears(-1)
            }
        };
    }
}
