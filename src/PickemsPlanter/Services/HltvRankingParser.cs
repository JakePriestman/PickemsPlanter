using HtmlAgilityPack;

namespace PickemsPlanter.Services;

public record HltvRankedTeam(int GlobalRank, string TeamName);

public interface IHltvRankingParser
{
	IReadOnlyList<HltvRankedTeam> Parse(Stream htmlContent);
}

// Parses a browser-saved copy of HLTV's Valve Ranking page (hltv.org/valve-ranking/teams).
// That page blocks server-side/automated fetches (403, confirmed), so the admin saves it
// from their own browser and uploads the file instead — this only ever parses static HTML
// already on disk, no network access of its own.
//
// Markup (confirmed against a real saved copy): each team is a
// <div class="ranked-team standard-box"> containing <span class="position wide-position">#N</span>
// and, nested inside, <span class="name">Team Name</span>.
public class HltvRankingParser : IHltvRankingParser
{
	public IReadOnlyList<HltvRankedTeam> Parse(Stream htmlContent)
	{
		HtmlDocument document = new();
		document.Load(htmlContent);

		var teamNodes = document.DocumentNode.SelectNodes(
			"//div[contains(concat(' ', normalize-space(@class), ' '), ' ranked-team ')]");

		if (teamNodes is null)
			return [];

		List<HltvRankedTeam> teams = [];

		foreach (var node in teamNodes)
		{
			var positionNode = node.SelectSingleNode(
				".//span[contains(concat(' ', normalize-space(@class), ' '), ' position ')]");
			var nameNode = node.SelectSingleNode(".//span[@class='name']");

			if (positionNode is null || nameNode is null)
				continue;

			string positionText = HtmlEntity.DeEntitize(positionNode.InnerText).Trim().TrimStart('#');

			if (!int.TryParse(positionText, out int rank))
				continue;

			string teamName = HtmlEntity.DeEntitize(nameNode.InnerText).Trim();

			if (teamName.Length == 0)
				continue;

			teams.Add(new HltvRankedTeam(rank, teamName));
		}

		return [.. teams.OrderBy(t => t.GlobalRank)];
	}
}
