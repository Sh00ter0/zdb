using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Infrastructure.Logging;

namespace Tests.Infrastructure.Logging;

public class LogSanitizerEnricherTests
{
    private static LogEvent CreateTestEvent(params LogEventProperty[] properties)
    {
        var template = new MessageTemplate("", Enumerable.Empty<MessageTemplateToken>());
        return new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null, template, properties);
    }

    [Theory]
    [InlineData("Safe log", "Safe log")]
    [InlineData("Error\rDetails", "Error_Details")]
    [InlineData("Error\nDetails", "Error_Details")]
    [InlineData("Multiple\r\nLines\n", "Multiple__Lines_")]
    [InlineData("Tab\tSeparated\0Null", "Tab\tSeparatedNull")]
    [InlineData("\x1b[31mRedText\x1b[0m", "[31mRedText[0m")]
    public void Enrich_WhenPropertyIsString_SanitizesCorrectly(string maliciousInput, string expectedOutput)
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();
        var logEvent = CreateTestEvent(new LogEventProperty("Message", new ScalarValue(maliciousInput)));

        // Act
        enricher.Enrich(logEvent, null!);

        // Assert
        var result = (ScalarValue)logEvent.Properties["Message"];
        Assert.Equal(expectedOutput, result.Value);
    }

    [Fact]
    public void Enrich_WhenPropertyIsNotString_DoesNotModifyValue()
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();
        var logEvent = CreateTestEvent(
            new LogEventProperty("StatusCode", new ScalarValue(404)),
            new LogEventProperty("IsSuccess", new ScalarValue(false))
        );

        // Act
        enricher.Enrich(logEvent, null!);

        // Assert
        Assert.Equal(404, ((ScalarValue)logEvent.Properties["StatusCode"]).Value);
        Assert.Equal(false, ((ScalarValue)logEvent.Properties["IsSuccess"]).Value);
    }

    [Fact]
    public void Enrich_WhenStringValueIsNull_DoesNotThrow()
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();
        var logEvent = CreateTestEvent(new LogEventProperty("NullValue", new ScalarValue(null)));

        // Act
        var exception = Record.Exception(() => enricher.Enrich(logEvent, null!));

        // Assert
        Assert.Null(exception);
        Assert.Null(((ScalarValue)logEvent.Properties["NullValue"]).Value);
    }

    [Fact]
    public void Enrich_WithDictionaryCollisions_HandlesGracefullyWithoutThrowing()
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();

        // Both keys will be sanitized to "key_"
        var dictElements = new Dictionary<ScalarValue, LogEventPropertyValue>
        {
            { new ScalarValue("key\r"), new ScalarValue("value1") },
            { new ScalarValue("key\n"), new ScalarValue("value2") }
        };

        var logEvent = CreateTestEvent(new LogEventProperty("Data", new DictionaryValue(dictElements)));

        // Act
        var exception = Record.Exception(() => enricher.Enrich(logEvent, null!));

        // Assert
        Assert.Null(exception);

        var updatedDictionary = (DictionaryValue)logEvent.Properties["Data"];

        Assert.Single(updatedDictionary.Elements);
        Assert.Equal("value2", ((ScalarValue)updatedDictionary.Elements.Single().Value).Value);
    }

    [Fact]
    public void Enrich_WithExtremelyLongString_TruncatesAndAppendsWarning()
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();
        var hugeString = new string('A', 5000);

        var logEvent = CreateTestEvent(new LogEventProperty("HugeData", new ScalarValue(hugeString)));

        // Act
        enricher.Enrich(logEvent, null!);

        // Assert
        var result = (ScalarValue)logEvent.Properties["HugeData"];
        var stringValue = (string)result.Value!;

        Assert.True(stringValue.Length <= 4050);
        Assert.EndsWith("[TRUNCATED]", stringValue);
        Assert.StartsWith(new string('A', 4000), stringValue);
    }

    [Fact]
    public void Enrich_WithSurrogatePairAtTruncationBoundary_SafelyStepsBack()
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();

        // Build a string where the 3999th and 4000th characters form a UTF-16 surrogate pair (emoji).
        // 3999 + 1 Emoji (2 chars) = 4001 characters total length.
        var emoji = "😀";
        var payload = new string('A', 3999) + emoji + "ExtraData";

        var logEvent = CreateTestEvent(new LogEventProperty("Payload", new ScalarValue(payload)));

        // Act
        enricher.Enrich(logEvent, null!);

        // Assert
        var result = (ScalarValue)logEvent.Properties["Payload"];
        var stringValue = (string)result.Value!;

        Assert.True(stringValue.Length <= 4050);
        Assert.EndsWith("[TRUNCATED]", stringValue);
        Assert.DoesNotContain(emoji, stringValue);
        Assert.StartsWith(new string('A', 3999), stringValue);
    }

    [Fact]
    public void Enrich_WithComplexStructure_RecursivelySanitizesValues()
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();

        var complexObject = new StructureValue(new[]
        {
            new LogEventProperty("SafeName", new ScalarValue("Ferdynand Kiepski")),
            new LogEventProperty("MaliciousName", new ScalarValue("Ferdynand\r\nKiepski"))
        });

        var logEvent = CreateTestEvent(new LogEventProperty("User", complexObject));

        // Act
        enricher.Enrich(logEvent, null!);

        // Assert
        var updatedStructure = (StructureValue)logEvent.Properties["User"];

        var safeNameValue = (ScalarValue)updatedStructure.Properties.Single(p => p.Name == "SafeName").Value;
        var maliciousNameValue = (ScalarValue)updatedStructure.Properties.Single(p => p.Name == "MaliciousName").Value;

        Assert.Equal("Ferdynand Kiepski", safeNameValue.Value);
        Assert.Equal("Ferdynand__Kiepski", maliciousNameValue.Value);
    }

    [Fact]
    public void Enrich_WithSequence_RecursivelySanitizesValues()
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();

        var sequence = new SequenceValue(new[]
        {
            new ScalarValue("Safe Item"),
            new ScalarValue("Bad\x1b[31mItem\r\n") // ANSI + New lines
        });

        var logEvent = CreateTestEvent(new LogEventProperty("Items", sequence));

        // Act
        enricher.Enrich(logEvent, null!);

        // Assert
        var updatedSequence = (SequenceValue)logEvent.Properties["Items"];
        var elements = updatedSequence.Elements.Cast<ScalarValue>().ToList();

        Assert.Equal("Safe Item", elements[0].Value);
        // The ESC control character is stripped, leaving the printable characters
        Assert.Equal("Bad[31mItem__", elements[1].Value);
    }

    [Fact]
    public void Enrich_WithDeeplyNestedStructures_RecursivelySanitizesAllValues()
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();

        var innerDictionary = new Dictionary<ScalarValue, LogEventPropertyValue>
        {
            { new ScalarValue("DeepKey"), new ScalarValue("Bad\r\nValue") }
        };

        var structureValue = new StructureValue(new[]
        {
            new LogEventProperty("NestedDictionary", new DictionaryValue(innerDictionary))
        });

        var sequence = new SequenceValue(new[] { structureValue });

        var logEvent = CreateTestEvent(new LogEventProperty("Data", sequence));

        // Act
        enricher.Enrich(logEvent, null!);

        // Assert
        var updatedSequence = (SequenceValue)logEvent.Properties["Data"];
        var updatedStructure = (StructureValue)updatedSequence.Elements.Single();
        var updatedDictionary = (DictionaryValue)updatedStructure.Properties.Single().Value;
        var sanitizedValue = (ScalarValue)updatedDictionary.Elements.Single().Value;

        Assert.Equal("Bad__Value", sanitizedValue.Value);
    }

    [Fact]
    public void Enrich_WhenValueDoesNotRequireSanitization_ReusesOriginalInstances()
    {
        // Arrange
        var enricher = new LogSanitizerEnricher();

        var originalDictionary = new Dictionary<ScalarValue, LogEventPropertyValue>
        {
            { new ScalarValue("Key"), new ScalarValue("Value") }
        };

        var originalStructure = new StructureValue(new[]
        {
            new LogEventProperty("Dict", new DictionaryValue(originalDictionary))
        });

        var originalSequence = new SequenceValue(new[] { originalStructure });

        var logEvent = CreateTestEvent(new LogEventProperty("Data", originalSequence));

        // Act
        enricher.Enrich(logEvent, null!);

        // Assert
        var updatedSequence = (SequenceValue)logEvent.Properties["Data"];

        // Since no sanitization was needed, the original instances should be reused
        Assert.Same(originalSequence, updatedSequence);
    }
}