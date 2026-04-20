using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace ScreenshotProcessApp
{
    public class ProcessFlow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int StartPageId { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class ProcessPage
    {
        public int Id { get; set; }
        public int FlowId { get; set; }
        public string Name { get; set; }
        public byte[] ImageData { get; set; }
        public string ImageName { get; set; }
        public string Remark { get; set; }
    }

    public class PageRegion
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Remark { get; set; }
        public int? TargetPageId { get; set; }
    }

    public class Database
    {
        private string _dbPath;

        public Database(string dbPath)
        {
            _dbPath = dbPath;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            bool needCreate = !File.Exists(_dbPath);
            
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                
                if (needCreate)
                {
                    string createFlowTable = @"
                        CREATE TABLE ProcessFlow (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Description TEXT,
                            StartPageId INTEGER,
                            CreateTime TEXT NOT NULL
                        )";
                    ExecuteNonQuery(conn, createFlowTable);

                    string createPageTable = @"
                        CREATE TABLE ProcessPage (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            FlowId INTEGER NOT NULL,
                            Name TEXT NOT NULL,
                            ImageData BLOB NOT NULL,
                            ImageName TEXT,
                            Remark TEXT,
                            FOREIGN KEY (FlowId) REFERENCES ProcessFlow(Id)
                        )";
                    ExecuteNonQuery(conn, createPageTable);

                    string createRegionTable = @"
                        CREATE TABLE PageRegion (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            PageId INTEGER NOT NULL,
                            X INTEGER NOT NULL,
                            Y INTEGER NOT NULL,
                            Width INTEGER NOT NULL,
                            Height INTEGER NOT NULL,
                            Remark TEXT,
                            TargetPageId INTEGER,
                            FOREIGN KEY (PageId) REFERENCES ProcessPage(Id),
                            FOREIGN KEY (TargetPageId) REFERENCES ProcessPage(Id)
                        )";
                    ExecuteNonQuery(conn, createRegionTable);
                }
            }
        }

        private void ExecuteNonQuery(SQLiteConnection conn, string sql, params SQLiteParameter[] parameters)
        {
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        public int AddFlow(ProcessFlow flow)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "INSERT INTO ProcessFlow (Name, Description, StartPageId, CreateTime) VALUES (@Name, @Description, @StartPageId, @CreateTime)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", flow.Name);
                    cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(flow.Description) ? (object)DBNull.Value : flow.Description);
                    cmd.Parameters.AddWithValue("@StartPageId", flow.StartPageId);
                    cmd.Parameters.AddWithValue("@CreateTime", flow.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                    return (int)conn.LastInsertRowId;
                }
            }
        }

        public void UpdateFlow(ProcessFlow flow)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "UPDATE ProcessFlow SET Name=@Name, Description=@Description, StartPageId=@StartPageId WHERE Id=@Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", flow.Id);
                    cmd.Parameters.AddWithValue("@Name", flow.Name);
                    cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(flow.Description) ? (object)DBNull.Value : flow.Description);
                    cmd.Parameters.AddWithValue("@StartPageId", flow.StartPageId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteFlow(int flowId)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    ExecuteNonQuery(conn, "DELETE FROM PageRegion WHERE PageId IN (SELECT Id FROM ProcessPage WHERE FlowId=@FlowId)", 
                        new SQLiteParameter("@FlowId", flowId));
                    ExecuteNonQuery(conn, "DELETE FROM ProcessPage WHERE FlowId=@FlowId", 
                        new SQLiteParameter("@FlowId", flowId));
                    ExecuteNonQuery(conn, "DELETE FROM ProcessFlow WHERE Id=@FlowId", 
                        new SQLiteParameter("@FlowId", flowId));
                    transaction.Commit();
                }
            }
        }

        public List<ProcessFlow> GetAllFlows()
        {
            List<ProcessFlow> flows = new List<ProcessFlow>();
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "SELECT Id, Name, Description, StartPageId, CreateTime FROM ProcessFlow ORDER BY CreateTime DESC";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        flows.Add(new ProcessFlow
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            StartPageId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                            CreateTime = DateTime.Parse(reader.GetString(4))
                        });
                    }
                }
            }
            return flows;
        }

        public ProcessFlow GetFlowById(int id)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "SELECT Id, Name, Description, StartPageId, CreateTime FROM ProcessFlow WHERE Id=@Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ProcessFlow
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                StartPageId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                CreateTime = DateTime.Parse(reader.GetString(4))
                            };
                        }
                    }
                }
            }
            return null;
        }

        public int AddPage(ProcessPage page)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "INSERT INTO ProcessPage (FlowId, Name, ImageData, ImageName, Remark) VALUES (@FlowId, @Name, @ImageData, @ImageName, @Remark)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FlowId", page.FlowId);
                    cmd.Parameters.AddWithValue("@Name", page.Name);
                    cmd.Parameters.AddWithValue("@ImageData", page.ImageData);
                    cmd.Parameters.AddWithValue("@ImageName", string.IsNullOrEmpty(page.ImageName) ? (object)DBNull.Value : page.ImageName);
                    cmd.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(page.Remark) ? (object)DBNull.Value : page.Remark);
                    cmd.ExecuteNonQuery();
                    return (int)conn.LastInsertRowId;
                }
            }
        }

        public void UpdatePage(ProcessPage page)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "UPDATE ProcessPage SET Name=@Name, ImageData=@ImageData, ImageName=@ImageName, Remark=@Remark WHERE Id=@Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", page.Id);
                    cmd.Parameters.AddWithValue("@Name", page.Name);
                    cmd.Parameters.AddWithValue("@ImageData", page.ImageData);
                    cmd.Parameters.AddWithValue("@ImageName", string.IsNullOrEmpty(page.ImageName) ? (object)DBNull.Value : page.ImageName);
                    cmd.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(page.Remark) ? (object)DBNull.Value : page.Remark);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeletePage(int pageId)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    ExecuteNonQuery(conn, "DELETE FROM PageRegion WHERE PageId=@PageId", 
                        new SQLiteParameter("@PageId", pageId));
                    ExecuteNonQuery(conn, "DELETE FROM ProcessPage WHERE Id=@PageId", 
                        new SQLiteParameter("@PageId", pageId));
                    transaction.Commit();
                }
            }
        }

        public List<ProcessPage> GetPagesByFlowId(int flowId)
        {
            List<ProcessPage> pages = new List<ProcessPage>();
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "SELECT Id, FlowId, Name, ImageData, ImageName, Remark FROM ProcessPage WHERE FlowId=@FlowId ORDER BY Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FlowId", flowId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pages.Add(new ProcessPage
                            {
                                Id = reader.GetInt32(0),
                                FlowId = reader.GetInt32(1),
                                Name = reader.GetString(2),
                                ImageData = (byte[])reader.GetValue(3),
                                ImageName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Remark = reader.IsDBNull(5) ? null : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return pages;
        }

        public ProcessPage GetPageById(int id)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "SELECT Id, FlowId, Name, ImageData, ImageName, Remark FROM ProcessPage WHERE Id=@Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ProcessPage
                            {
                                Id = reader.GetInt32(0),
                                FlowId = reader.GetInt32(1),
                                Name = reader.GetString(2),
                                ImageData = (byte[])reader.GetValue(3),
                                ImageName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Remark = reader.IsDBNull(5) ? null : reader.GetString(5)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public int AddRegion(PageRegion region)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "INSERT INTO PageRegion (PageId, X, Y, Width, Height, Remark, TargetPageId) VALUES (@PageId, @X, @Y, @Width, @Height, @Remark, @TargetPageId)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@PageId", region.PageId);
                    cmd.Parameters.AddWithValue("@X", region.X);
                    cmd.Parameters.AddWithValue("@Y", region.Y);
                    cmd.Parameters.AddWithValue("@Width", region.Width);
                    cmd.Parameters.AddWithValue("@Height", region.Height);
                    cmd.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(region.Remark) ? (object)DBNull.Value : region.Remark);
                    cmd.Parameters.AddWithValue("@TargetPageId", region.TargetPageId.HasValue ? region.TargetPageId.Value : (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                    return (int)conn.LastInsertRowId;
                }
            }
        }

        public void UpdateRegion(PageRegion region)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "UPDATE PageRegion SET X=@X, Y=@Y, Width=@Width, Height=@Height, Remark=@Remark, TargetPageId=@TargetPageId WHERE Id=@Id";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", region.Id);
                    cmd.Parameters.AddWithValue("@X", region.X);
                    cmd.Parameters.AddWithValue("@Y", region.Y);
                    cmd.Parameters.AddWithValue("@Width", region.Width);
                    cmd.Parameters.AddWithValue("@Height", region.Height);
                    cmd.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(region.Remark) ? (object)DBNull.Value : region.Remark);
                    cmd.Parameters.AddWithValue("@TargetPageId", region.TargetPageId.HasValue ? region.TargetPageId.Value : (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteRegion(int regionId)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                ExecuteNonQuery(conn, "DELETE FROM PageRegion WHERE Id=@Id", 
                    new SQLiteParameter("@Id", regionId));
            }
        }

        public List<PageRegion> GetRegionsByPageId(int pageId)
        {
            List<PageRegion> regions = new List<PageRegion>();
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "SELECT Id, PageId, X, Y, Width, Height, Remark, TargetPageId FROM PageRegion WHERE PageId=@PageId";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@PageId", pageId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            regions.Add(new PageRegion
                            {
                                Id = reader.GetInt32(0),
                                PageId = reader.GetInt32(1),
                                X = reader.GetInt32(2),
                                Y = reader.GetInt32(3),
                                Width = reader.GetInt32(4),
                                Height = reader.GetInt32(5),
                                Remark = reader.IsDBNull(6) ? null : reader.GetString(6),
                                TargetPageId = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7)
                            });
                        }
                    }
                }
            }
            return regions;
        }
    }
}