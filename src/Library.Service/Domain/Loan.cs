namespace Library.Service.Domain;

public class Loan
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public int PatronId { get; set; }

    public DateTimeOffset CheckedOutOn { get; set; }

    public DateTimeOffset? ReturnedOn { get; set; }

    public Book Book { get; set; } = null!;

    public Patron Patron { get; set; } = null!;

    public bool IsOut => ReturnedOn is null;
}
