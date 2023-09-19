using System;
using System.Collections.Generic;
using System.IO;
using RawDealView;
using Xunit;

namespace RawDeal.Tests;

public class DeckValidationTests
{
    [Theory]
    [MemberData(nameof(GetTestsAssociatedWithThisFolder), parameters: "01-ValidDecks")]
    public void TestValidDecks(string deckFolder, string testFile)
        => RunTest(deckFolder, testFile);

    [Theory]
    [MemberData(nameof(GetTestsAssociatedWithThisFolder), parameters: "02-InvalidDecks")]
    public void TestInvalidDecks(string deckFolder, string testFile)
        => RunTest(deckFolder, testFile);

    public static IEnumerable<object[]> GetTestsAssociatedWithThisFolder(string deckFolder)
    {
        deckFolder = Path.Combine("data", deckFolder);
        string testFolder = deckFolder + "-Tests"; 
        string[] testFiles = GetAllTestFilesFrom(testFolder);
        return ConvertDataIntoTheAppropriateFormat(deckFolder, testFiles);
    }
    
    private static string[] GetAllTestFilesFrom(string folder)
        => Directory.GetFiles(folder, "*.txt", SearchOption.TopDirectoryOnly);

    private static IEnumerable<object[]> ConvertDataIntoTheAppropriateFormat(string deckFolder, string[] testFiles)
    {
        var allData = new List<object[]>();
        foreach (var testFile in testFiles)
            allData.Add(new object []{deckFolder, testFile});
        return allData;
    }

    private void RunTest(string deckFolder, string testFile)
    {
        View view = View.BuildTestingView(testFile);
        Game game = new Game(view, deckFolder);
        game.Play();

        string[] actualScript = view.GetScript();
        string[] expectedScript = File.ReadAllLines(testFile);
        CompareScripts(actualScript, expectedScript);
    }

    private void CompareScripts(string[] actualScript, string[] expectedScript)
    {
        int numberOfLines = Math.Max(expectedScript.Length, actualScript.Length);
        for (int i = 0; i < numberOfLines; i++)
        {
            string expected = GetTheItemOrEmptyIfOutOfIndex(i, expectedScript);
            string actual = GetTheItemOrEmptyIfOutOfIndex(i, actualScript);
            Assert.Equal($"L{i+1}" + expected, $"L{i+1}" + actual);
        }
    }

    private string GetTheItemOrEmptyIfOutOfIndex(int index, string[] array)
        => index < array.Length ? array[index] : "";

}