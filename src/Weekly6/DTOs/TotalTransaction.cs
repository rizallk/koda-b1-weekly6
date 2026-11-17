namespace Weekly6.DTOs;

public sealed record TotalTransaction
(
  decimal TopUp,
  decimal Income,
  decimal Expense
);