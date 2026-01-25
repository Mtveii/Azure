using AzureP33.Models.Cosmos;

namespace AzureP33.Models.Home
{
    public class HomeCosmosViewModel
    {
        public List<Product> Products { get; set; } = new();
        public double RequestCharge { get; set; }

        // Список доступних груп (для select)
        public List<Category> AvailableCategories { get; set; } = new();

        // Обрані групи (GUID-и)
        public Guid[] SelectedCategoryIds { get; set; } = Array.Empty<Guid>();
    }
}
