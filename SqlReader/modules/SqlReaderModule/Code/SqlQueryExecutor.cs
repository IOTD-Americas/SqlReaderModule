using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
namespace Helper
{
    public class SqlQueryExecutor
    {
        string connectionString;
        string query;
        SqlConnection sqlConnection;
        DataTable tableSchema;
        Dictionary<string, Type> schema = new Dictionary<string, Type>();

        bool verbose = false;

        public SqlQueryExecutor(string connectionString, string query, bool verbose = false)
        {
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(query))
                throw new ArgumentException("ConnectionString and/or query missing");

            this.connectionString = connectionString;
            this.query = query;
            this.sqlConnection = new SqlConnection(connectionString);
            this.verbose = verbose;
            sqlConnection.Open();
        }

        //Obtiene el resultado del query, si es json
        public async Task<string> GetJsonResult()
        {
            string json = string.Empty;
            try
            {
                if (this.sqlConnection != null & this.sqlConnection.State != ConnectionState.Open)
                    await this.sqlConnection.OpenAsync();

                SqlCommand sqlCommand = new SqlCommand(this.query, this.sqlConnection);

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                {
                    int counter = 0;
                    while (await reader.ReadAsync())
                    {
                        json = reader.GetString(0);
                        counter++;
                    }

                    Logger.Writer.LogInformation($"{counter} rows were retrieved");
                }
                return json;
            }
            catch (Exception ex)
            {
                string message = verbose ? ex.Message : ex.ToString();
                Logger.Writer.LogError(ex, $"Error ocurred: {message}");
            }

            return json;
        }

        public async IAsyncEnumerable<string> GetQueryResultPackages(int maxBatchSize = int.MaxValue)
        {
            string jsonResult = string.Empty;

            if (this.tableSchema == null || schema == null)
                await this.InitializeSchema();

            if (this.sqlConnection != null & this.sqlConnection.State != ConnectionState.Open)
                await this.sqlConnection.OpenAsync();

            SqlCommand sqlCommand = new SqlCommand(this.query, this.sqlConnection);
            sqlCommand.CommandTimeout = 0;
            DateTime queryStart = DateTime.Now;

            using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection))
            {
                DateTime queryFinish = DateTime.Now;
                Logger.Writer.LogInformation($"Query took {(queryFinish - queryStart).TotalSeconds} seconds");
                int counter = 0;
                int packages = 0;
                JArray jsonArray = new JArray();
                while (await reader.ReadAsync())
                {
                    JObject objJson = new JObject();
                    foreach (var item in schema)
                    {
                        object columnValue = reader.GetValue(item.Key);
                        if (columnValue == DBNull.Value)
                            continue;

                        dynamic realValue = Convert.ChangeType(columnValue, item.Value);
                        objJson.Add(item.Key, realValue);

                    }
                    jsonArray.Add(objJson);
                    jsonResult = jsonArray.ToString(Formatting.None);
                    counter++;

                    if (counter >= maxBatchSize)
                    {
                        jsonResult = jsonArray.ToString(Formatting.None);
                        yield return jsonResult;

                        jsonResult = string.Empty;
                        jsonArray = new JArray();
                        counter = 0;
                        packages++;
                    }
                }
                //hay algunos registros que deben enviarse
                if (counter > 0)
                {
                    jsonResult = jsonArray.ToString(Formatting.None);
                    yield return jsonResult;
                }
                Logger.Writer.LogInformation($"{(packages * maxBatchSize) + counter} rows were retrieved");
            }
        }


        public async Task<string> GetQueryResult()
        {
            string jsonResult = string.Empty;
            try
            {
                if (this.tableSchema == null || schema == null)
                    await this.InitializeSchema();

                if (this.sqlConnection != null & this.sqlConnection.State != ConnectionState.Open)
                    await this.sqlConnection.OpenAsync();

                SqlCommand sqlCommand = new SqlCommand(this.query, this.sqlConnection);
                DateTime queryStart = DateTime.Now;

                using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                {
                    DateTime queryFinish = DateTime.Now;
                    Logger.Writer.LogInformation($"Query took {(queryFinish - queryStart).TotalSeconds} seconds");
                    int counter = 0;
                    JArray jsonArray = new JArray();
                    while (await reader.ReadAsync())
                    {
                        JObject objJson = new JObject();
                        foreach (var item in schema)
                        {
                            object columnValue = reader.GetValue(item.Key);
                            if (columnValue == DBNull.Value)
                                continue;

                            dynamic realValue = Convert.ChangeType(columnValue, item.Value);
                            objJson.Add(item.Key, realValue);

                        }
                        jsonArray.Add(objJson);
                        jsonResult = jsonArray.ToString(Formatting.None);
                        counter++;
                    }
                    Logger.Writer.LogInformation($"{counter} rows  retrieved", verbose);
                }

                return jsonResult;
            }
            catch (Exception ex)
            {
                string message = verbose ? ex.Message : ex.ToString();
                Logger.Writer.LogError(ex, $"Error ocurred: {message}");
            }
            return jsonResult;
        }

        private async Task InitializeSchema()
        {
            if (this.sqlConnection != null & this.sqlConnection.State != ConnectionState.Open)
                await this.sqlConnection.OpenAsync();

            SqlCommand sqlCommand = new SqlCommand(this.query, this.sqlConnection);
            using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SchemaOnly | CommandBehavior.CloseConnection))
            {
                tableSchema = reader.GetSchemaTable();
            }

            schema = new Dictionary<string, Type>();
            foreach (DataRow col in tableSchema.Rows)
            {
                schema.Add(col.Field<String>("ColumnName"), col.Field<System.Type>("DataType"));
            }

        }

    }
}