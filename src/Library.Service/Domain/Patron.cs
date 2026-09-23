namespace Library.Service.Domain;

public class Patron
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateOnly JoinedOn { get; set; }
}
