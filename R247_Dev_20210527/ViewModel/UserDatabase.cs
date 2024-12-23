using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Data.Sqlite;

namespace NOVisionDesigner.ViewModel
{
    public class UserDatabase
    {
        SqliteConnection DataConnection;
        SqliteCommand DataCommand;
        SqliteDataReader DataRead;
        public void SetConnection()
        {
            DataConnection = new SqliteConnection("Data Source=UserDatabase.sqlite;");
            DataConnection.Open();
            string sql = @"CREATE TABLE IF NOT EXISTS Users(
                               ID INTEGER PRIMARY KEY AUTOINCREMENT ,
                               UserName          TEXT    NOT NULL,
                               Password            TEXT       NOT NULL,
                               Role            TEXT       NOT NULL 
                            );";
            DataCommand = new SqliteCommand(sql, DataConnection);
            DataCommand.ExecuteNonQuery();
            DataConnection.Close();
        }

        public void AddData(string username, string password, string role)
        {
            DataCommand = new SqliteCommand();
            SetConnection();
            DataConnection.Open();
            DataCommand.Connection = DataConnection;
            DataCommand.CommandText = "insert into Users(UserName, Password, Role) values ('" + username + "','" + password + "','" + role + "')";
            DataCommand.ExecuteNonQuery();
            DataConnection.Close();
        }

        public void RemoveData(int id)
        {
            DataCommand = new SqliteCommand();
            SetConnection();
            DataConnection.Open();
            DataCommand.Connection = DataConnection;
            DataCommand.CommandText = "delete from Users where ID = '" + id.ToString() + "'";
            DataCommand.ExecuteNonQuery();
            DataConnection.Close();
        }
        public void UpdateData(int id, string username, string password, string role)
        {
            DataCommand = new SqliteCommand();
            SetConnection();
            DataConnection.Open();
            DataCommand.Connection = DataConnection;
            DataCommand.CommandText = "Update Users Set UserName = @username, Password = @password, Role = @role where ID = '" + id.ToString() + "'";
            DataCommand.Parameters.Add(new SqliteParameter("@username", username));
            DataCommand.Parameters.Add(new SqliteParameter("@password", password));
            DataCommand.Parameters.Add(new SqliteParameter("@role", role));
            DataCommand.ExecuteNonQuery();
            DataConnection.Close();
        }
        public List<UserModel> GetAllData()
        {
            List<UserModel> data = new List<UserModel>();
            SetConnection();
            DataCommand = new SqliteCommand("Select *From Users", DataConnection);
            DataConnection.Open();
            DataRead = DataCommand.ExecuteReader();
            while (DataRead.Read())
            {

                data.Add(new UserModel() { Id =int.Parse(DataRead[0].ToString()), UserName = DataRead[1].ToString(), Password = DataRead[2].ToString(), Role = DataRead[3].ToString() });

            }
            DataConnection.Close();
            return data;

        }
    }
}
