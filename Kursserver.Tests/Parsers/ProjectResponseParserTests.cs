using FluentAssertions;
using Kursserver.Services;

namespace Kursserver.Tests.Parsers;

public class ProjectResponseParserTests
{
    private static string MakeResponse(string difficulty) =>
        "TITLE:\nMy Project\nDESCRIPTION:\nA cool project\nDIFFICULTY:\n" + difficulty +
        "\nTECH STACK:\nHTML\nLEARNING GOALS:\nLearn stuff\nUSER STORIES:\nAs a user\n" +
        "DESIGN SPECS:\nLooks nice\nASSETS NEEDED:\nNone\nSTARTER HTML:\n" +
        "SOLUTION HTML:\nSOLUTION CSS:\nSOLUTION JS:\nBONUS CHALLENGES:\nAdd animation\n";

    [Fact]
    public void Parse_ExtractsTitle()
    {
        var result = ProjectResponseParser.Parse(MakeResponse("2/5"));
        result.Title.Should().Be("My Project");
    }

    [Fact]
    public void Parse_Difficulty2of5_Returns2()
    {
        var result = ProjectResponseParser.Parse(MakeResponse("2/5"));
        result.Difficulty.Should().Be(2);
    }

    [Fact]
    public void Parse_Difficulty5of5_Returns5()
    {
        var result = ProjectResponseParser.Parse(MakeResponse("5/5"));
        result.Difficulty.Should().Be(5);
    }

    [Fact]
    public void Parse_Difficulty0of5_Returns0()
    {
        var result = ProjectResponseParser.Parse(MakeResponse("0/5"));
        result.Difficulty.Should().Be(0);
    }

    [Fact]
    public void Parse_MalformedDifficulty_Returns1()
    {
        var result = ProjectResponseParser.Parse(MakeResponse("hard"));
        result.Difficulty.Should().Be(1);
    }

    [Fact]
    public void Parse_EmptyDifficulty_Returns1()
    {
        var result = ProjectResponseParser.Parse(MakeResponse(""));
        result.Difficulty.Should().Be(1);
    }

    [Fact]
    public void Parse_SetsSuccessTrue()
    {
        var result = ProjectResponseParser.Parse(MakeResponse("3/5"));
        result.Success.Should().BeTrue();
    }
}
