namespace MultiCloud.Test.Unit.Application.Web.Logging;

using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Application.Web.Logging;
using Serilog.Events;
using Serilog.Parsing;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="JsonLogFormat"/> class: every line is one JSON
/// object whose <c>severity</c> a log system can read, with the message, the exception and every property.
/// </summary>
[TestClass]
public sealed class JsonLogFormatTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that each of Serilog's levels is written as the severity most log systems know, so a log
    /// system's severity filters and error alerts see the application's lines.
    /// </summary>
    /// <param name="level">The event's level.</param>
    /// <param name="severity">The severity the line should carry.</param>
    [TestMethod]
    [DataRow(LogEventLevel.Verbose, "DEBUG")]
    [DataRow(LogEventLevel.Debug, "DEBUG")]
    [DataRow(LogEventLevel.Information, "INFO")]
    [DataRow(LogEventLevel.Warning, "WARNING")]
    [DataRow(LogEventLevel.Error, "ERROR")]
    [DataRow(LogEventLevel.Fatal, "CRITICAL")]
    public void Format_EachLevel_WritesItsSeverity(LogEventLevel level, string severity)
    {
        // Arrange (Given)
        LogEvent logEvent = CreateEvent(level, "A line at any level", exception: null);

        // Act (When)
        JsonElement line = Render(logEvent);

        // Assert (Then)
        Assert.AreEqual(
            severity,
            line.GetProperty("severity").GetString(),
            "The level should be written as the matching severity.");
    }

    /// <summary>
    /// Unit test to verify that the message is written rendered, with its values in place, as a log system shows it.
    /// </summary>
    [TestMethod]
    public void Format_TemplateWithProperty_WritesTheRenderedMessage()
    {
        // Arrange (Given)
        LogEvent logEvent = CreateEvent(LogEventLevel.Information, "Heartbeat {HeartbeatKind:l}", exception: null);

        // Act (When)
        JsonElement line = Render(logEvent);

        // Assert (Then)
        Assert.AreEqual(
            "Heartbeat Started",
            line.GetProperty("message").GetString(),
            "The message should be rendered with its values.");
    }

    /// <summary>
    /// Unit test to verify that an error's exception is written on its line, so the log system holds the stack.
    /// </summary>
    [TestMethod]
    public void Format_ErrorWithException_WritesTheException()
    {
        // Arrange (Given)
        InvalidOperationException exception = new InvalidOperationException("Failed in a test.");
        LogEvent logEvent = CreateEvent(LogEventLevel.Error, "A failure", exception);

        // Act (When)
        JsonElement line = Render(logEvent);

        // Assert (Then)
        StringAssert.Contains(
            line.GetProperty("exception").GetString(),
            "System.InvalidOperationException",
            "The line should carry the exception.");
    }

    /// <summary>
    /// Unit test to verify that a line with no exception and no trace leaves those keys off rather than writing them
    /// empty.
    /// </summary>
    [TestMethod]
    public void Format_NoExceptionOrTrace_LeavesThoseKeysOff()
    {
        // Arrange (Given)
        LogEvent logEvent = CreateEvent(LogEventLevel.Information, "A quiet line", exception: null);

        // Act (When)
        JsonElement line = Render(logEvent);

        // Assert (Then)
        Assert.IsFalse(
            line.TryGetProperty("exception", out _),
            "A line with no exception should have no exception key.");
        Assert.IsFalse(
            line.TryGetProperty("traceId", out _),
            "A line with no trace should have no traceId key.");
        Assert.IsTrue(
            line.TryGetProperty("time", out _),
            "Every line should carry its time.");
    }

    /// <summary>
    /// Unit test to verify that every property, a dotted OpenTelemetry name among them, is written as its own key, so
    /// a search for the session's identifier finds the line.
    /// </summary>
    [TestMethod]
    public void Format_DottedProperty_WritesItAsItsOwnKey()
    {
        // Arrange (Given)
        LogEvent logEvent = CreateEvent(LogEventLevel.Information, "Heartbeat {HeartbeatKind:l}", exception: null);

        // Act (When)
        JsonElement line = Render(logEvent);

        // Assert (Then)
        Assert.AreEqual(
            "019a6b2c-1d2e-7f30-8a41-b52c63d74e85",
            line.GetProperty("service.instance.id").GetString(),
            "A dotted property should be its own key.");
        Assert.AreEqual(
            "Started",
            line.GetProperty("HeartbeatKind").GetString(),
            "A message property should be its own key.");
    }

    /// <summary>
    /// Unit test to verify that each event is written as exactly one line, as the console sink and every platform's
    /// log collector expect.
    /// </summary>
    [TestMethod]
    public void Format_AnyEvent_WritesExactlyOneLine()
    {
        // Arrange (Given)
        LogEvent logEvent = CreateEvent(LogEventLevel.Warning, "A warning", exception: null);

        // Act (When)
        string text = Write(logEvent);

        // Assert (Then)
        StringAssert.EndsWith(
            text,
            "\n",
            "The line should end with a newline.");
        Assert.AreEqual(
            text.Length - 1,
            text.IndexOf('\n', StringComparison.Ordinal),
            "The only newline should be the last character.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates an event with a heartbeat kind and a session identifier among its properties.
    /// </summary>
    /// <param name="level">The event's level.</param>
    /// <param name="messageTemplate">The event's message template.</param>
    /// <param name="exception">The event's exception, or <see langword="null"/>.</param>
    /// <returns>The event.</returns>
    private static LogEvent CreateEvent(LogEventLevel level, string messageTemplate, Exception? exception)
    {
        MessageTemplateParser parser = new MessageTemplateParser();
        MessageTemplate template = parser.Parse(messageTemplate);
        LogEventProperty[] properties =
        [
            new LogEventProperty("HeartbeatKind", new ScalarValue("Started")),
            new LogEventProperty("service.instance.id", new ScalarValue("019a6b2c-1d2e-7f30-8a41-b52c63d74e85")),
        ];
        DateTimeOffset time = new DateTimeOffset(2026, 11, 11, 16, 5, 14, TimeSpan.Zero);
        return new LogEvent(time, level, exception, template, properties);
    }

    /// <summary>
    /// Writes an event with the format.
    /// </summary>
    /// <param name="logEvent">The event.</param>
    /// <returns>The text written.</returns>
    private static string Write(LogEvent logEvent)
    {
        using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
        {
            JsonLogFormat.Formatter.Format(logEvent, writer);
            return writer.ToString();
        }
    }

    /// <summary>
    /// Writes an event with the format and reads the line back as JSON.
    /// </summary>
    /// <param name="logEvent">The event.</param>
    /// <returns>The line's JSON object.</returns>
    private static JsonElement Render(LogEvent logEvent)
    {
        string text = Write(logEvent);
        using (JsonDocument document = JsonDocument.Parse(text))
        {
            return document.RootElement.Clone();
        }
    }

    #endregion Private Methods
}
