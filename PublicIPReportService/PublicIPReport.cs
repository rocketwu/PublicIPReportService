using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Mail;
using System.ServiceProcess;
using System.Net;

namespace PublicIPReportService
{
    public partial class PublicIPReport : ServiceBase
    {
        private int eID {
            get {return IPLog.Entries.Count; }
        }

        private int eIndex
        {
            //the index of the latest event entry
            get { return eID - 1; }
        }

        private string mailAddr = "";   //TODO: FILL IN THE MAIL ADDRESS
        private string mailPass = "";   //TODO: FILL IN THE PASSWORD

        public PublicIPReport()
        {
            InitializeComponent();

            //setup event log
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
            IPLog.Clear();  //clear previous event
            IPLog.WriteEntry("IP report service start!");
            getIPAsync(null, null);

            //set a timer for fetching public ip address
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 20*60000;     //set the time interval (ms)
            timer.Elapsed += new System.Timers.ElapsedEventHandler(getIPAsync);     //set the event handler when the count down finished. 
            timer.Start();
        }

        protected override void OnStop()
        {
            IPLog.Clear();
            IPLog.WriteEntry("IP report service stop!");
        }



        public async void getIPAsync(object sender, System.Timers.ElapsedEventArgs args)
        {
            var client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync("https://api.ipify.org?format=json");      //fetch public address from api.ipify.org
            HttpContent responseContent = response.Content;

            // Get the stream of the content.
            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                // Write the output.
                string result = await reader.ReadToEndAsync();      //get the public ip reported from api.ipify.org
                result = result.Split('\"')[3];
                if (IPLog.Entries[eIndex].Message != result) {
                    //when the ip changes, do ...
                    IPLog.WriteEntry(result, EventLogEntryType.Information, eID);   //log the new ip address
                    //TODO: notify method                    
                    MailAddress from = new MailAddress(mailAddr, "IP Reporter");
                    MailAddress to = new MailAddress(mailAddr);

                    SmtpClient mailClient = new SmtpClient("smtp.gmail.com", 587);
                    mailClient.EnableSsl = true;
                    mailClient.Credentials = new NetworkCredential(from.Address, mailPass);

                    MailMessage message = new MailMessage(from, to);
                    message.Body = "Your new public IP address is: \n " + result;
                    message.Subject = "IP address changed";

                    mailClient.Send(message);
                    
                }
                
            }
        }
    }
}
