namespace AzureP33.Models.Cosmos
{
    public class Product
    {
        public Guid   id            { get; set; }
        public Guid   categoryId    { get; set; }
        public String categoryName  { get; set; } = null!;
        public String sku           { get; set; } = null!;
        public String name          { get; set; } = null!;
        public String description   { get; set; } = null!;
        public double price         { get; set; }
    }
}
/*  "id": "3B52D15D-DF6C-4042-BA15-2EFEA8A2F852",
    "categoryId": "3E4CEACD-D007-46EB-82D7-31F6141752B2",
    "categoryName": "Components, Road Frames",
    "sku": "FR-R92B-62",
    "name": "HL Road Frame - Black, 62",
    "description": "The product called \"HL Road Frame - Black, 62\"",
    "price": 1431.5,
 */