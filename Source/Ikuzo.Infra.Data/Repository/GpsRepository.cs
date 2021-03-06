﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Ikuzo.Domain.Entities;
using Ikuzo.Domain.Histories;
using Ikuzo.Domain.Interfaces.Repositories;

namespace Ikuzo.Infra.Data.Repository
{
    public class GpsRepository : BaseRepository<Gps>, IGpsRepository
    {
        public GpsRepository(Context.Context context) : base(context)
        {
        }

        public Gps GetModalGps(string ModalId)
        {
            var gps = DbSet.FirstOrDefault(i => string.Equals(i.ModalId.ToLower(), ModalId.ToLower()));

            return gps;
        }

        public void RemoveAll()
        {
            //Send to history table
            var history = ParseToHistory();
            GpsHistoryBulkInsert(history);

            //Delete from main table
            var sql = @"
                      SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
                      DELETE  
                      FROM [Gps]";

            using (var cn = Connection)
            {
                if (cn.State.Equals(ConnectionState.Open) == false) cn.Open();

                var trans = cn.BeginTransaction("removeAllTransaction");

                var comm = new SqlCommand(sql, cn, trans);

                comm.ExecuteNonQuery();

                trans.Commit();

                if (cn.State.Equals(ConnectionState.Open)) cn.Close();
            }

            if (Connection.State.Equals(ConnectionState.Open)) Connection.Close();
        }

        public void RemoveFromLine(string lineId)
        {
            //Send to history table
            var history = ParseToHistory(lineId);
            GpsHistoryBulkInsert(history);

            //Delete from main table
            var sql = @"
                      SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; 
                      DELETE 
                      FROM [Gps] 
                      WHERE [LineId] = '" + lineId + "'";

            using (var cn = Connection)
            {
                if (cn.State.Equals(ConnectionState.Open) == false) cn.Open();

                var trans = cn.BeginTransaction("removeFromLineTransaction");

                var comm = new SqlCommand(sql, cn, trans);

                comm.ExecuteNonQuery();

                trans.Commit();

                if (cn.State.Equals(ConnectionState.Open)) cn.Close();
            }
        }

        public IEnumerable<Gps> GetNerbyModalsGps(decimal latitude, decimal longitude, decimal distance)
        {
            var itens = new List<Gps>();
             
            var sql = $@"
                    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
                    SELECT * 
                    FROM [Gps] 
                    WHERE  dbo.FnGetDistance(@lat, @lon, [Latitude], [Longitude],'Meters') <= @distance  ";

            using (var cn = Connection)
            {
                var args = new DynamicParameters();
                args.Add("lat", latitude, DbType.Decimal, precision: 12, scale: 6);
                args.Add("lon", longitude, DbType.Decimal, precision: 12, scale: 6);
                args.Add("distance", distance, DbType.Decimal, precision: 12, scale: 6); 

                if (cn.State.Equals(ConnectionState.Open) == false) cn.Open();

                itens.AddRange(Connection.Query<Gps>(sql, args, commandTimeout: 6000));

                if (cn.State.Equals(ConnectionState.Open)) cn.Close();
            }

            return itens;
        }

        public IEnumerable<Gps> GetNerbyModalsGpsFromLine(string lineId)
        {
            var itens = new List<Gps>();

            var sql = $@"
                    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
                    SELECT * 
                    FROM [Gps] 
                    WHERE [LineId] = '" + lineId + "'"; 

            using (var cn = Connection)
            {
                if (cn.State.Equals(ConnectionState.Open) == false) cn.Open();

                itens.AddRange(Connection.Query<Gps>(sql, commandTimeout: 6000));

                if (cn.State.Equals(ConnectionState.Open)) cn.Close();
            }

            return itens;
        }


        public void GpsBulkInsert(IEnumerable<Gps> gpses)
        {
            var dt = MakeTable(gpses);

            using (var cn = Connection)
            {
                if (cn.State.Equals(ConnectionState.Open) == false) cn.Open();

                using (var s = new SqlBulkCopy(cn))
                {
                    s.DestinationTableName = dt.TableName;

                    foreach (var column in dt.Columns)
                    {
                        s.ColumnMappings.Add(column.ToString(), column.ToString());
                    }

                    s.WriteToServer(dt);
                }

                if (cn.State.Equals(ConnectionState.Open)) cn.Close();
            }
        }

        private List<GpsHistory> ParseToHistory(string lineId = null)
        {
            var itens = new List<GpsHistory>();

            var sql = @"
                    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
                    SELECT ModalId, LineId, Latitude, Longitude, Direction, [TimeStamp], LastUpdateDate   
                    FROM [Gps] ";

            if (lineId != null)
            {
                sql += "WHERE [LineId] = '" + lineId + "'";
            }

            using (var cn = Connection)
            {
                if (cn.State.Equals(ConnectionState.Open) == false) cn.Open();

                itens.AddRange(Connection.Query<GpsHistory>(sql, commandTimeout: 6000));

                if (cn.State.Equals(ConnectionState.Open)) cn.Close();
            }

            return itens;
        }

