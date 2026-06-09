namespace BackendAssignment.Domain.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime PublicationDate { get; set; }
    public int? CategoryId { get; set; }
    public int? AuthorId { get; set; }
    public int Pages { get; set; }
    public Author? Author { get; set; } = new();
    public Category? Category { get; set; } = new();
}