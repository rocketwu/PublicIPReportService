using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PublicIPReportService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
            IPLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("IPReportSource"))
            {
                System.Diagnostics.EventLog.CreateEventSource("IPReportSource", "IPReportLog");
            }
            IPLog.Source = "IPReportSource";
            IPLog.Log = "IPReportLog";
        }

        protected override void OnStart(string[] args)
        {
            IPLog.Clear();
            IPLog.WriteEntry("IP report service start!");
            eID = IPLog.Entries.Count;
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 30000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(getIPAsync);
            timer.Start();
        }

        protected override void OnStop()
        {
            IPLog.WriteEntry(IPLog.Entries[eID].Message, EventLogEntryType.Warning, (int)IPLog.Entries[eID].InstanceId);
            //IPLog.Clear();
            IPLog.WriteEntry("IP report service stop!");
        }

        private int eID = 0;

        public async void getIPAsync(object sender, System.Timers.ElapsedEventArgs args)
        {
            var client = new HttpClient();

            eID = IPLog.Entries.Count;

            HttpResponseMessage response = await client.GetAsync("https://api.ipify.org?format=json");
            HttpContent responseContent = response.Content;

            // Get the stream of the content.
            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                // Write the output.
                string result = await reader.ReadToEndAsync();
                result = result.Split('\"')[3];
                if (IPLog.Entries[eID-1].Message != result) {
                    IPLog.WriteEntry(result, EventLogEntryType.Information, eID);
                }
                
            }
        }
    }
}
