using FluentAssertions;
using Kursserver.Utils;

namespace Kursserver.Tests.Parsers;

public class ExerciseResponseParserTests
{
    private const string ValidResponse = """
        TITLE:
        Sum Two Numbers
        DESCRIPTION:
        Write a function that sums two integers.
        EXAMPLE:
        sum(2, 3) returns 5
        ASSUMPTIONS:
        Inputs are valid integers.
        FUNCTION SIGNATURE:
        int Sum(int a, int b)
        SOLUTION:
        return a + b;
        ASSERTS:
        // Adding two positives
        assert Sum(2, 3) == 5
        // Adding zero
        assert Sum(0, 0) == 0
        """;

    [Fact]
    public void ParseAssertResponse_ExtractsTitle()
    {
        var result = ExerciseResponseParser.ParseAssertResponse(ValidResponse);
        result.Title.Should().Be("Sum Two Numbers");
    }

    [Fact]
    public void ParseAssertResponse_ExtractsDescription()
    {
        var result = ExerciseResponseParser.ParseAssertResponse(ValidResponse);
        result.Description.Should().Contain("sums two integers");
    }

    [Fact]
    public void ParseAssertResponse_ParsesTwoAsserts()
    {
        var result = ExerciseResponseParser.ParseAssertResponse(ValidResponse);
        result.Asserts.Should().HaveCount(2);
    }

    [Fact]
    public void ParseAssertResponse_AssertHasCommentAndCode()
    {
        var result = ExerciseResponseParser.ParseAssertResponse(ValidResponse);
        result.Asserts[0].Comment.Should().Be("// Adding two positives");
        result.Asserts[0].Code.Should().Be("assert Sum(2, 3) == 5");
    }

    [Fact]
    public void ParseAssertResponse_EmptyString_ReturnsEmptyFields()
    {
        var result = ExerciseResponseParser.ParseAssertResponse("");
        result.Title.Should().BeEmpty();
        result.Description.Should().BeEmpty();
        result.Asserts.Should().BeEmpty();
    }

    [Fact]
    public void ParseAssertResponse_MissingSection_ReturnsEmptyForThatField()
    {
        var result = ExerciseResponseParser.ParseAssertResponse("TITLE:\nHello\n");
        result.Description.Should().BeEmpty();
        result.Solution.Should().BeEmpty();
    }
}
