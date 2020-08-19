using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using Dapper;
using System.Linq;

namespace ParkingLibrary
{
    public class ParkingClass1
    {
        private readonly string connectionString;
        private readonly string mongoConnString;
        private SqlConnection conn;
        private SqlCommand Cmd;

        public ParkingClass1()
        {
            connectionString = ConfigurationManager.ConnectionStrings["SqlSeverDBConn"].ConnectionString;
            mongoConnString = ConfigurationManager.ConnectionStrings["MongoDBConn"].ConnectionString;
        }
        
        public List<ParkingData> Query(string search, DynamicParameters param = null)
        {
            List<ParkingData> results = new List<ParkingData>();
            using (conn = new SqlConnection(connectionString))
            {
                results = conn.Query<ParkingData>(search, param).ToList();
            }
            return results;
        }

        public void DapperExecute(string execute, DynamicParameters param = null)
        {
            using (conn = new SqlConnection(connectionString))
            {
                conn.Execute(execute, param);
            }
        }

        public SqlDataReader ReadData(string search, SqlParameter[] param = null) 
        {
            conn = new SqlConnection(connectionString);
            conn.Open();
            Cmd = new SqlCommand(search, conn);
            if (param != null)
            {
                Cmd.Parameters.AddRange(param);
            }
            SqlDataReader dr = Cmd.ExecuteReader();
            return dr;
        }

        public void Execute(string execute, SqlParameter[] param = null)
        {
            using (conn = new SqlConnection(connectionString))
            {
                conn.Open();
                Cmd = new SqlCommand(execute, conn);
                if (param != null)
                {
                    Cmd.Parameters.AddRange(param);
                }
                Cmd.ExecuteNonQuery();
            }
        }

        public void CloseDB() 
        {
            conn.Close();
        }

        public IMongoCollection<BsonDocument> MongodbConn(string database, string collection)
        {
            MongoClient mongoClient = new MongoClient(mongoConnString);
            var mongoDatabase = mongoClient.GetDatabase(database);
            var mongoCollection = mongoDatabase.GetCollection<BsonDocument>(collection);
            return mongoCollection;
        }

    }
}
