using NUnit.Framework;
using SQLiteWrapperDotNetFive;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UnitTests
{
  public class Tests
  {
    private const string dbFileName = "UnitTest.db";
    private static SQLiteWrapper database;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      RemoveDb();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      RemoveDb();
    }

    [SetUp]
    public void SetUp()
    {
      RemoveDb($"{Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{TestContext.CurrentContext.Test.ID}_{dbFileName}")}");
      database = new SQLiteWrapper($"{Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{TestContext.CurrentContext.Test.ID}_{dbFileName}")}");
      Assert.That(database.RawCmd("CREATE TABLE paths (id INTEGER PRIMARY KEY AUTOINCREMENT, path VARCHAR(250) UNIQUE, age int(4))"), Is.True);
      Assert.That(database.Set("PathDBInstalled", "TRUE"), Is.True);
    }

    [TearDown]
    public void TearDown()
    {
      database.Dispose();
      RemoveDb($"{Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{TestContext.CurrentContext.Test.ID}_{dbFileName}")}");
    }

    [Test]
    public void SetTest()
    {
      Assert.That(database.Set("dbName", dbFileName), Is.True);
      Assert.That(database.Get("dbName"), Is.EqualTo(dbFileName));
    }

    [Test]
    [TestCaseSource(typeof(Tests), nameof(Tests.GetTestCases))]
    public string GetTest(string key)
    {
      return database.Get(key);
    }

    private static IEnumerable GetTestCases
    {
      get
      {
        yield return new TestCaseData("dbInstalled").Returns("TRUE");
        yield return new TestCaseData("PathDBInstalled").Returns("TRUE");
      }
    }

    [Test]
    public void GetSetUpdateTest()
    {
      Assert.That(database.Set("dbName", dbFileName), Is.True);
      Assert.That(database.Get("dbName"), Is.EqualTo(dbFileName));
      Assert.That(database.Set("dbName", nameof(GetSetUpdateTest)), Is.True);
      Assert.That(database.Get("dbName"), Is.EqualTo(nameof(GetSetUpdateTest)));
    }

    [Test]
    [TestCaseSource(typeof(Tests), nameof(Tests.InsertTestCases))]
    public bool InsertTest(string table, KeyValuePair<string, string> kvpField1, KeyValuePair<string, int> kvpField2)
    {
      //Assert.That(database.RawCmd("INSERT INTO paths (path, age) VALUES ('TestPath', '3')"), Is.True);
      Assert.That(database.RawCmd($"INSERT INTO {table} ({kvpField1.Key}, {kvpField2.Key}) VALUES ('{kvpField1.Value}', '{kvpField2.Value}')"), Is.True);
      var results = database.Select($"SELECT {kvpField1.Key}, {kvpField2.Key} FROM {table}");
      var rows = results.Select();
      Assert.That(rows, Is.Not.Empty);
      Assert.That(rows, Has.Length.EqualTo(1));
      Assert.That(rows[0][$"{kvpField1.Key}"], Is.EqualTo(kvpField1.Value));
      Assert.That(rows[0][$"{kvpField2.Key}"], Is.EqualTo(kvpField2.Value));
      return true;
    }

    private static IEnumerable InsertTestCases
    {
      get
      {
        yield return new TestCaseData("paths", new KeyValuePair<string, string>("path", "TestPath"), new KeyValuePair<string, int>("age", 3)).Returns(true);
        yield return new TestCaseData("paths", new KeyValuePair<string, string>("path", "TestPath2"), new KeyValuePair<string, int>("age", 13)).Returns(true);
      }
    }

    [Test]
    public void DeleteTest()
    {
      Assert.That(InsertTest("paths", new KeyValuePair<string, string>("path", "TestPath"), new KeyValuePair<string, int>("age", 3)), Is.True);
      var results = database.Select($"SELECT * FROM paths");
      var rows = results.Select();
      Assert.That(rows, Is.Not.Empty);
      Assert.That(rows, Has.Length.EqualTo(1));
      Assert.That(database.RawCmd($"DELETE FROM paths"), Is.True);
      results = database.Select($"SELECT * FROM paths");
      rows = results.Select();
      Assert.That(rows, Is.Empty);
    }

    #region Helpers
    private static void RemoveDb(string filename = dbFileName)
    {
      var dbFileInfo = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, filename));
      if (dbFileInfo.Exists)
      {
        dbFileInfo.Delete();
      }
    }
    #endregion
  }
}
