using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookPricesWatcher.Model;
class Book
{
    public string Title { get; set; }
    public string Author { get; set; }
    public double Price { get; set; }
    public int Discount { get; set; }
    public Book(string title, string author, double price, int discount)
    {
        Title = title;
        Author = author;
        Price = price;
        Discount = discount;
    }
}
