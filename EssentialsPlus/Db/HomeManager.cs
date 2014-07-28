﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace EssentialsPlus.Db
{
	public class HomeManager
	{
		private IDbConnection db;
		private object dbLock = new object(); 
		private List<Home> homes = new List<Home>();

		public HomeManager(IDbConnection db)
		{
			this.db = db;

			var sqlCreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlCreator.EnsureExists(new SqlTable("Homes",
				new SqlColumn("ID", MySqlDbType.Int32) { AutoIncrement = true, Primary = true },
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("Name", MySqlDbType.Text),
				new SqlColumn("X", MySqlDbType.Double),
				new SqlColumn("Y", MySqlDbType.Double),
				new SqlColumn("WorldID", MySqlDbType.Int32)));

			using (QueryResult result = db.QueryReader("SELECT * FROM Homes WHERE WorldID = @0", Main.worldID))
			{
				while (result.Read())
					homes.Add(new Home(result.Get<int>("UserID"), result.Get<string>("Name"), result.Get<float>("X"), result.Get<float>("Y")));
			}
		}

		public async Task<bool> AddAsync(TSPlayer player, string name, float x, float y)
		{
			try
			{
				homes.Add(new Home(player.UserID, name, x, y));
				return await Task.Run(() =>
				{
					lock (dbLock)
						return db.Query("INSERT INTO Homes (UserID, Name, X, Y, WorldID) VALUES (@0, @1, @2, @3, @4)", player.UserID, name, x, y, Main.worldID) > 0;
				});
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return false;
			}
		}
		public async Task<bool> DeleteAsync(TSPlayer player, string name)
		{
			try
			{
				homes.RemoveAll(h => h.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && h.UserID == player.UserID);

				string query = db.GetSqlType() == SqlType.Mysql ?
					"DELETE FROM Homes WHERE UserID = @0 AND Name = @1 AND WorldID = @2" :
					"DELETE FROM Homes WHERE UserID = @0 AND Name = @1 AND WorldID = @2 COLLATE NOCASE";

				return await Task.Run(() =>
				{
					lock (dbLock)
						return db.Query(query, player.UserID, name, Main.worldID) > 0;
				});
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return false;
			}
		}
		public Home Get(TSPlayer player, string name)
		{
			return homes.FirstOrDefault(h => h.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && h.UserID == player.UserID);
		}
		public List<Home> GetAll(TSPlayer player)
		{
			return homes.Where(h => h.UserID == player.UserID).ToList();
		}
		public async Task<bool> ReloadAsync()
		{
			homes.Clear();
			try
			{
				return await Task.Run(() =>
				{
					lock (dbLock)
					{
						using (QueryResult result = db.QueryReader("SELECT * FROM Homes"))
						{
							while (result.Read())
								homes.Add(new Home(result.Get<int>("UserID"), result.Get<string>("Name"), result.Get<float>("X"), result.Get<float>("Y")));
						}
						return true;
					}
				});
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return false;
			}
		}
		public async Task<bool> UpdateAsync(TSPlayer player, string name, float x, float y)
		{
			try
			{
				homes.RemoveAll(h => h.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && h.UserID == player.UserID);

				string query = db.GetSqlType() == SqlType.Mysql ?
					"UPDATE Homes SET X = @0, Y = @1 WHERE UserID = @2 AND Name = @3 AND WorldID = @4" :
					"UPDATE Homes SET X = @0, Y = @1 WHERE UserID = @2 AND Name = @3 AND WorldID = @4 COLLATE NOCASE";

				return await Task.Run(() =>
				{
					lock (dbLock)
						return db.Query(query, x, y, player.UserID, name, Main.worldID) > 0;
				});
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return false;
			}
		}
	}
}