using OpenQA.Selenium.DevTools.V127.Runtime;

namespace API.Domain;

public class Book
{
    public string Title { get; set; }
    public string Author { get; set; }
    public double Price { get; set; }
    public int Discount { get; set; }
    public string WebSite { get; set; }

    public Book(string title, string author, double price, int discount, string webSite)
    {
        Title = title;
        Author = author;
        Price = price;
        Discount = discount;
        WebSite = webSite;
    }

    public Book()
    {
    }
}

