using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using CsvHelper;
using System.IO;
using CorrelationStation.Models;

namespace CorrelationStation.Dal
{
    public class Blobs
    {

        public void SaveBlobs(CsvParser parser, MapAndBlobVM mapAndBlob)
        {
            //string guid = Guid.NewGuid().ToString();



            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = "Server=JESS_COMPUTER\\SQLEXPRESS;Database=Scatter;Trusted_Connection=true";
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT TOP 1 [Tape] FROM dbo.Blobs ORDER BY Tape DESC", conn);

                int lastVal = Int32.Parse(command.ExecuteScalar().ToString());

                int updatedTapeVal = lastVal + 1;

                mapAndBlob.TapeId = updatedTapeVal;

                while (true)
                {
                    var row = parser.Read();

                    if (row == null)
                    {
                        break;
                    }

                    var rowJoined = string.Join(",", row);

                    SqlCommand insertCommand = new SqlCommand("INSERT INTO dbo.Blobs (Blob, Tape) VALUES (@row, @tape)", conn);

                    insertCommand.Parameters.Add(new SqlParameter("row", rowJoined));
                    insertCommand.Parameters.Add(new SqlParameter("tape", updatedTapeVal));
                    insertCommand.ExecuteNonQuery();
                    insertCommand.Dispose();

                }


            }

        }


        public void SaveMap(MapAndBlobVM mapAndBlob, string map)
        {
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = "Server=JESS_COMPUTER\\SQLEXPRESS;Database=Scatter;Trusted_Connection=true";
                conn.Open();

                SqlCommand insertCommand = new SqlCommand("INSERT INTO dbo.TapeToMap (TapeId, Map) VALUES (@tapeId, @map)", conn);


                insertCommand.Parameters.Add(new SqlParameter("tapeId", mapAndBlob.TapeId));
                insertCommand.Parameters.Add(new SqlParameter("map", map));
                insertCommand.ExecuteNonQuery();
                insertCommand.Dispose();
            }

        }


        public string GetMap(int id)
        {
            string test1 = "";

            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = "Server=JESS_COMPUTER\\SQLEXPRESS;Database=Scatter;Trusted_Connection=true";
                conn.Open();
                SqlCommand command = new SqlCommand("SELECT Map FROM dbo.TapeToMap WHERE TapeId = @id", conn);

                command.Parameters.Add(new SqlParameter("id", id));

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //row = reader.GetString(0);
                        test1 = (reader[0]).ToString();



                    }




                }
            }

            //return row.ToString();
            return test1;
        }


        public List<string> GetBlobs(int id)
        {

            List<string> rows = new List<string>();

            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = "Server=JESS_COMPUTER\\SQLEXPRESS;Database=Scatter;Trusted_Connection=true";
                conn.Open();
                SqlCommand command = new SqlCommand("SELECT Blob FROM dbo.Blobs WHERE Tape = @id", conn);

                command.Parameters.Add(new SqlParameter("id", id));

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(reader.GetString(0));
                    }

                }
            }

            return rows;

        }


    }
}