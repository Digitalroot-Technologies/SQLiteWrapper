using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace SQLiteWrapperDotNetFive
{
  /// <summary>
  /// This class makes reading and writing to a SQLite database easy.
  /// 
  /// SQLite Wrapper
  ///
  ///  Author: Nicholas Dunnaway
  ///  Copyright: digitalroot.net 2008 - 2021
  ///  Web: http://digitalroot.net
  ///
  /// This program was written to make reading and writing to a SQLite database easy.
  ///
  /// Requirements:
  ///  .Net Runtime Files
  ///  System.Data.SQLite.dll
  /// </summary>
  public class SQLiteWrapper : IDisposable
  {
    /// <summary>
    /// Path used to locate the database file.
    /// </summary>
    private string _pathToDatabase;

    /// <summary>
    /// Name of the database file.
    /// </summary>
    private readonly string _databaseFileName;

    /// <summary>
    /// This is the connection to the database. This holds it open 
    /// so we do not have to reconnect each time we want to run a query. 
    /// </summary>
    private SQLiteConnection _sqLiteConnection;

    /// <summary>
    /// Public method for getting and settings the Path to the database file.
    /// </summary>
    public string PathToDatabase
    {
      get => _pathToDatabase;
      set
      {
        if (Directory.Exists(value))
        {
          char[] charsToTrim = { '\\', '/' }; // Remove trailing slash.
          _pathToDatabase = value.TrimEnd(charsToTrim);
        }
        else
        {
          throw new FileNotFoundException("Path for database is invalid.", _pathToDatabase);
        }
      }
    }

    /// <summary>
    /// Constructor 
    /// </summary>
    /// <param name="databaseName">Database File Name</param>
    /// <param name="pathToDatabase">Path to the database file.</param>
    public SQLiteWrapper(string databaseName, string pathToDatabase = ".")
    {
      _pathToDatabase = pathToDatabase; // Set the path
      _databaseFileName = databaseName; // Set File Name
      if (File.Exists(Path.Combine(_pathToDatabase, _databaseFileName)) == false)
      {
        CreateSettingsTable();
      }
      else
      {
        _sqLiteConnection = new SQLiteConnection($"Data Source={Path.Combine(_pathToDatabase, _databaseFileName)}");
        _sqLiteConnection.Open(); // Open the database connection. 
      }
    }

    /// <summary>
    /// This creates the database for the first time. It creates a by default a settings table.
    /// </summary>
    private void CreateSettingsTable()
    {
      _sqLiteConnection = new SQLiteConnection($"Data Source={Path.Combine(_pathToDatabase, _databaseFileName)}");
      using var command = _sqLiteConnection.CreateCommand();
      _sqLiteConnection.Open(); // Open the database connection. 

      // Create a new table
      command.CommandText = "CREATE TABLE settings (id INTEGER PRIMARY KEY AUTOINCREMENT, name VARCHAR(150) UNIQUE, value VARCHAR(250))";
      command.ExecuteNonQuery(); // Create the table, don't expect returned data

      // Insert something into the table
      command.CommandText = "INSERT INTO settings (name, value) VALUES ('dbInstalled', 'TRUE')";
      command.ExecuteNonQuery();

      // Read the values back out
      command.CommandText = "SELECT value FROM settings WHERE name = 'dbInstalled'";
      using var reader = command.ExecuteReader();
      while (reader.Read())
      {
        //Console.WriteLine(String.Format("id = {0}, ServerName = {1}", reader[0], reader[1]));
        if (reader[0].ToString() == "TRUE") continue;
        // Database was not setup correctly. We should prob delete the database file.
        File.Delete(Path.Combine(_pathToDatabase, _databaseFileName));
        throw new Exception("Error creating database");
      }
    }

    /// <summary>
    /// This queries the database and returns the values of a setting.
    /// </summary>
    /// <param name="name">Name of the settings you would like the value for.</param>
    public string Get(string name)
    {
      using var command = _sqLiteConnection.CreateCommand();
      // check if the connection is already open. If not Open it. 
      if (_sqLiteConnection.State != ConnectionState.Open)
      {
        _sqLiteConnection.Open();
      }

      // Read the values back out
      command.CommandText = $"SELECT value FROM settings WHERE name = '{name}'";
      using var reader = command.ExecuteReader();
      while (reader.Read())
      {
        return reader[0].ToString();
      }

      return string.Empty;
    }

    /// <summary>
    /// This is for passing raw commands to the database.
    /// </summary>
    /// <param name="sqlCommand">SQL Query</param>
    /// <returns>True or throws an Exception</returns>
    public bool RawCmd(string sqlCommand)
    {
      using var command = _sqLiteConnection.CreateCommand();
      // check if the connection is already open. If not Open it. 
      if (_sqLiteConnection.State != ConnectionState.Open)
      {
        _sqLiteConnection.Open();
      }

      // Run command from user
      command.CommandText = sqlCommand;
      command.ExecuteNonQuery();

      return true;
    }

    /// <summary>
    /// This is for passing raw select statements to the database.
    /// </summary>
    /// <param name="sqlCommand">SQL Query</param>
    /// <returns>True or throws an Exception</returns>
    public DataTable Select(string sqlCommand)
    {
      using var command = _sqLiteConnection.CreateCommand();
      // check if the connection is already open. If not Open it. 
      if (_sqLiteConnection.State != ConnectionState.Open)
      {
        _sqLiteConnection.Open();
      }

      // Read the values back out
      var dataTable = new DataTable();

      using var sqLiteDataAdapter = new SQLiteDataAdapter(sqlCommand, _sqLiteConnection);
      using (new SQLiteCommandBuilder(sqLiteDataAdapter))
      {
        sqLiteDataAdapter.Fill(dataTable);
      }

      return dataTable;
    }

    /// <summary>
    /// Save the value of something to the database so we can get it back later.
    /// </summary>
    /// <param name="name">Name of the settings you would like to set.</param>
    /// <param name="value">Value you of settings we are saving.</param>
    /// <returns>Returns True if the value was saved to the database.</returns>
    public bool Set(string name, string value)
    {
      using var command = _sqLiteConnection.CreateCommand();
      // check if the connection is already open. If not Open it. 
      if (_sqLiteConnection.State != ConnectionState.Open)
      {
        _sqLiteConnection.Open();
      }

      // Insert something into the table
      command.CommandText = $"DELETE FROM settings WHERE name = '{name}'";
      command.ExecuteNonQuery();

      // Insert something into the table
      command.CommandText = $"INSERT INTO settings (name, value) VALUES ('{name}', '{value}')";
      command.ExecuteNonQuery();

      return true;
    }

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
      _sqLiteConnection?.Dispose();
    }

    #endregion
  }
}
