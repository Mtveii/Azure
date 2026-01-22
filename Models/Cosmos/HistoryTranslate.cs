using AzureP33.Models.Orm;

namespace AzureP33.Models.Cosmos
{
    public class HistoryTranslate
    {
        public string categoryId { get; set; }
        public object userId { get; set; }
        public int time { get; set; }
        public trans_from from { get; set; }
        public trans_to to { get; set; }
    }
}
