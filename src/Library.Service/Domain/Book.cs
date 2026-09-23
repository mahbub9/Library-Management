namespace Library.Service.Domain;

public class Book
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Author { get; set; }

    public int Pages { get; set; }
}
