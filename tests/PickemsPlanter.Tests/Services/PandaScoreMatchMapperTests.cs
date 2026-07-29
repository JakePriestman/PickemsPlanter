using Xunit;

namespace PickemsPlanter.Services;

public class PandaScoreMatchMapperTests
{
	[Fact]
	public void ResolveTeamName_MatchesSuffixVariant_EvenWhenAShortUnrelatedNameSharesTrailingLetters()
	{
		// Regression: HLTV lists Team Liquid as "Liquid", and separately lists an unrelated
		// team called "AM". A character-substring/prefix/suffix check (rather than whole-word)
		// treats "am" as a suffix match for "TeamLiquid" too, since "am" is the tail of the word
		// "team" — which made this ambiguous (2 candidates) and blocked the correct "Liquid" match.
		string[] candidates = ["Liquid", "AM", "AM"];

		// Act
		string? result = PandaScoreMatchMapper.ResolveTeamName(candidates, "Team Liquid");

		// Assert
		Assert.Equal("Liquid", result);
	}

	[Fact]
	public void ResolveTeamName_MatchesPrefixVariant_EvenWhenAShortUnrelatedNameSharesTrailingLetters()
	{
		// Regression: same class of bug as above, but the extra word is a suffix on the Steam
		// side instead of a prefix — HLTV lists "9z Team" as just "9z", and "AM" still collides
		// on the trailing letters of "Team" ("9z-TE-AM").
		string[] candidates = ["9z", "AM"];

		// Act
		string? result = PandaScoreMatchMapper.ResolveTeamName(candidates, "9z Team");

		// Assert
		Assert.Equal("9z", result);
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
