namespace MultiCloud.Test.Unit.Adapter.Notes.Contracts;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="NoteText"/> struct: the validation rule for a
/// note's text, 1 to 200 characters and not only white space.
/// </summary>
[TestClass]
public sealed class NoteTextTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that text of exactly the maximum length is accepted.
    /// </summary>
    [TestMethod]
    public void Constructor_TwoHundredCharacters_KeepsTheText()
    {
        // Arrange (Given)
        string text = new string('a', 200);

        // Act (When)
        NoteText noteText = new NoteText(text);

        // Assert (Then)
        Assert.AreEqual(
            text,
            noteText.Value,
            "Text of exactly 200 characters should be kept.");
    }

    /// <summary>
    /// Unit test to verify that text one character over the maximum is refused.
    /// </summary>
    [TestMethod]
    public void Constructor_TwoHundredOneCharacters_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        string text = new string('a', 201);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new NoteText(text);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Text of 201 characters should be refused with ArgumentOutOfRangeException.");
    }

    /// <summary>
    /// Unit test to verify that text of only white space is refused, since a note must say something.
    /// </summary>
    [TestMethod]
    public void Constructor_OnlyWhiteSpace_ThrowsArgumentException()
    {
        // Arrange (Given)
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new NoteText("   ");
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Text of only white space should be refused with ArgumentException.");
    }

    /// <summary>
    /// Unit test to verify that parsing empty text answers false rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_EmptyText_ReturnsFalse()
    {
        // Arrange (Given)

        // Act (When)
        bool parsed = NoteText.TryParse(string.Empty, provider: null, out NoteText _);

        // Assert (Then)
        Assert.IsFalse(
            parsed,
            "Empty text should not parse.");
    }

    /// <summary>
    /// Unit test to verify that parsing text over the maximum answers false rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_TwoHundredOneCharacters_ReturnsFalse()
    {
        // Arrange (Given)
        string text = new string('a', 201);

        // Act (When)
        bool parsed = NoteText.TryParse(text, provider: null, out NoteText _);

        // Assert (Then)
        Assert.IsFalse(
            parsed,
            "Text of 201 characters should not parse.");
    }

    /// <summary>
    /// Unit test to verify that parsing valid text answers true with the text kept as given.
    /// </summary>
    [TestMethod]
    public void TryParse_ValidText_ReturnsTheText()
    {
        // Arrange (Given)

        // Act (When)
        bool parsed = NoteText.TryParse("A valid note.", provider: null, out NoteText noteText);

        // Assert (Then)
        Assert.IsTrue(
            parsed,
            "Valid text should parse.");
        Assert.AreEqual(
            "A valid note.",
            noteText.Value,
            "The parsed text should be kept as given.");
    }

    #endregion Public Methods
}
