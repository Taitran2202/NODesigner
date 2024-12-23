using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Data.Sqlite;

namespace NOVisionDesigner.ViewModel
{
    public class PrivilegeDatabase
    {
        SqliteConnection DataConnection;
        SqliteCommand DataCommand;
        SqliteDataReader DataRead;
        public void SetConnection()
        {
            string sql = @"CREATE TABLE IF NOT EXISTS PrivilegeDatabase(
                               ID INTEGER PRIMARY KEY AUTOINCREMENT ,
                               Role          TEXT    NOT NULL,
                               EditJob            TEXT       NOT NULL,
                               EditTool            TEXT       NOT NULL,
                               Privilege            TEXT NOT NULL,
                               UserManagement       TEXT  NOT NULL
                            );";
            DataConnection = new SqliteConnection("Data Source=PrivilegeDatabase.sqlite;");
            DataConnection.Open();
            DataCommand = new SqliteCommand(sql, DataConnection);
            DataCommand.ExecuteNonQuery();
            DataConnection.Close();
        }

        public void AddData(string role, bool EditJob, bool EditTool, bool Privilege, bool UserManagement )
        {
            DataCommand = new SqliteCommand();
            SetConnection();
            DataConnection.Open();
            DataCommand.Connection = DataConnection;
            DataCommand.CommandText = "insert into PrivilegeDatabase(Role, EditJob, EditTool, Privilege, UserManagement) values ('" + role + "'," +
                "'" + EditJob + "','" + EditTool + "','" + Privilege + "','" + UserManagement + "')";
            DataCommand.ExecuteNonQuery();
            DataConnection.Close();
        }

        public void UpdateData(string role, string column, string value)
        {
            DataCommand = new SqliteCommand();
            SetConnection();
            DataConnection.Open();
            DataCommand.Connection = DataConnection;
            DataCommand.CommandText = "Update PrivilegeDatabase Set " + column + " = @column where Role = '" + role + "'";
            DataCommand.Parameters.Add(new SqliteParameter("@column", value));
            
            DataCommand.ExecuteNonQuery();
            DataConnection.Close();
        }

        public void RemoveData(int id)
        {
            DataCommand = new SqliteCommand();
            SetConnection();
            DataConnection.Open();
            DataCommand.Connection = DataConnection;
            DataCommand.CommandText = "delete from PrivilegeDatabase where ID = '" + id.ToString() + "'";
            DataCommand.ExecuteNonQuery();
            DataConnection.Close();
        }
        public List<CurrentUserRole> GetAllData()
        {
            List<CurrentUserRole> data = new List<CurrentUserRole>();
            SetConnection();
            DataCommand = new SqliteCommand("Select *From PrivilegeDatabase", DataConnection);
            DataConnection.Open();
            DataRead = DataCommand.ExecuteReader();
            while (DataRead.Read())
            {

                data.Add(new CurrentUserRole() { Id = int.Parse(DataRead[0].ToString()),           
                    Role = DataRead[1].ToString(), 
                    EditJob = bool.Parse(DataRead[2].ToString()), 
                    EditTool = bool.Parse(DataRead[3].ToString()),
                    Privilege = bool.Parse(DataRead[4].ToString()), 
                    UserManagement =  bool.Parse(DataRead[5].ToString()) });

            }
            DataConnection.Close();
            return data;

        }
    }
}
