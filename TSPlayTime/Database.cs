using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.DB;

namespace TSPlayTime
{
    public class Database
    {

        public class DatabaseManager
        {
            private IDbConnection _db;

            public DatabaseManager(IDbConnection db)
            {
                _db = db;

                var sqlCreator = new SqlTableCreator(db, new SqliteQueryCreator());

                sqlCreator.EnsureTableStructure(new SqlTable("Players",
                    new SqlColumn("Name", MySqlDbType.String) { Primary = true, Unique = true },
                    new SqlColumn("Time", MySqlDbType.Int32)));
            }

            /// <exception cref="NullReferenceException"></exception>
            public int GetPlayerTime(string name)
            {
                using var reader = _db.QueryReader("SELECT * FROM Players WHERE Name = @0", name);
                while (reader.Read())
                {
                    return reader.Get<int>("Time");
                }
                throw new NullReferenceException();
            }

            public Dictionary<string, int> GetLeaderBoard(int maxpage)
            {
                using var reader = _db.QueryReader("SELECT * FROM Players ORDER BY Time DESC");
                /*
                 * SELECT column_list
                 * FROM table_name
                 * ORDER BY column1 ASC, column2 DESC;
                */

                Dictionary<string, int> result = new();

                int index = 0;

                maxpage--;
                if (maxpage < 0) { maxpage = 0; }

                while (reader.Read())
                {
                    if (index >= (maxpage*10)+10)
                    {
                        break;
                    }
                    result.Add(reader.Get<string>("Name"), reader.Get<int>("Time"));
                    index++;
                }
                return result;
                throw new NullReferenceException();
            }

            public bool InsertPlayer(string name)
            {
                return _db.Query("INSERT INTO Players (Name, Time) VALUES (@0, @1)", name, 0) != 0;
            }

            public bool SavePlayer(string name, int time)
            {
                return _db.Query("UPDATE Players SET Time = @0 WHERE Name = @1", time, name) != 0;
            }
        }
    }
}