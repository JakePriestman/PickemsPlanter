using Azure;
using Azure.Data.Tables;

namespace PickemsPlanter.Models.StorageAccount;

public class Seed : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty; //Event Id
	public string RowKey { get; set; } = string.Empty; //Team name
	public int Rank { get; set; }
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}
