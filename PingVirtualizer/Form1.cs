using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PingVirtualizer
{
    public partial class Form1 : Form
    {
        Random _random = new Random();
        private HashSet<PingHandler> _pingHandler;
        private Thread _updateGuiThread;
        private int _actualizationIntervall = 500;

        public Form1(string[] additionalUrls)
        {
            InitializeComponent();

            _chartBoxPlot.ChartAreas["Data"].AxisX.Minimum = -60;
            _chartBoxPlot.ChartAreas["Data"].AxisX.Maximum = 0;

            _pingHandler = new HashSet<PingHandler>();
            _pingHandler.Add(new PingHandler("google.com"));
            _pingHandler.Add(new PingHandler("8.8.8.8"));
            foreach (var url in additionalUrls)
            {
                if (!_pingHandler.Select(o => o.Hostname).Contains(url))
                    _pingHandler.Add(new PingHandler(url));
            }

            _chartBoxPlot.Series.Clear();
            foreach (PingHandler handler in _pingHandler)
            {
                AddLineToChart(handler.Hostname, handler);
            }

            _updateGuiThread = new Thread(() => UpdateGuiThread());
            _updateGuiThread.IsBackground = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _updateGuiThread.Start();

            foreach (var handler in _pingHandler)
            {
                handler.Running = true;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (var handler in _pingHandler)
            {
                handler.Running = false;
            }
        }

        private void UpdateGuiThread()
        {
            try
            {
                while (!this.IsDisposed)
                {
                    _chartBoxPlot.SuspendLayout();

                    foreach (var item in _pingHandler)
                    {
                        ShowResult(item);
                    }

                    _chartBoxPlot.ResumeLayout();

                    Thread.Sleep(_actualizationIntervall);
                }
            }
            catch (Exception ex)
            { }
        }

        private void ShowResult(PingHandler handler)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => ShowResult(handler)));
                    return;
                }

                if (_chartBoxPlot.IsDisposed)
                    return;

                try
                {
                    var history = handler.History.Where(o => o.PingReply.Status == IPStatus.Success).ToList();

                    if (_chartBoxPlot.Series.FindByName(handler.Hostname) == null)
                    {
                        AddLineToChart(handler.Hostname, handler);
                    }

                    _chartBoxPlot.Series[handler.Hostname].Points.Clear();

                    int backlogSeconds = Convert.ToInt32(_chartBoxPlot.ChartAreas["Data"].AxisX.Minimum);
                    double startTime = DateTime.Now.AddSeconds(backlogSeconds).ToUTCUnixTimestamp();

                    history = history.Where(o => o.DateTime >= DateTime.Now.AddSeconds(backlogSeconds)).ToList();

                    foreach (var item in history)
                    {
                        _chartBoxPlot.Series[handler.Hostname].Points.AddXY(
                            item.DateTime.ToUTCUnixTimestamp() - startTime + backlogSeconds,
                            item.PingReply.RoundtripTime);
                    }
                    _chartBoxPlot.Series["BoxPlot-" + handler.Hostname].LegendText = handler.ToString();

                    _chartBoxPlot.Series.OrderPositionsBy(o => (o.Tag as PingHandler).History.Count(i => i.PingReply.Status == IPStatus.Success) > 0, o => (o.Tag as PingHandler).Average);
                }
                catch (InvalidOperationException) { }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            { }
        }

        private void AddLineToChart(string name, object tag = null)
        {
            Color currentColor = GetRandomColor();

            Series currentSeries = _chartBoxPlot.Series.Add(name);
            currentSeries.Tag = tag;
            currentSeries.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            //currentSeries.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            currentSeries.ChartArea = "Data";
            currentSeries.BorderWidth = 2;
            currentSeries.IsVisibleInLegend = false;
            currentSeries.Color = currentColor;
            //currentSeries.IsValueShownAsLabel = true;

            // Specify data series name for the Box Plot.
            currentSeries = _chartBoxPlot.Series.Add("BoxPlot-" + name);
            currentSeries.Tag = tag;
            currentSeries.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.BoxPlot;
            currentSeries.ChartArea = "BoxPlot";
            currentSeries.BorderWidth = 2;
            currentSeries.Color = currentColor;
            currentSeries["BoxPlotWhiskerPercentile"] = "10";
            currentSeries["BoxPlotShowAverage"] = "true";
            currentSeries["BoxPlotShowMedian"] = "true";
            currentSeries["BoxPlotShowUnusualValues"] = "true";

            currentSeries["BoxPlotSeries"] = name;
        }

        private Color GetRandomColor()
        {
            var color = Color.FromArgb((byte)_random.Next(0, 256), (byte)_random.Next(0, 256), (byte)_random.Next(0, 256));

            var sum = color.R + color.G + color.B;
            if (sum < 70 || sum > 700)
                color = GetRandomColor();

            return color;
        }

        private void Form1_Activated(object sender, EventArgs e) { ActializationIntervall(250); }

        private void Form1_Deactivate(object sender, EventArgs e) { ActializationIntervall(1000); }

        private void ActializationIntervall(int intervall)
        {
            _actualizationIntervall = intervall;
        }
    }
}
