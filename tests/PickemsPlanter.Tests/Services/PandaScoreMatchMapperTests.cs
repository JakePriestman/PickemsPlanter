using Xunit;

namespace PickemsPlanter.Services;

public class PandaScoreMatchMapperTests
{
	[Fact]
	public void ResolveTeamName_MatchesSuffixVariant_EvenWhenAShortUnrelatedNameIsBuriedInsideTheTarget()
	{
		// Regression: HLTV lists Team Liquid as "Liquid", and separately lists an unrelated
		// team called "AM". A naive Contains() substring check treats "am" as a match for
		// "TeamLiquid" too (it sits inside "te-AM-liquid" once normalized), which made this
		// ambiguous (2 candidates) and blocked the otherwise-correct "Liquid" match.
		string[] candidates = ["Liquid", "AM", "AM"];

		// Act
		string? result = PandaScoreMatchMapper.ResolveTeamName(candidates, "Team Liquid");

		// Assert
		Assert.Equal("Liquid", result);
	}

	[Fact]
	public void ResolveTeamName_ReturnsExactMatch_IgnoringCase()
	{
		// Act
		string? result = PandaScoreMatchMapper.ResolveTeamName(["BIG", "NRG"], "big");

		// Assert
		Assert.Equal("BIG", result);
	}

	[Fact]
	public void ResolveTeamName_ReturnsNull_WhenNoCandidateRelates()
	{
		// Act
		string? result = PandaScoreMatchMapper.ResolveTeamName(["Falcons", "Vitality"], "Team Liquid");

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void ResolveTeamName_ReturnsNull_WhenGenuinelyAmbiguous()
	{
		// Arrange — both candidates are legitimate prefix/suffix variants of the target, so
		// this must still refuse rather than guess.
		string[] candidates = ["Team Alpha", "Alpha Team"];

		// Act
		string? result = PandaScoreMatchMapper.ResolveTeamName(candidates, "Alpha");

		// Assert
		Assert.Null(result);
	}
}
