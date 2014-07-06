using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Linq;
using Nancy;
using Nancy.Responses;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace RestTest.Modules
{
    public class ServiceModule : NancyModule
    {
        private readonly Storage _storage;

        public ServiceModule(Storage storage) : this()
        {
            _storage = storage;
        }

        public ServiceModule()
        {
            Post["/{key}"] = _ => ProcessRequest("POST", _);
            Get["/{key}"] = _ => ProcessRequest("GET", _);
            Put["/{key}"] = _ => ProcessRequest("PUT", _);
            Delete["/{key}"] = _ => ProcessRequest("DELETE", _);

            Get["/{key}/View"] = _ =>
            {
                var results = _storage.GetRecords((string)_.key);
                var resultsViewModel = new ResultsViewModel
                {
                    Key = (string)_.key,
                    Results = results.Select(r => new RequestDataViewModel
                    {
                        Key = r.PartitionKey,
                        RowKey = r.RowKey,
                        RequestTime = r.RequestTime.ToString("G"),
                        Body = FormatBody(r.Body, r.Header),
                        Header = r.Header,
                        Verb = r.Verb
                    })
                };
                return View["View2", resultsViewModel];
            };

            Get["/{key}/Clear"] = _ =>
            {
                _storage.DeleteAll((string) _.key);
                return "OK";
            };

            Get["/Dump"] = _ =>
            {
                _storage.DumpTable();
                return "Ok, dumped everything";
            };
        }

        private string FormatBody(string body, string header)
        {
            if (header.ToLower().Contains("application/xml") || header.ToLower().Contains("text/xml") || header.ToLower().Contains("application/soap+xml"))
            {
                try
                {
                    XDocument doc = XDocument.Parse(body);
                    return doc.ToString();
                }
                catch (Exception)
                {
                    // oh well, we tried
                }
            }
            else if (header.ToLower().Contains("application/json"))
            {
                try
                {
                    return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(body), Formatting.Indented);
                }
                catch (Exception)
                {
                    // eh, we tried
                }
            }
            return body;
        }

        private TextResponse ProcessRequest(string verb, dynamic _)
        {
            var key = (string)_.key;
            var req = new RequestData(key)
            {
                Body = new StreamReader(Request.Body).ReadToEnd(),
                Header = string.Join("\n", Request.Headers.Select(r => r.Key + ": " + string.Join(", ", r.Value))),
                Verb = verb,
                RequestTime = DateTime.Now
            };

            _storage.WriteRecord(req);

            return new TextResponse();
        }
    }

    public class ResultsViewModel
    {
        public string Key { get; set; }
        public IEnumerable<RequestDataViewModel> Results { get; set; }
    }
    public class RequestDataViewModel
    {
        public string Key { get; set; }
        public string RowKey { get; set; }
        public string RequestTime { get; set; }
        public string Verb { get; set; }

        public string Header { get; set; }

        public string Body { get; set; }
    }
}