        private void GpsHistoryBulkInsert(IEnumerable<GpsHistory> gpses)
        {
            var dt = MakeHistoryTable(gpses);

            using (var cn = Connection)
            {
                if (cn.State.Equals(ConnectionState.Open) == false) cn.Open();

                using (var s = new SqlBulkCopy(cn))
                {
                    s.DestinationTableName = dt.TableName;

                    foreach (var column in dt.Columns)
                    {
                        s.ColumnMappings.Add(column.ToString(), column.ToString());
                    }

                    s.WriteToServer(dt);
                }

                if (cn.State.Equals(ConnectionState.Open)) cn.Close();
            }
        }

        private DataTable MakeTable(IEnumerable<Gps> gpses)
        {
            var dtTable = new DataTable("Gps");

            var column1 = new DataColumn
            {
                DataType = Type.GetType("System.Guid"),
                ColumnName = "GpsGuid"
            };

            var column2 = new DataColumn
            {
                DataType = Type.GetType("System.String"),
                ColumnName = "ModalId"
            };

            var column3 = new DataColumn
            {
                DataType = Type.GetType("System.String"),
                ColumnName = "LineId"
            };

            var column4 = new DataColumn
            {
                DataType = Type.GetType("System.Decimal"),
                ColumnName = "Latitude"
            };

            var column5 = new DataColumn
            {
                DataType = Type.GetType("System.Decimal"),
                ColumnName = "Longitude"
            };

            var column6 = new DataColumn
            {
                DataType = Type.GetType("System.Int32"),
                ColumnName = "Direction"
            };

            var column7 = new DataColumn
            {
                DataType = Type.GetType("System.DateTime"),
                ColumnName = "TimeStamp"
            };

            var column8 = new DataColumn
            {
                DataType = Type.GetType("System.DateTime"),
                ColumnName = "LastUpdateDate"
            };

            dtTable.Columns.Add(column1);
            dtTable.Columns.Add(column2);
            dtTable.Columns.Add(column3);
            dtTable.Columns.Add(column4);
            dtTable.Columns.Add(column5);
            dtTable.Columns.Add(column6);
            dtTable.Columns.Add(column7);
            dtTable.Columns.Add(column8);

            //Adding rows
            foreach (var gps in gpses)
            {
                var dr = dtTable.NewRow();
                dr["GpsGuid"] = gps.GpsGuid;
                dr["ModalId"] = gps.ModalId;
                dr["LineId"] = gps.LineId;
                dr["Latitude"] = gps.Latitude;
                dr["Longitude"] = gps.Longitude;
                dr["Direction"] = gps.Direction;
                dr["TimeStamp"] = gps.Timestamp.ToLocalTime();
                dr["LastUpdateDate"] = gps.LastUpdateDate;

                dtTable.Rows.Add(dr);
            }

            return dtTable;
        }

        private DataTable MakeHistoryTable(IEnumerable<GpsHistory> gpses)
        {
            var dtTable = new DataTable("GpsHistory");

            var column1 = new DataColumn
            {
                DataType = Type.GetType("System.Int64"),
                ColumnName = "GpsHistoryId"
            };

            var column2 = new DataColumn
            {
                DataType = Type.GetType("System.String"),
                ColumnName = "ModalId"
            };

            var column3 = new DataColumn
            {
                DataType = Type.GetType("System.String"),
                ColumnName = "LineId"
            };

            var column4 = new DataColumn
            {
                DataType = Type.GetType("System.Decimal"),
                ColumnName = "Latitude"
            };

            var column5 = new DataColumn
            {
                DataType = Type.GetType("System.Decimal"),
                ColumnName = "Longitude"
            };

            var column6 = new DataColumn
            {
                DataType = Type.GetType("System.Int32"),
                ColumnName = "Direction"
            };

            var column7 = new DataColumn
            {
                DataType = Type.GetType("System.DateTime"),
                ColumnName = "TimeStamp"
            };

            var column8 = new DataColumn
            {
                DataType = Type.GetType("System.DateTime"),
                ColumnName = "LastUpdateDate"
            };

            dtTable.Columns.Add(column1);
            dtTable.Columns.Add(column2);
            dtTable.Columns.Add(column3);
            dtTable.Columns.Add(column4);
            dtTable.Columns.Add(column5);
            dtTable.Columns.Add(column6);
            dtTable.Columns.Add(column7);
            dtTable.Columns.Add(column8);

            //Adding rows
            foreach (var gps in gpses)
            {
                var dr = dtTable.NewRow();
                dr["GpsHistoryId"] = gps.GpsHistoryId;
                dr["ModalId"] = gps.ModalId;
                dr["LineId"] = gps.LineId;
                dr["Latitude"] = gps.Latitude;
                dr["Longitude"] = gps.Longitude;
                dr["Direction"] = gps.Direction;
                dr["TimeStamp"] = gps.Timestamp.ToLocalTime();
                dr["LastUpdateDate"] = gps.LastUpdateDate;

                dtTable.Rows.Add(dr);
            }

            return dtTable;
        }
    }
}
