# Expense Management System Context

## System Overview
This is an expense management system that helps employees submit expenses and managers approve them.

## Database Schema

### Tables:
- **Roles**: RoleId, RoleName (Employee, Manager), Description
- **Users**: UserId, UserName, Email, RoleId, ManagerId, IsActive, CreatedAt
- **ExpenseCategories**: CategoryId, CategoryName (Travel, Meals, Supplies, Accommodation, Other), IsActive
- **ExpenseStatus**: StatusId, StatusName (Draft, Submitted, Approved, Rejected)
- **Expenses**: ExpenseId, UserId, CategoryId, StatusId, AmountMinor (in pence), Currency (GBP), ExpenseDate, Description, ReceiptFile, SubmittedAt, ReviewedBy, ReviewedAt, CreatedAt

## Key Concepts

### Expense Workflow:
1. **Draft (StatusId=1)**: Employee creates expense but hasn't submitted yet
2. **Submitted (StatusId=2)**: Employee submits expense for manager review
3. **Approved (StatusId=3)**: Manager approves the expense
4. **Rejected (StatusId=4)**: Manager rejects the expense

### Amount Storage:
- Amounts are stored in minor units (pence) to avoid floating point issues
- Example: £12.34 is stored as 1234
- To convert: divide by 100 to get pounds

### User Roles:
- **Employee (RoleId=1)**: Can create and submit their own expenses
- **Manager (RoleId=2)**: Can approve or reject submitted expenses

## Sample Data

### Users:
- Alice Example (UserId=1, Employee, alice@example.co.uk)
- Bob Manager (UserId=2, Manager, bob.manager@example.co.uk)

### Common Operations:

#### Creating an Expense:
```
POST /api/expenses
{
  "userId": 1,
  "categoryId": 1,
  "amount": 25.40,
  "expenseDate": "2025-11-20",
  "description": "Taxi from airport"
}
```

#### Getting Expenses:
- All expenses: GET /api/expenses
- By user: GET /api/expenses?userId=1
- By status: GET /api/expenses?statusId=2 (submitted)
- With filter: GET /api/expenses?filter=taxi

#### Updating Expense Status:
```
PUT /api/expenses/{id}/status
{
  "statusId": 2,  // 2=Submitted, 3=Approved, 4=Rejected
  "reviewedBy": 2 // Manager's UserId (for approve/reject only)
}
```

## Natural Language Examples

When users ask questions, translate them to API calls:

- "Show me my expenses" → GET /api/expenses?userId=1
- "Add an expense for £50 for meals today" → POST /api/expenses with appropriate data
- "Approve expense 5" → PUT /api/expenses/5/status with statusId=3 and reviewedBy=2
- "What pending expenses do I have?" → GET /api/expenses?userId=X&statusId=2
- "Show all travel expenses" → GET /api/expenses?filter=Travel
- "Submit my expense 3" → PUT /api/expenses/3/status with statusId=2
