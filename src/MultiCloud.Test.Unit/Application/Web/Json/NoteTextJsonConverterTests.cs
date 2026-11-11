namespace MultiCloud.Test.Unit.Application.Web.Json;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Application.Web.Json;
using MultiCloud.Application.Web.Models;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="NoteTextJsonConverter"/> class: a body whose text a
/// note cannot hold is refused as JSON, which the API controller answers with 400 and problem details.
/// </summary>
[TestClass]
public sealed class NoteTextJsonConverterTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that empty text is refused.
    /// </summary>
    [TestMethod]
    public void Read_EmptyText_ThrowsJsonException()
    {
        // Arrange (Given)
        const string body = """{ "text": "" }""";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            JsonSerializer.Deserialize<NoteCreateBody>(body, CreateOptions());
        }
        catch (JsonException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Empty text should be refused with JsonException.");
    }

    /// <summary>
    /// Unit test to verify that text over 200 characters is refused.
    /// </summary>
    [TestMethod]
    public void Read_TwoHundredOneCharacters_ThrowsJsonException()
    {
        // Arrange (Given)
        string body = $$"""{ "text": "{{new string('a', 201)}}" }""";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            JsonSerializer.Deserialize<NoteCreateBody>(body, CreateOptions());
        }
        catch (JsonException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Text of 201 characters should be refused with JsonException.");
    }

    /// <summary>
    /// Unit test to verify that valid text is read, and that a body that leaves out done means not done.
    /// </summary>
    [TestMethod]
    public void Read_ValidText_ReadsTheText()
    {
        // Arrange (Given)
        const string body = """{ "text": "Read by a test." }""";

        // Act (When)
        NoteCreateBody? read = JsonSerializer.Deserialize<NoteCreateBody>(body, CreateOptions());

        // Assert (Then)
        Assert.IsNotNull(
            read,
            "A valid body should be read.");
        Assert.AreEqual(
            new NoteText("Read by a test."),
            read.Text,
            "The text should be read as sent.");
        Assert.IsNull(
            read.Done,
            "A body that leaves out done should leave it unset.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates serializer options with the converter, as the application registers it.
    /// </summary>
    /// <returns>The options.</returns>
    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.Converters.Add(new NoteTextJsonConverter());
        return options;
    }

    #endregion Private Methods
}
