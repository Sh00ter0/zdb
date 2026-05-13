using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Logging;

public class LogSanitizerEnricher : ILogEventEnricher
{
    // Prevent log bombing by limiting the maximum length of logged strings
    private const int MaxStringLength = 4000;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties.ToList())
        {
            var sanitizedValue = SanitizePropertyValue(property.Value);

            if (!ReferenceEquals(sanitizedValue, property.Value))
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, sanitizedValue));
            }
        }
    }

    private LogEventPropertyValue SanitizePropertyValue(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue scalar => SanitizeScalar(scalar),
            SequenceValue sequence => SanitizeSequence(sequence),
            StructureValue structure => SanitizeStructure(structure),
            DictionaryValue dictionary => SanitizeDictionary(dictionary),
            _ => value
        };
    }

    private ScalarValue SanitizeScalar(ScalarValue scalar)
    {
        if (scalar.Value is string stringValue)
        {
            var sanitized = SanitizeString(stringValue);
            if (!ReferenceEquals(sanitized, stringValue))
            {
                return new ScalarValue(sanitized);
            }
        }
        return scalar;
    }

    private string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var resultString = input;

        // Log bombing protection with UTF-16 surrogate pair safety
        if (resultString.Length > MaxStringLength)
        {
            var length = MaxStringLength;

            if (char.IsHighSurrogate(resultString[length - 1]))
            {
                length--;
            }

            resultString = resultString[..length] + "...[TRUNCATED]";
        }

        // Fast check to avoid allocations if the string is already clean
        var needsSanitization = false;
        foreach (var c in resultString)
        {
            if (c == '\r' || c == '\n' || (char.IsControl(c) && c != '\t'))
            {
                needsSanitization = true;
                break;
            }
        }

        if (!needsSanitization) return resultString;

        // Allocation friendly sanitization
        var builder = new StringBuilder(resultString.Length);
        foreach (var c in resultString)
        {
            if (c == '\r' || c == '\n')
            {
                builder.Append('_');
            }
            // Preserve tabs \t, strip out other control chars
            else if (c == '\t' || !char.IsControl(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private SequenceValue SanitizeSequence(SequenceValue sequence)
    {
        var modified = false;
        var newElements = new List<LogEventPropertyValue>(sequence.Elements.Count);

        foreach (var element in sequence.Elements)
        {
            var sanitized = SanitizePropertyValue(element);
            newElements.Add(sanitized);
            if (!ReferenceEquals(sanitized, element)) modified = true;
        }

        return modified ? new SequenceValue(newElements) : sequence;
    }

    private StructureValue SanitizeStructure(StructureValue structure)
    {
        var modified = false;
        var newProperties = new List<LogEventProperty>(structure.Properties.Count);

        foreach (var prop in structure.Properties)
        {
            var sanitizedValue = SanitizePropertyValue(prop.Value);
            newProperties.Add(new LogEventProperty(prop.Name, sanitizedValue));

            if (!ReferenceEquals(sanitizedValue, prop.Value)) modified = true;
        }

        return modified ? new StructureValue(newProperties, structure.TypeTag) : structure;
    }

    private DictionaryValue SanitizeDictionary(DictionaryValue dictionary)
    {
        var modified = false;
        var newElements = new Dictionary<ScalarValue, LogEventPropertyValue>();

        foreach (var kvp in dictionary.Elements)
        {
            var sanitizedKey = SanitizeScalar(kvp.Key);
            var sanitizedValue = SanitizePropertyValue(kvp.Value);

            newElements[sanitizedKey] = sanitizedValue;

            if (!ReferenceEquals(sanitizedKey, kvp.Key) || !ReferenceEquals(sanitizedValue, kvp.Value))
            {
                modified = true;
            }
        }

        return modified ? new DictionaryValue(newElements) : dictionary;
    }
}