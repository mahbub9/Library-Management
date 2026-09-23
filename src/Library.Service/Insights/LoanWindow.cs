using Library.Service.Domain;

namespace Library.Service.Insights;

public static class LoanWindow
{
    // Half-open: a loan checked out exactly on "to" is excluded, so adjacent windows never both
    // count the same loan.
    public static IQueryable<Loan> InWindow(
        this IQueryable<Loan> loans, DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is not null)
        {
            loans = loans.Where(loan => loan.CheckedOutOn >= from);
        }

        if (to is not null)
        {
            loans = loans.Where(loan => loan.CheckedOutOn < to);
        }

        return loans;
    }
}
