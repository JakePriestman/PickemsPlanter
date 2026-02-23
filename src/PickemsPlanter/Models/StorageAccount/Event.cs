using Azure;
using Azure.Data.Tables;

namespace PickemsPlanter.Models.StorageAccount
{
	public class Event: ITableEntity
	{
		public required string PartitionKey { get; set; }
		public required string RowKey { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public required string Name { get; init; }
		public ETag ETag { get; set; }
	}
}
