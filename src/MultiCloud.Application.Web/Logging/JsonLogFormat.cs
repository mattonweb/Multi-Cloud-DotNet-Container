namespace MultiCloud.Application.Web.Logging;

using System.Globalization;
using Serilog.Formatting;
using Serilog.Templates;

/// <summary>
/// The one shape of a log line, in every environment: one JSON object per line, with the level as a <c>severity</c> a
/// log system can read (docs/SDD.md, decision D8).
/// </summary>
/// <remarks>
/// <para>
/// Each line starts with <c>time</c>, <c>severity</c> (<c>DEBUG</c>, <c>INFO</c>, <c>WARNING</c>, <c>ERROR</c> or
/// <c>CRITICAL</c>), the rendered <c>message</c>, and, when they exist, the <c>exception</c>, <c>traceId</c> and
/// <c>spanId</c>; then every property of the event, the OpenTelemetry names from <see cref="TelemetryEnricher"/> among
/// them. A value that does not exist is left off the line, never written empty.
/// </para>
/// <para>
/// Cloud Logging, among others, reads a line's level from <c>severity</c> and shows <c>message</c> as its summary; the
/// names are generic, so no log system is named in the code. The <c>Serilog</c> section of <c>appsettings.json</c>
/// names <see cref="Formatter"/> for the console, and the bootstrap logger uses it directly, so even a failure while
/// starting is written in this shape.
/// </para>
/// </remarks>
public static class JsonLogFormat
{
    /// <summary>
    /// The Serilog.Expressions template that writes one line. Serilog's levels map to the severities most log systems
    /// know: Verbose and Debug to DEBUG, Information to INFO, Warning to WARNING, Error to ERROR, Fatal to CRITICAL.
    /// </summary>
    internal const string Template =
        "{ {time: @t, "
        + "severity: if @l = 'Verbose' or @l = 'Debug' then 'DEBUG' "
        + "else if @l = 'Information' then 'INFO' "
        + "else if @l = 'Warning' then 'WARNING' "
        + "else if @l = 'Error' then 'ERROR' "
        + "else 'CRITICAL', "
        + "message: @m, exception: @x, traceId: @tr, spanId: @sp, ..@p} }\n";

    /// <summary>
    /// Gets the formatter that writes <see cref="Template"/>. Serilog's configuration reaches it by name, as
    /// <c>MultiCloud.Application.Web.Logging.JsonLogFormat::Formatter, MultiCloud.Application.Web</c>.
    /// </summary>
    public static ITextFormatter Formatter { get; } = new ExpressionTemplate(Template, CultureInfo.InvariantCulture);
}
