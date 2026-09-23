using Library.Service.Data;
using Library.Service.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Library.Service.Circulation;

public sealed class CirculationDesk(LibraryDbContext db)
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task<Loan> CheckOut(int patronId, int bookId, DateTimeOffset now, CancellationToken ct)
    {
        var patron = await db.Patrons.FirstOrDefaultAsync(p => p.Id == patronId, ct)
            ?? throw new CirculationRuleException(
                CirculationRule.PatronNotFound, $"Patron {patronId} is not registered.");

        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == bookId, ct)
            ?? throw new CirculationRuleException(
                CirculationRule.BookNotFound, $"Book {bookId} is not in the catalogue.");

        var alreadyOut = await db.Loans
            .AnyAsync(l => l.PatronId == patronId && l.BookId == bookId && l.ReturnedOn == null, ct);

        if (alreadyOut)
        {
            throw AlreadyHasIt(patron, book);
        }

        var loan = new Loan { BookId = book.Id, PatronId = patron.Id, CheckedOutOn = now };
        db.Loans.Add(loan);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException failure) when (IsDuplicateOpenLoan(failure))
        {
            // Two requests can both pass the check above; the filtered index rejects the loser.
            throw AlreadyHasIt(patron, book);
        }

        loan.Book = book;
        loan.Patron = patron;

        return loan;
    }

    public async Task<Loan> GetLoan(int loanId, CancellationToken ct) =>
        await db.Loans
            .Include(loan => loan.Book)
            .Include(loan => loan.Patron)
            .FirstOrDefaultAsync(loan => loan.Id == loanId, ct)
        ?? throw new CirculationRuleException(
            CirculationRule.LoanNotFound, $"Loan {loanId} does not exist.");

    private static CirculationRuleException AlreadyHasIt(Patron patron, Book book) =>
        new(CirculationRule.AlreadyCheckedOut, $"{patron.Name} already has \"{book.Title}\" out.");

    private static bool IsDuplicateOpenLoan(DbUpdateException failure) =>
        failure.InnerException is SqlException sql &&
        sql.Number is UniqueIndexViolation or UniqueConstraintViolation;
}
