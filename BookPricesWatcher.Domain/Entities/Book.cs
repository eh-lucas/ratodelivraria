namespace Sherlock.Domain.Entities;

public class Book
{
    public string Title { get; set; }
    public string Author { get; set; }
    public decimal Price { get; set; }
    public int Discount { get; set; }
    public string Isbn { get; set; }
    public string Editor { get; set; }
    public int PageNumber { get; set; }
    public string Language { get; set; }


    public Book(string title, string author, decimal price, int discount)
    {
        Title = title;
        Author = author;
        Price = price;
        Discount = discount;
    }

    public Book()
    {
    }
}

