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
}
