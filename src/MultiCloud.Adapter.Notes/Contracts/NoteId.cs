namespace MultiCloud.Adapter.Notes.Contracts;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Identifies one note.
/// </summary>
/// <remarks>
/// Unlike the text, an identifier is not checked when it is made. Entity Framework gives a new note a placeholder
/// identifier until the database assigns the real one, and it builds that placeholder through this constructor. An
/// identifier no note has is simply not found.
/// </remarks>
public readonly record struct NoteId : IParsable<NoteId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoteId"/> class.
    /// </summary>
    /// <param name="value">The identifier the database gave the note.</param>
    public NoteId(long value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Reads an identifier from its rendering, the number in invariant culture.
    /// </summary>
    /// <param name="s">The rendering, named as the interface names it (Sonar S927).</param>
    /// <param name="provider">Ignored; the rendering is always invariant.</param>
    /// <returns>The identifier.</returns>
    public static NoteId Parse(string s, IFormatProvider? provider)
    {
        long value = long.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
        return new NoteId(value);
    }

    /// <summary>
    /// Reads an identifier from its rendering, the number in invariant culture, without throwing.
    /// </summary>
    /// <param name="s">The rendering, named as the interface names it (Sonar S927).</param>
    /// <param name="provider">Ignored; the rendering is always invariant.</param>
    /// <param name="result">The identifier, when the rendering is one.</param>
    /// <returns><see langword="true"/> when the rendering is an identifier.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out NoteId result)
    {
        bool parsed = long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value);
        result = new NoteId(value);
        return parsed;
    }

    /// <summary>
    /// Renders the identifier as the number in invariant culture, as it appears in a URL.
    /// </summary>
    /// <returns>The rendering.</returns>
    public override string ToString()
    {
        return this.Value.ToString(CultureInfo.InvariantCulture);
    }
}
