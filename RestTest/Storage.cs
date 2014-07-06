using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace RestTest
{
    public class Storage
    {
        private readonly CloudStorageAccount _account;

        public Storage()
        {
            var useEmulated = true;
            bool.TryParse(ConfigurationManager.AppSettings["UseEmulatedStorage"], out useEmulated);
            _account = CloudStorageAccount.Parse(
                useEmulated ?
                ConfigurationManager.AppSettings["EmulatedConnectionString"] :
                ConfigurationManager.AppSettings["StorageConnectionString"]);
        }

        public bool WriteRecord(RequestData request)
        {
            var tableClient = _account.CreateCloudTableClient();

            var table = tableClient.GetTableReference("requests");
            table.CreateIfNotExists();

            var operation = TableOperation.Insert(request);

            try
            {
                table.Execute(operation);
                return true;
            }
            finally
            {
            }
            return false;
        }

        public IEnumerable<RequestData> GetRecords(string key, int take = 5)
        {
            var tableClient = _account.CreateCloudTableClient();

            var table = tableClient.GetTableReference("requests");

            TableQuery<RequestData> query = new TableQuery<RequestData>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey",QueryComparisons.Equal,key))
                .Take(take);

            return table.ExecuteQuery(query);
        }

        public void DeleteAll(string key)
        {
            var tableClient = _account.CreateCloudTableClient();

            var table = tableClient.GetTableReference("requests");

            TableQuery<RequestData> query = new TableQuery<RequestData>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, key));

            var batchDelete = new TableBatchOperation();
            foreach (var entity in table.ExecuteQuery(query))
            {
                batchDelete.Add(TableOperation.Delete(entity));
            }
            table.ExecuteBatch(batchDelete);
        }

        public void DumpTable()
        {
            var tableClient = _account.CreateCloudTableClient();

            var table = tableClient.GetTableReference("requests");

            table.Delete();
        }
    }

    public class RequestData : TableEntity
    {
        public RequestData(string key)
        {
            PartitionKey = key;
            RowKey = (DateTime.MaxValue.Ticks - DateTime.Now.Ticks).ToString("{0:d20}");
        }

        public RequestData() { }

        public DateTime RequestTime { get; set; }
        public string Verb { get; set; }

        public string Header { get; set; }

        public string Body { get; set; }
    }
}