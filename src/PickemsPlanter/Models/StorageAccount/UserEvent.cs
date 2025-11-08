using Azure;
using Azure.Data.Tables;

namespace PickemsPlanter.Models.TableStorage
{
	public class UserEvent : ITableEntity
	{
		public required string PartitionKey { get; set; } //User Steam ID
		public required string RowKey { get; set; } //Event ID
		public required string AuthCode { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }
	}
}
