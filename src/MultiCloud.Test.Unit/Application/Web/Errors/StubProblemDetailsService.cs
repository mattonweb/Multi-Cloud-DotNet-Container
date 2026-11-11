namespace MultiCloud.Test.Unit.Application.Web.Errors;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Keeps each problem the exception handler writes, instead of writing it to a response, so a test can read it.
/// </summary>
internal sealed class StubProblemDetailsService : IProblemDetailsService
{
    private readonly List<ProblemDetailsContext> _written = [];

    /// <summary>
    /// Gets each problem written, in order.
    /// </summary>
    internal IReadOnlyList<ProblemDetailsContext> Written => this._written;

    /// <inheritdoc/>
    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this._written.Add(context);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Keeps the problem and says it was written, as the framework's service does once a writer has written it. The
    /// interface's own default answers <see langword="false"/>, so it is replaced here.
    /// </summary>
    /// <param name="context">The problem and the request it answers.</param>
    /// <returns><see langword="true"/>, always.</returns>
    public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this._written.Add(context);
        return ValueTask.FromResult(true);
    }
}
