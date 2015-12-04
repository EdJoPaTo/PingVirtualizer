using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PingVirtualizer
{
    public class PingReceivedEventArgs : EventArgs
    {
        public List<PingStats> History { get; set; }
    }

    public class PingStats : EventArgs
    {
        public DateTime DateTime { get; set; }
        public PingReply PingReply { get; set; }
    }

    public class PingHandler
    {
        #region Events

        public event EventHandler<PingReceivedEventArgs> PingReceived;

        #endregion

        #region Properties

        public string Hostname { get; private set; }

        public int SecondsOfStatTime { get; set; }

        public int PingTimeoutMilliseconds { get; set; }

        public int MinimumMillisecondsWaittimeBetweenPings { get; set; }

        public string FormatPercentage { get; set; }
        public string FormatPingRoundtripTime { get; set; }


        private bool _running = false;
        public bool Running
        {
            get { return _running; }
            set
            {
                if (_running == value)
                    return;

                _running = value;

                if (_running)
                {
                    Thread bla = new Thread(() => PingThread());
                    bla.IsBackground = true;
                    bla.Start();
                }
            }
        }

        private Stack<PingStats> _history;
        public List<PingStats> History
        {
            get
            {
                return _history
                    .ToList()
                    .Where(o => o.DateTime > DateTime.Now.AddSeconds(-1 * SecondsOfStatTime))
                    .ToList();
                ;
            }
        }

        public double Average
        {
            get
            {
                return History
                    .Where(o => o.PingReply.Status == IPStatus.Success)
                    .Average(o => o.PingReply.RoundtripTime);
            }
        }

        public double Deviation
        {
            get
            {
                return Math.Sqrt(History
                    .Where(o => o.PingReply.Status == IPStatus.Success)
                    .Deviation(o => o.PingReply.RoundtripTime)
                    );
            }
        }

        public int PackagesTotal { get { return History.Count; } }

        public Dictionary<IPStatus, int> PackageStatusRates
        {
            get
            {
                return History
                    .Select(o => o.PingReply.Status)
                    .GroupBy(o => o)
                    .ToDictionary(o => o.Key, o => o.Count());
            }
        }

        #endregion

        public PingHandler(string hostname)
        {
            Hostname = hostname;

            _history = new Stack<PingStats>();
            SecondsOfStatTime = 60; // 1 min
            PingTimeoutMilliseconds = 20000; // 20 sec
            MinimumMillisecondsWaittimeBetweenPings = 250; // max 4 Ping per sec
            FormatPercentage = "#00.0";
            FormatPingRoundtripTime = "#,##0.0";
        }

        private void PingThread()
        {
            var myPing = new Ping();
            var stopwatch = new Stopwatch();

            while (_running)
            {
                stopwatch.Restart();
                var reply = myPing.Send(Hostname, PingTimeoutMilliseconds);

                var eventArgs = new PingStats()
                {
                    DateTime = DateTime.Now,
                    PingReply = reply
                };

                _history.Push(eventArgs);

                if (PingReceived != null)
                    PingReceived(this, new PingReceivedEventArgs() { History = History });

                var millis = Convert.ToInt32(stopwatch.ElapsedMilliseconds);
                if (millis < MinimumMillisecondsWaittimeBetweenPings)
                    Thread.Sleep(MinimumMillisecondsWaittimeBetweenPings - millis);
            }
        }

        #region ToString

        public override string ToString()
        {
            var returnString = string.Empty;

            returnString += Hostname;
            returnString += Environment.NewLine;

            var avg = 0.0;
            var dev = 0.0;

            var successfulCount = History.Count(o => o.PingReply.Status == IPStatus.Success);

            if (successfulCount > 0)
            {
                avg = Average;
                dev = Deviation;
            }

            returnString += avg.ToString(FormatPingRoundtripTime) + " ± " + dev.ToString(FormatPingRoundtripTime);

            foreach (var item in PackageStatusRates)
            {
                returnString += Environment.NewLine;
                returnString += (100.0 * item.Value / PackagesTotal).ToString(FormatPercentage) + "% " + item.Key;
            }
            returnString += Environment.NewLine;

            return returnString;
        }

        #endregion
    }
}

