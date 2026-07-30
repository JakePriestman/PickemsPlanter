using System.Text;
using Xunit;

namespace PickemsPlanter.Services;

public class HltvRankingParserTests
{
	private readonly HltvRankingParser _parser = new();

	[Fact]
	public void Parse_ReturnsTeamsOrderedByGlobalRank_FromTheRealPageStructure()
	{
		// Arrange
		using var stream = File.OpenRead("../../../Services/hltvRanking.trimmed.html");

		// Act
		var result = _parser.Parse(stream);

		// Assert
		Assert.Equal(4, result.Count);
		Assert.Equal([1, 2, 50, 128], result.Select(t => t.GlobalRank));
		Assert.Equal(["Spirit", "Falcons", "SINNERS", "M&M Gaming"], result.Select(t => t.TeamName));
	}

	[Fact]
	public void Parse_DecodesHtmlEntitiesInTeamNames()
	{
		// Arrange
		using var stream = File.OpenRead("../../../Services/hltvRanking.trimmed.html");

		// Act
		var result = _parser.Parse(stream);

		// Assert — "M&amp;M Gaming" in the source must decode to a literal "&".
		Assert.Contains(result, t => t.TeamName == "M&M Gaming");
	}

	[Fact]
	public void Parse_ReturnsEmpty_WhenNoRankedTeamsFound()
	{
		// Arrange
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<html><body><p>Not a ranking page</p></body></html>"));

		// Act
		var result = _parser.Parse(stream);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void Parse_SkipsEntry_WhenPositionIsMissing()
	{
		// Arrange
		const string html = """
			<div class="ranked-team standard-box">
			  <div class="ranking-header"><span class="team-logo"></span>
			    <div class="teamLine"><span class="name">NoPosition</span></div>
			  </div>
			</div>
			""";

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

		// Act
		var result = _parser.Parse(stream);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void Parse_SkipsEntry_WhenNameIsMissing()
	{
		// Arrange
		const string html = """
			<div class="ranked-team standard-box">
			  <div class="ranking-header"><span class="position wide-position">#7</span>
			    <div class="teamLine"></div>
			  </div>
			</div>
			""";

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

		// Act
		var result = _parser.Parse(stream);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void Parse_KeepsOnlyTheHigherRank_WhenTheSameTeamProfileLinkAppearsTwice()
	{
		// Arrange — confirmed against a real saved HLTV page: a regional-ranking tab left
		// mounted in the DOM re-lists a team under its own (smaller) region-only position,
		// alongside its real entry in the Global tab. Same team profile link, two different
		// #ranks — the true global rank is always the larger number.
		const string html = """
			<div class="ranked-team standard-box">
			  <span class="position wide-position">#18</span>
			  <span class="name">BIG</span>
			  <a href="/team/7532/big" class="moreLink">HLTV Team profile</a>
			</div>
			<div class="ranked-team standard-box">
			  <span class="position wide-position">#202</span>
			  <span class="name">BIG</span>
			  <a href="/team/7532/big" class="moreLink">HLTV Team profile</a>
			</div>
			""";

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

		// Act
		var result = _parser.Parse(stream);

		// Assert
		var big = Assert.Single(result);
		Assert.Equal(202, big.GlobalRank);
	}

	[Fact]
	public void Parse_KeepsBothEntries_WhenDifferentTeamsGenuinelyShareTheSameDisplayName()
	{
		// Arrange — dedupe is keyed on the team's profile link, not its name, so two distinct
		// teams that happen to share a display name aren't silently collapsed into one.
		const string html = """
			<div class="ranked-team standard-box">
			  <span class="position wide-position">#5</span>
			  <span class="name">Nemesis</span>
			  <a href="/team/1/nemesis-eu" class="moreLink">HLTV Team profile</a>
			</div>
			<div class="ranked-team standard-box">
			  <span class="position wide-position">#310</span>
			  <span class="name">Nemesis</span>
			  <a href="/team/2/nemesis-sa" class="moreLink">HLTV Team profile</a>
			</div>
			""";

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

		// Act
		var result = _parser.Parse(stream);

		// Assert
		Assert.Equal(2, result.Count);
		Assert.Equal([5, 310], result.Select(t => t.GlobalRank));
	}
}
