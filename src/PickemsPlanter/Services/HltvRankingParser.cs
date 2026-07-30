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

		// (team href, rank, name) — the href (HLTV's own stable /team/{id}/{slug} link, present
		// on every card via the "HLTV Team profile" link) is the dedupe key below, not the name.
		List<(string Key, int Rank, string Name)> teams = [];

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

			// A saved page can carry a team's position twice: once from a regional-ranking tab
			// panel left in the DOM (HLTV's ranking page keeps previously-viewed tabs mounted
			// rather than removing them) and once from the actual Global tab — same team, two
			// different position numbers, since a regional pool is always smaller than the
			// global one. Confirmed against a real saved file: every duplicate pair shares the
			// same team profile href but a different #rank, and the Global (true) rank is
			// always the larger of the two. Fall back to the name itself when a card is missing
			// its profile link, so a single malformed entry doesn't get silently dropped.
			string href = node.SelectSingleNode(".//a[contains(concat(' ', normalize-space(@class), ' '), ' moreLink ')]")?.GetAttributeValue("href", "") ?? "";
			string key = href.Length > 0 ? href : teamName;

			teams.Add((key, rank, teamName));
		}

		return [.. teams
			.GroupBy(t => t.Key)
			.Select(g => g.OrderByDescending(t => t.Rank).First())
			.Select(t => new HltvRankedTeam(t.Rank, t.Name))
			.OrderBy(t => t.GlobalRank)];
	}
}
