using FluentAssertions;
using Kursserver.Dto;

namespace Kursserver.Tests.Dto;

public class ExerciseRequestTests
{
    // --- Topic validation ---

    [Fact]
    public void IsValid_EmptyTopic_ReturnsFalse()
    {
        var request = new ExerciseRequest { Topic = "" };
        var result = request.IsValid(out var error);
        result.Should().BeFalse();
        error.Should().Be("Topic is required");
    }

    [Fact]
    public void IsValid_WhitespaceTopic_ReturnsFalse()
    {
        var request = new ExerciseRequest { Topic = "   " };
        var result = request.IsValid(out var error);
        result.Should().BeFalse();
        error.Should().Be("Topic is required");
    }

    [Fact]
    public void IsValid_ArbitraryNonEmptyTopic_ReturnsTrue()
    {
        var request = new ExerciseRequest { Topic = "Some Unknown Topic", Difficulty = 3 };
        var result = request.IsValid(out var error);
        result.Should().BeTrue();
        error.Should().BeEmpty();
    }

    // --- Difficulty validation ---

    [Fact]
    public void IsValid_DifficultyZero_ReturnsFalse()
    {
        var request = new ExerciseRequest { Topic = "Functions", Difficulty = 0 };
        var result = request.IsValid(out var error);
        result.Should().BeFalse();
        error.Should().Be("Difficulty level must be between 1 and 5");
    }

    [Fact]
    public void IsValid_DifficultyAboveFive_ReturnsFalse()
    {
        var request = new ExerciseRequest { Topic = "Functions", Difficulty = 6 };
        var result = request.IsValid(out var error);
        result.Should().BeFalse();
        error.Should().Be("Difficulty level must be between 1 and 5");
    }

    [Fact]
    public void IsValid_NegativeDifficulty_ReturnsFalse()
    {
        var request = new ExerciseRequest { Topic = "Functions", Difficulty = -1 };
        var result = request.IsValid(out var error);
        result.Should().BeFalse();
        error.Should().Be("Difficulty level must be between 1 and 5");
    }

    [Fact]
    public void IsValid_DifficultyOne_ReturnsTrue()
    {
        var request = new ExerciseRequest { Topic = "Functions", Difficulty = 1 };
        var result = request.IsValid(out var error);
        result.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_DifficultyFive_ReturnsTrue()
    {
        var request = new ExerciseRequest { Topic = "Functions", Difficulty = 5 };
        var result = request.IsValid(out var error);
        result.Should().BeTrue();
        error.Should().BeEmpty();
    }

    // --- DOM/Events JS-only restriction ---

    [Fact]
    public void IsValid_DomBasicsWithPython_ReturnsFalse()
    {
        var request = new ExerciseRequest { Topic = "DOM Basics", Language = "Python", Difficulty = 2 };
        var result = request.IsValid(out var error);
        result.Should().BeFalse();
        error.Should().Be("'DOM Basics' is only available for JavaScript");
    }

    [Fact]
    public void IsValid_EventsWithCSharp_ReturnsFalse()
    {
        var request = new ExerciseRequest { Topic = "Events", Language = "C#", Difficulty = 2 };
        var result = request.IsValid(out var error);
        result.Should().BeFalse();
        error.Should().Be("'Events' is only available for JavaScript");
    }

    [Fact]
    public void IsValid_DomBasicsWithJavaScript_ReturnsTrue()
    {
        var request = new ExerciseRequest { Topic = "DOM Basics", Language = "JavaScript", Difficulty = 2 };
        var result = request.IsValid(out var error);
        result.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_DomBasicsCaseInsensitive_ReturnsFalse()
    {
        var request = new ExerciseRequest { Topic = "dom basics", Language = "Python", Difficulty = 2 };
        var result = request.IsValid(out var error);
        result.Should().BeFalse();
        error.Should().Be("'dom basics' is only available for JavaScript");
    }

    [Fact]
    public void IsValid_DomShortformWithJavaScript_ReturnsTrue()
    {
        var request = new ExerciseRequest { Topic = "dom", Language = "JavaScript", Difficulty = 2 };
        var result = request.IsValid(out var error);
        result.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_DomTopicWithJsShorthand_ReturnsTrue()
    {
        var request = new ExerciseRequest { Topic = "DOM Basics", Language = "js", Difficulty = 2 };
        var result = request.IsValid(out var error);
        result.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_EventsWithJavaScript_ReturnsTrue()
    {
        var request = new ExerciseRequest { Topic = "Events", Language = "JavaScript", Difficulty = 2 };
        var result = request.IsValid(out var error);
        result.Should().BeTrue();
        error.Should().BeEmpty();
    }

    // --- Happy path ---

    [Fact]
    public void IsValid_ValidTopicLanguageDifficulty_ReturnsTrue()
    {
        var request = new ExerciseRequest { Topic = "Arrays", Language = "Python", Difficulty = 3 };
        var result = request.IsValid(out var error);
        result.Should().BeTrue();
        error.Should().BeEmpty();
    }
}
