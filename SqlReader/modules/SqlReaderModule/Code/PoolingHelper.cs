using System;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;

namespace Helper
{
    public class PoolingHelper
    {
        string connectionString { get; set; }
        string sqlQuery { get; set; }
        int poolingIntervalMiliseconds;

        bool IsSqlQueryJson;
        SqlQueryExecutor queryExecutor;
        ModuleClient ioTHubModuleClient;
        Timer timer;
        bool verbose = false;
        int maxBatchSize = int.MaxValue;

         public PoolingHelper(ModuleClient ioTHubModuleClient, string connectionString, string sqlQuery, bool isSqlQueryJson, int poolingIntervalMiliseconds = 1000, int maxBatchSize = int.MaxValue, bool verbose = false)
        {
            this.connectionString = connectionString;
            this.sqlQuery = sqlQuery;
            this.poolingIntervalMiliseconds = poolingIntervalMiliseconds;
            this.IsSqlQueryJson = isSqlQueryJson;
            this.ioTHubModuleClient = ioTHubModuleClient;
            this.verbose = verbose;
            this.maxBatchSize = maxBatchSize;
            this.timer = new Timer();

            if (string.IsNullOrEmpty(this.connectionString) || string.IsNullOrEmpty(this.sqlQuery))
            {
                this.poolingIntervalMiliseconds = int.MaxValue;
            }
            this.queryExecutor = new SqlQueryExecutor(this.connectionString, this.sqlQuery, this.verbose);
            this.timer.Interval = this.poolingIntervalMiliseconds;
            this.timer.Elapsed += Timer_Elapsed;

            Task.Run(() => PooligData());
        }

        public void Run()
        {
            Logger.Writer.LogInformation($"Initializing pooling every {this.poolingIntervalMiliseconds} miliseconds...");
            this.timer.Start();
        }
        public void Stop()
        {
            Logger.Writer.LogInformation($"Stopping pooling...");
            this.timer.Stop();
        }


        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Task.Run(() => PooligData());
        }

        private async Task PooligData()
        {
            Logger.Writer.LogInformation("Pooling data...");
            try
            {
                string jsonMessage = string.Empty;
                if (this.IsSqlQueryJson)
                    jsonMessage = this.queryExecutor.GetJsonResult().Result;
                else
                {
                    int counterPackages = 0;

                    await foreach (var package in this.queryExecutor.GetQueryResultPackages(this.maxBatchSize))
                    {
                        Logger.Writer.LogInformation($"Enviando paquete {++counterPackages}...");
                        int counter=1;
                        await SendTelemetry(package, counter++);
                    }
                }


            }
            catch (System.Exception ex)
            {
                string message = verbose ? ex.Message : ex.ToString();
                Logger.Writer.LogError(ex, $"Error ocurred Timer_Elapsed. Error{message}");
            }
        }


        private async Task SendTelemetry(string jsonMessage, int counter)
        {
            try
            {
                Logger.Writer.LogInformation($"Message to send: {jsonMessage}");
                var message = new Message(Encoding.UTF8.GetBytes(jsonMessage));
                
                await ioTHubModuleClient.SendEventAsync("output1", message);
                Logger.Writer.LogInformation($"{counter}.- Message sent!");
            }
            catch (System.Exception ex)
            {
                string message = verbose ? ex.Message : ex.ToString();
                Logger.Writer.LogError(ex, $"Error ocurred SendTelemetry. Error{message}");
            }
        }

        ~PoolingHelper()
        {
            Logger.Writer.LogInformation($"Stopping timer...");
            if (this.timer != null)
            {
                this.timer.Stop();
                this.timer.Dispose();
            }
        }
    }
}