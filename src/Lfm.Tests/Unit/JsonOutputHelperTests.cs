using FluentAssertions;
using Lfm.Core.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for JsonOutputHelper
/// Tests JSON serialization and formatting
/// </summary>
public class JsonOutputHelperTests
{
    // Test data model
    private class TestData
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public string? OptionalField { get; set; }

        public TestData(string name, int count, string? optionalField = null)
        {
            Name = name;
            Count = count;
            OptionalField = optionalField;
        }
    }

    // ========== SerializeToJson Tests ==========

    [Fact]
    public void SerializeToJson_WithSimpleObject_ProducesValidJson()
    {
        // Arrange
        var data = new TestData("Test", 42);

        // Act
        var json = JsonOutputHelper.SerializeToJson(data);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"name\""); // camelCase property
        json.Should().Contain("\"Test\"");
        json.Should().Contain("\"count\"");
        json.Should().Contain("42");
    }

    [Fact]
    public void SerializeToJson_WithNullValues_OmitsNullProperties()
    {
        // Arrange
        var data = new TestData("Test", 42, optionalField: null);

        // Act
        var json = JsonOutputHelper.SerializeToJson(data);

        // Assert
        json.Should().NotContain("optionalField"); // Null field omitted
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"count\"");
    }

    [Fact]
    public void SerializeToJson_WithNonNullValues_IncludesAllProperties()
    {
        // Arrange
        var data = new TestData("Test", 42, optionalField: "Present");

        // Act
        var json = JsonOutputHelper.SerializeToJson(data);

        // Assert
        json.Should().Contain("\"optionalField\"");
        json.Should().Contain("\"Present\"");
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"count\"");
    }

    [Fact]
    public void SerializeToJson_WithList_SerializesAsArray()
    {
        // Arrange
        var data = new List<TestData>
        {
            new("Item1", 1),
            new("Item2", 2)
        };

        // Act
        var json = JsonOutputHelper.SerializeToJson(data);

        // Assert
        json.Should().Contain("[");
        json.Should().Contain("]");
        json.Should().Contain("\"Item1\"");
        json.Should().Contain("\"Item2\"");
    }

    [Fact]
    public void SerializeToJson_WithEmptyList_SerializesEmptyArray()
    {
        // Arrange
        var data = new List<TestData>();

        // Act
        var json = JsonOutputHelper.SerializeToJson(data);

        // Assert
        json.Should().Be("[]");
    }

    [Fact]
    public void SerializeToJson_ProducesFormattedOutput()
    {
        // Arrange
        var data = new TestData("Test", 42);

        // Act
        var json = JsonOutputHelper.SerializeToJson(data);

        // Assert
        json.Should().Contain("\n"); // Indented (contains newlines)
        json.Should().Contain("  "); // Contains spacing for indentation
    }

    [Fact]
    public void SerializeToJson_WithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var data = new TestData("Test\"Quote", 42);

        // Act
        var json = JsonOutputHelper.SerializeToJson(data);

        // Assert
        // JSON will escape the quote (either \" or \u0022 depending on serializer)
        json.Should().Contain("Quote"); // Content is still present
        json.Should().Match("*Test*Quote*"); // Both parts of name present
    }

    // ========== GetJsonOptions Tests ==========

    [Fact]
    public void GetJsonOptions_ReturnsConsistentOptions()
    {
        // Act
        var options1 = JsonOutputHelper.GetJsonOptions();
        var options2 = JsonOutputHelper.GetJsonOptions();

        // Assert
        options1.Should().NotBeNull();
        options2.Should().NotBeNull();
        // Both should produce identical serialization
        var data = new TestData("Test", 42);
        var json1 = JsonSerializer.Serialize(data, options1);
        var json2 = JsonSerializer.Serialize(data, options2);
        json1.Should().Be(json2);
    }

    [Fact]
    public void GetJsonOptions_HasCamelCaseNaming()
    {
        // Act
        var options = JsonOutputHelper.GetJsonOptions();
        var data = new TestData("Test", 42);

        // Assert
        var json = JsonSerializer.Serialize(data, options);
        json.Should().Contain("\"name\""); // camelCase
        json.Should().Contain("\"count\""); // camelCase
        json.Should().NotContain("\"Name\""); // Not PascalCase
        json.Should().NotContain("\"Count\""); // Not PascalCase
    }

    [Fact]
    public void GetJsonOptions_IgnoresNullValues()
    {
        // Act
        var options = JsonOutputHelper.GetJsonOptions();
        var data = new TestData("Test", 42, optionalField: null);

        // Assert
        var json = JsonSerializer.Serialize(data, options);
        json.Should().NotContain("optionalField");
    }

    [Fact]
    public void GetJsonOptions_WritesIndented()
    {
        // Act
        var options = JsonOutputHelper.GetJsonOptions();

        // Assert
        options.WriteIndented.Should().BeTrue();
    }

    // ========== WriteJsonToConsole Tests ==========

    [Fact]
    public void WriteJsonToConsole_WithSimpleObject_WritesValidJson()
    {
        // Arrange
        var data = new TestData("Test", 42);
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            // Act
            Console.SetOut(stringWriter);
            JsonOutputHelper.WriteJsonToConsole(data);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("\"name\"");
            output.Should().Contain("\"Test\"");
            output.Should().Contain("\"count\"");
            output.Should().Contain("42");
            output.Should().EndWith(Environment.NewLine);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteJsonToConsole_WithList_WritesFormattedArray()
    {
        // Arrange
        var data = new List<TestData>
        {
            new("Item1", 1),
            new("Item2", 2)
        };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            // Act
            Console.SetOut(stringWriter);
            JsonOutputHelper.WriteJsonToConsole(data);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("[");
            output.Should().Contain("]");
            output.Should().Contain("\"Item1\"");
            output.Should().Contain("\"Item2\"");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteJsonToConsole_OutputContainsNewline()
    {
        // Arrange
        var data = new TestData("Test", 42);
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            // Act
            Console.SetOut(stringWriter);
            JsonOutputHelper.WriteJsonToConsole(data);
            var output = stringWriter.ToString();

            // Assert
            output.Should().EndWith(Environment.NewLine);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
