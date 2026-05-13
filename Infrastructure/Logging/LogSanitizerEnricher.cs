using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Logging
{
    public class LogSanitizerEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var propertiesToUpdate = new List<LogEventProperty>();

            foreach (var property in logEvent.Properties)
            {
                if (property.Value is ScalarValue { Value: string stringValue } &&
                    (stringValue.Contains('\r') || stringValue.Contains('\n')))
                {
                    var sanitizedValue = stringValue.Replace("\r", "_").Replace("\n", "_");
                    propertiesToUpdate.Add(new LogEventProperty(property.Key, new ScalarValue(sanitizedValue)));
                }
            }

            foreach (var property in propertiesToUpdate)
            {
                logEvent.AddOrUpdateProperty(property);
            }
        }
    }
}
