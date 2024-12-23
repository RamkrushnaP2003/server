namespace BookApi.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Genre { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string ISBN { get; set; }
        public bool Trending { get; set; }
        public bool Bestseller { get; set; }
        public string Publisher { get; set; }
        public string ImageLink {get; set; } 
        public string UserId { get; set; }
    }
}
