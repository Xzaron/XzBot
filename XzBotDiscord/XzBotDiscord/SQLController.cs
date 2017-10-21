using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SqlClient;


namespace XzBotDiscord
{
    public class SqlRow
    {
        public List<Tuple<string, string>> MainTuple { get; set; }

        public SqlRow(List<Tuple<string, string>> mainTuple)
        {
            MainTuple = mainTuple;
        }
        public SqlRow()
        {

        }

    }

    public class SQLController
    {
        Program mainProgram;

        private static string databaseName = null;
        private static string userId = null;
        private static string userPassword = null;

        private static string connectionString = "Server=Core\\SQLExpress;Database="+ databaseName + ";User ID = "+ userId + ";Password = "+ userPassword + ";";
        private int connectionCounter = 0;

        public SQLController(Program program)
        {
            mainProgram = program;
            string[] allLines = mainProgram.readWriteFile.ReturnAllLinesAsArray("c:\\Users\\Public\\Documents\\DiscordSQLConnection.txt");
            Dictionary<string, string> sqlDict = mainProgram.readWriteFile.CreateDictFromStringArray(allLines, '=');
            databaseName = sqlDict["Database"];
            userId = sqlDict["UserID"];
            userPassword = sqlDict["Password"];
        }

        public void InsertGo(string table_name, string fields, string values)
        {
            //public string SQLReplaceAPO(string incomingString)
            //public string SQLReverseAPO(string incomingString)
            if (!fields.Contains("is_obsolete"))
            {
                fields += ",is_obsolete";
                values += ",'N'";
            }

            string fullQuery = "INSERT INTO " + table_name + "(" + fields + ")" + " VALUES(" + values + ")";
            SqlGo(fullQuery, false);
        }

        public void UpdateGo(string table_name, string values, string where)
        {
            string fullQuery = "Update " + table_name + " SET " + values + " " + "where " + where;
            SqlGo(fullQuery, false);
        }

        public List<SqlRow> RetreiveGo(string how_many, string table_name, string where_statement, string calling_function)
        {
            string returns = "";

            string query = "Select " + how_many + " from " + table_name;

            if (where_statement.Length > 0)
            {
                query += " WHERE " + where_statement;
            }

            List<SqlRow> returnedList = SqlGo(query, true);

            return returnedList;
        }

        private List<SqlRow> SqlGo(string fullQuery, bool isSelect)
        {

            List<SqlRow> sqlRowList = new List<SqlRow>();
            List<string> rowSchemaList = new List<string>();
            string sqlConnectionWithString = "";

            if (isSelect == true)
            {
                sqlConnectionWithString = connectionString + "MultipleActiveResultSets=true";
            }
            else
            {
                sqlConnectionWithString = connectionString;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand sqlQuery = new SqlCommand(fullQuery, connection);
                //sqlQuery.ExecuteNonQuery();

                SqlDataReader querycommandreader = null;

                using (var reader = sqlQuery.ExecuteReader())
                {
                    // This will return false - we don't care, we just want to make sure the schema table is there.
                    //reader.Read();

                    var tableSchema = reader.GetSchemaTable();

                    // Each row in the table schema describes a column
                    if (tableSchema != null)
                    {
                        foreach (System.Data.DataRow row in tableSchema.Rows)
                        {
                            rowSchemaList.Add(row["ColumnName"].ToString().Trim());
                            //Console.WriteLine(row["ColumnName"]);
                        }

                        while (reader.Read())
                        {
                            List<Tuple<string, string>> queryReturnList = new List<Tuple<string, string>>();
                            for (int i = 0; i < rowSchemaList.Count; i++)
                            {
                                queryReturnList.Add(new Tuple<string, string>(rowSchemaList[i], reader[i].ToString().Trim()));
                            }
                            sqlRowList.Add(new SqlRow(queryReturnList));
                        }
                    }

                }

                connection.Close();
            }
            return sqlRowList;
        }

        public string CreateValues(List<string> stringList)
        {
            string valueString = "'";
            for (int i = 0; i < stringList.Count; i++)
            {
                if (i == (stringList.Count - 1))
                {
                    valueString += stringList[i] + "'";
                }
                else
                {
                    valueString += stringList[i] + "','";
                }
            }

            return valueString;
        }

        public string SQLReplaceAPO(string incomingString)
        {
            return incomingString.Replace("'", "''");
        }

        public string SQLReverseAPO(string incomingString)
        {
            return incomingString.Replace("''", "'");
        }




    }
}
