using System;
using System.IO;
using System.Data;
using System.Data.SQLite;

namespace SQLiteWrapper
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
  public class SQLiteWrapper
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
    private static SQLiteConnection _cnn;

    /// <summary>
    /// This is what we use to issue commands to the database.
    /// </summary>
    private static SQLiteCommand _cmd;

    /// <summary>
    /// Public method for getting and settings the Path to the database file.
    /// </summary>
    public string PathToDatabase
    {
      get { return _pathToDatabase; }
      set
      {
        if (Directory.Exists(value))
        {
          char[] charsToTrim = { '\\', '/' }; // Remove trailing slash.
          _pathToDatabase = value.TrimEnd(charsToTrim);
        }
        else
        {
          throw new Exception("Path for database is invalid.");
        }
      }
    }

    /// <summary>
    /// This creates the database for the first time. It creates a by default a settings table.
    /// </summary>
    private void CreateSettingsTable()
    {
      using (_cnn = new SQLiteConnection("Data Source=" + _pathToDatabase + '\\' + _databaseFileName))
      {
        using (_cmd = _cnn.CreateCommand())
        {
          _cnn.Open(); // Open the database connection. 

          // Create a new table
          _cmd.CommandText = "CREATE TABLE settings (id INTEGER PRIMARY KEY AUTOINCREMENT, name VARCHAR(150) UNIQUE, value VARCHAR(250))";
          _cmd.ExecuteNonQuery(); // Create the table, don't expect returned data

          // Insert something into the table
          _cmd.CommandText = "INSERT INTO settings (name, value) VALUES ('dbInstalled', 'TRUE')";
          _cmd.ExecuteNonQuery();

          // Read the values back out
          _cmd.CommandText = "SELECT value FROM settings WHERE name = 'dbInstalled'";
          using (SQLiteDataReader reader = _cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              //Console.WriteLine(String.Format("id = {0}, ServerName = {1}", reader[0], reader[1]));
              if (reader[0].ToString() != "TRUE")
              {
                // Database was not setup correctly. We should prob delete the database file.
                File.Delete(_pathToDatabase + '\\' + _databaseFileName);
                throw new Exception("Error creating database");
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// This queries the database and returns the values of a setting.
    /// </summary>
    /// <param name="name">Name of the settings you would like the value for.</param>
    public string Get(string name)
    {
      using (_cmd = _cnn.CreateCommand())
      {
        // check if the connection is already open. If not Open it. 
        if (_cnn.State != ConnectionState.Open)
        {
          _cnn.Open();
        }

        // Read the values back out
        _cmd.CommandText = "SELECT value FROM settings WHERE name = '" + name + "'";
        using (SQLiteDataReader reader = _cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            return reader[0].ToString();
          }
        }
      }

      return "";
    }

    /// <summary>
    /// This is for passing raw commands to the database.
    /// </summary>
    /// <param name="sqlCommand">SQL Query</param>
    /// <returns>True or throws an Exception</returns>
    public bool RawCmd(string sqlCommand)
    {
      try
      {
        using (_cmd = _cnn.CreateCommand())
        {
          // check if the connection is already open. If not Open it. 
          if (_cnn.State != ConnectionState.Open)
          {
            _cnn.Open();
          }

          // Run command from user
          _cmd.CommandText = sqlCommand;
          _cmd.ExecuteNonQuery();
        }
      }
      catch (Exception e)
      {
        throw new Exception(e.Message);
      }

      return true;
    }

    /// <summary>
    /// This is for passing raw select statements to the database.
    /// </summary>
    /// <param name="sqlCommand">SQL Query</param>
    /// <returns>True or throws an Exception</returns>
    public DataTable Select(string sqlCommand)
    {
      using (_cmd = _cnn.CreateCommand())
      {
        // check if the connection is already open. If not Open it. 
        if (_cnn.State != ConnectionState.Open)
        {
          _cnn.Open();
        }

        // Read the values back out
        var myTable = new DataTable();

        using (var myDataAdp = new SQLiteDataAdapter(sqlCommand, _cnn))
        {
          using (new SQLiteCommandBuilder(myDataAdp))
          {
            myDataAdp.Fill(myTable);
          }
        }

        return myTable;
      }
    }

    /// <summary>
    /// Save the value of something to the database so we can get it back later.
    /// </summary>
    /// <param name="name">Name of the settings you would like to set.</param>
    /// <param name="value">Value you of settings we are saving.</param>
    /// <returns>Returns True if the value was saved to the database.</returns>
    public bool Set(string name, string value)
    {
      try
      {
        using (_cmd = _cnn.CreateCommand())
        {
          // check if the connection is already open. If not Open it. 
          if (_cnn.State != ConnectionState.Open)
          {
            _cnn.Open();
          }

          // Insert something into the table
          _cmd.CommandText = "DELETE FROM settings WHERE name = '" + name + "'";
          _cmd.ExecuteNonQuery();

          // Insert something into the table
          _cmd.CommandText = "INSERT INTO settings (name, value) VALUES ('" + name + "', '" + value + "')";
          _cmd.ExecuteNonQuery();
        }
      }
      catch (Exception e)
      {
        throw new Exception(e.Message);
      }

      return true;
    }

    /// <summary>
    /// Constructor 
    /// </summary>
    /// <param name="databaseName">Database File Name</param>
    /// <param name="pathToDatabase">Path to the database file.</param>
    public SQLiteWrapper(string databaseName, string pathToDatabase)
    {
      _pathToDatabase = pathToDatabase; // Set the path
      _databaseFileName = databaseName; // Set File Name
      if (File.Exists(_pathToDatabase + '\\' + _databaseFileName) == false)
      {
        CreateSettingsTable();
      }
      else
      {
        using (_cnn = new SQLiteConnection("Data Source=" + _pathToDatabase + '\\' + _databaseFileName))
        {
          using (_cmd = _cnn.CreateCommand())
          {
            _cnn.Open(); // Open the database connection. 
          }
        }
      }
    }

    /// <summary>
    /// Constructor 
    /// </summary>
    /// <param name="databaseName">Database File Name</param>
    public SQLiteWrapper(string databaseName)
      : this(databaseName, ".")
    {
    }
  }
}
