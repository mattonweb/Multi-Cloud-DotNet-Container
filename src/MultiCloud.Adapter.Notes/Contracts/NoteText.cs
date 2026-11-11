namespace MultiCloud.Adapter.Notes.Contracts;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// What a note says: 1 to 200 characters, and something other than white space.
/// </summary>
public readonly record struct NoteText : IParsable<NoteText>
{
    /// <summary>
    /// The most characters a note's text may hold.
    /// </summary>
    public const int MaximumLength = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoteText"/> class.
    /// </summary>
    /// <param name="value">The text; it must contain something other than white space, and at most
    /// <see cref="MaximumLength"/> characters.</param>
    public NoteText(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaximumLength);
        this.Value = value;
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Reads a note's text from its rendering, which is the text itself.
    /// </summary>
    /// <param name="s">The rendering, named as the interface names it (Sonar S927).</param>
    /// <param name="provider">Ignored; text has no culture.</param>
    /// <returns>The note's text.</returns>
    public static NoteText Parse(string s, IFormatProvider? provider)
    {
        return new NoteText(s);
    }

    /// <summary>
    /// Reads a note's text from its rendering, which is the text itself, without throwing.
    /// </summary>
    /// <param name="s">The rendering, named as the interface names it (Sonar S927).</param>
    /// <param name="provider">Ignored; text has no culture.</param>
    /// <param name="result">The note's text, when the rendering is valid.</param>
    /// <returns><see langword="true"/> when the rendering is a valid note's text.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out NoteText result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s) || s.Length > MaximumLength)
        {
            return false;
        }

        result = new NoteText(s);
        return true;
    }

    /// <summary>
    /// Renders the note's text, which is the text itself.
    /// </summary>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        return this.Value;
    }
}
