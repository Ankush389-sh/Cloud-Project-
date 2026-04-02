using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace ExpenseTrackerApp
{
    public class Expense
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }

    public class DatabaseManager
    {
        private string connectionString;

        public DatabaseManager(string dbPath = "expenses.db")
        {
            connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string tableCmd = @"
                    CREATE TABLE IF NOT EXISTS Expenses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        Amount REAL NOT NULL,
                        Description TEXT
                    );
                    CREATE TABLE IF NOT EXISTS Categories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT UNIQUE NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL
                    );";
                using (var cmd = new SQLiteCommand(tableCmd, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Initialize predefined categories
                string countCmd = "SELECT COUNT(*) FROM Categories";
                using (var cmd = new SQLiteCommand(countCmd, conn))
                {
                    long count = (long)cmd.ExecuteScalar();
                    if (count == 0)
                    {
                        var defaultCategories = new[] { "Food 🍔", "Travel 🚗", "Shopping 🛍", "Bills 💡", "Other 📦" };
                        foreach (var cat in defaultCategories)
                        {
                            string insertCat = "INSERT INTO Categories (Name) VALUES (@Name)";
                            using (var cCmd = new SQLiteCommand(insertCat, conn))
                            {
                                cCmd.Parameters.AddWithValue("@Name", cat);
                                cCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        public List<string> GetCategories()
        {
            var categories = new List<string>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string selectCmd = "SELECT Name FROM Categories ORDER BY Id";
                using (var cmd = new SQLiteCommand(selectCmd, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(reader["Name"].ToString());
                    }
                }
            }
            return categories;
        }

        public void AddCategory(string name)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string insertCmd = "INSERT OR IGNORE INTO Categories (Name) VALUES (@Name)";
                using (var cmd = new SQLiteCommand(insertCmd, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteCategory(string name)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string deleteCmd = "DELETE FROM Categories WHERE Name = @Name";
                using (var cmd = new SQLiteCommand(deleteCmd, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SaveSetting(string key, string value)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string cmdText = "INSERT INTO Settings (Key, Value) VALUES (@Key, @Value) ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value";
                using (var cmd = new SQLiteCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@Key", key);
                    cmd.Parameters.AddWithValue("@Value", value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string cmdText = "SELECT Value FROM Settings WHERE Key = @Key";
                using (var cmd = new SQLiteCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@Key", key);
                    var res = cmd.ExecuteScalar();
                    return res != null ? res.ToString() : defaultValue;
                }
            }
        }

        public void AddExpense(Expense expense)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string insertCmd = @"
                    INSERT INTO Expenses (Date, Category, Amount, Description) 
                    VALUES (@Date, @Category, @Amount, @Description)";
                using (var cmd = new SQLiteCommand(insertCmd, conn))
                {
                    cmd.Parameters.AddWithValue("@Date", expense.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Category", expense.Category);
                    cmd.Parameters.AddWithValue("@Amount", expense.Amount);
                    cmd.Parameters.AddWithValue("@Description", expense.Description);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateExpense(Expense expense)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string updateCmd = @"
                    UPDATE Expenses 
                    SET Date = @Date, Category = @Category, Amount = @Amount, Description = @Description 
                    WHERE Id = @Id";
                using (var cmd = new SQLiteCommand(updateCmd, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", expense.Id);
                    cmd.Parameters.AddWithValue("@Date", expense.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Category", expense.Category);
                    cmd.Parameters.AddWithValue("@Amount", expense.Amount);
                    cmd.Parameters.AddWithValue("@Description", expense.Description);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteExpense(int id)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string deleteCmd = "DELETE FROM Expenses WHERE Id = @Id";
                using (var cmd = new SQLiteCommand(deleteCmd, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Expense> GetAllExpenses()
        {
            var expenses = new List<Expense>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string selectCmd = "SELECT * FROM Expenses ORDER BY Date DESC";
                using (var cmd = new SQLiteCommand(selectCmd, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        expenses.Add(new Expense
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Date = DateTime.Parse(reader["Date"].ToString(), System.Globalization.CultureInfo.InvariantCulture),
                            Category = reader["Category"].ToString(),
                            Amount = Convert.ToDecimal(reader["Amount"]),
                            Description = reader["Description"].ToString()
                        });
                    }
                }
            }
            return expenses;
        }

        public List<Expense> GetExpensesByMonth(int year, int month)
        {
            var expenses = new List<Expense>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string selectCmd = @"
                    SELECT * FROM Expenses 
                    WHERE strftime('%Y', Date) = @Year AND strftime('%m', Date) = @Month 
                    ORDER BY Date DESC";
                using (var cmd = new SQLiteCommand(selectCmd, conn))
                {
                    cmd.Parameters.AddWithValue("@Year", year.ToString("D4"));
                    cmd.Parameters.AddWithValue("@Month", month.ToString("D2"));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            expenses.Add(new Expense
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Date = DateTime.Parse(reader["Date"].ToString(), System.Globalization.CultureInfo.InvariantCulture),
                                Category = reader["Category"].ToString(),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                Description = reader["Description"].ToString()
                            });
                        }
                    }
                }
            }
            return expenses;
        }
    }
}
