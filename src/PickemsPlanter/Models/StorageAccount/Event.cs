using Azure;
using Azure.Data.Tables;

namespace PickemsPlanter.Models.StorageAccount;

public class Event: ITableEntity
{
	public required string PartitionKey { get; set; }
	public required string RowKey { get; set; }
	public DateTimeOffset? Timestamp { get; set; }
	public required string Name { get; init; }
	public required bool Disabled { get; set; }
	public int? PandaScoreStage1TournamentId { get; set; }
	public int? PandaScoreStage2TournamentId { get; set; }
	public int? PandaScoreStage3TournamentId { get; set; }
	public ETag ETag { get; set; }
}
