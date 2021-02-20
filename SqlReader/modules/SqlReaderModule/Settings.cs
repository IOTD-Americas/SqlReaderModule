namespace SqlReaderModule
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Helper;
    using Microsoft.Azure.Devices.Shared;

    internal class Settings
    {
        public static Settings Current = Create();

        private Settings(string connectionString, string sqlQuery, bool isSqlQueryJson, int intervalMiliseconds, int maxBatchSize, bool verbose)
        {
            if(!string.IsNullOrEmpty(connectionString))
                this.ConnectionString = connectionString;
            else
                throw new ArgumentException("Connection string was not provided");

            if(!string.IsNullOrEmpty(sqlQuery))
                this.SqlQuery = sqlQuery;
            else
                throw new ArgumentException("SqlQuery was not provided");

            
            this.PoolingIntervalMiliseconds = intervalMiliseconds>0? intervalMiliseconds:60000;
            this.MaxBatchSize = maxBatchSize>0? maxBatchSize:60000;
            this.IsSqlQueryJson = isSqlQueryJson;
            this.Verbose = verbose;
        }

        public static bool RebuildFromTwinModule(TwinCollection desiredProperties)
        {
            Settings settings= null;
            try
            {
            string connectionString=string.Empty;
            string sqlQuery=string.Empty;
             bool isSqlQueryJson=false;
              int intervalMiliseconds=60000;
               int maxBatchSize=200;
                bool verbose=false;

             if (desiredProperties["ConnectionString"] != null)
                    connectionString = desiredProperties["ConnectionString"];

            if (desiredProperties["SqlQuery"] != null)
                sqlQuery = desiredProperties["SqlQuery"];

            if (desiredProperties["PoolingIntervalMiliseconds"] != null)
                intervalMiliseconds = desiredProperties["PoolingIntervalMiliseconds"];

            if (desiredProperties["IsSqlQueryJson"] != null)
                isSqlQueryJson = desiredProperties["IsSqlQueryJson"];

            if (desiredProperties["MaxBatchSize"] != null)
                maxBatchSize = desiredProperties["MaxBatchSize"];

            if (desiredProperties["Verbose"] != null)
                verbose = desiredProperties["Verbose"];

            settings = new Settings(connectionString, sqlQuery, isSqlQueryJson, intervalMiliseconds, maxBatchSize, verbose);
            if(settings!=null)
            {
                Logger.Writer.LogInformation("Correctly reading settings from Twin Module");
                Settings.Current =settings;
            }

                    return true;
             }
            catch (ArgumentException e)
            {
                Logger.Writer.LogCritical("Error reading arguments from TwinCollection vaiables.");
                Logger.Writer.LogCritical(e.ToString());
                settings = null;
                return false;
            }  
        }

        private static Settings Create()
        {
            try
            {
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("settings.json", true)
                    .AddEnvironmentVariables()
                    .Build();

                return new Settings(
                    configuration.GetValue<string>("ConnectionString"),
                    configuration.GetValue<string>("SqlQuery"),
                    configuration.GetValue<bool>("IsSqlQueryJson", false),
                     configuration.GetValue<int>("PoolingIntervalMiliseconds", 60000),
                      configuration.GetValue<int>("MaxBatchSize", 200),
                    configuration.GetValue<bool>("Verbose", true)
                    );
            }
            catch (ArgumentException e)
            {
                Logger.Writer.LogCritical("Error reading arguments from environment variables. Make sure all required parameter are present");
                Logger.Writer.LogCritical(e.ToString());
                Environment.Exit(2);
                throw new Exception();  // to make code analyzers happy (this line will never run)
            }
        }


 public string ConnectionString { get; }
  public string SqlQuery { get; }
  public bool IsSqlQueryJson{get;}
  public int PoolingIntervalMiliseconds{get;}

public int MaxBatchSize{get;}
public bool Verbose{get;}



        // TODO: is this used anywhere important? Make sure to test it if so
        public override string ToString()
        {
            string HostName = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");
            Console.WriteLine($"IOTEDGE_GATEWAYHOSTNAME: {HostName}");

            var fields = new Dictionary<string, string>()
            {
                { nameof(this.ConnectionString), this.ConnectionString },
                 { nameof(this.SqlQuery), this.SqlQuery },
                 { nameof(this.PoolingIntervalMiliseconds), this.PoolingIntervalMiliseconds.ToString() },
                { nameof(this.MaxBatchSize), this.MaxBatchSize.ToString() },
                                { nameof(this.IsSqlQueryJson), this.IsSqlQueryJson.ToString() },
                                 { nameof(this.Verbose), this.Verbose.ToString() }
               

            };

            return $"Settings:{Environment.NewLine}{string.Join(Environment.NewLine, fields.Select(f => $"{f.Key}={f.Value}"))}";
        }
    }
}
