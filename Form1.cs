using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using NativeWifi;


namespace JK_s_NM
{
    public partial class Main_Form : Form
    {
        public Main_Form()
        {
            InitializeComponent();
        }
        private static System.Timers.Timer aTimer;
        private static System.Timers.Timer wifi_Timer;
        delegate void SetTextCallback(float sent, float received, long time);
        //delegate void SetDatapointCallback(string delay, string address, string hostname, string RDP);
        delegate void SetDatapointCallback(List<string> values);
        delegate void Set_wifi_datapoint_Callback(float[] points, long time);
        delegate void Set_progressbar_Callback(float progress);
        delegate void Set_IP_of_label_Callback(string text);
        bool running = false;
        bool wifi_running = false;


        private void Form1_Load(object sender, EventArgs e)
        {
            PerformanceCounterCategory performanceCounterCategory = new PerformanceCounterCategory("Network Interface");
            string[] adapters = performanceCounterCategory.GetInstanceNames();
            interface_label.Text = adapters[0];
            for (int i = 0; i < adapters.Length; i++)
            {
                ToolStripMenuItem item = new ToolStripMenuItem();
                item = new ToolStripMenuItem();
                item.Name = i.ToString();
                item.Tag = i.ToString();
                item.Text = adapters[i];
                item.Click += new EventHandler(ClickHandler);
                Interface_dropDownButton.DropDownItems.Add(item);
            }
            splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
        }



        private void ClickHandler(object sender, EventArgs e)
        {
            interface_label.Text = sender.ToString();
        }

        public void add_point(float sent, float received, long time)
        {
            if(this.chart.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(add_point);
                this.Invoke(d, new object[] { sent, received, time });
            }
            else
            {
                float timef = (float)time;
                chart.Series[0].Points.AddXY(timef/1000, sent/1000*8);
                chart.Series[1].Points.AddXY(timef/1000, received/1000 * 8);
            }
        }

        public void Set_IP_of_label(string text)
        {
            if (this.toolStrip2.InvokeRequired)
            {
                Set_IP_of_label_Callback d = new Set_IP_of_label_Callback(Set_IP_of_label);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                IP_of_label.Text = text;
            }
        }

        public void update_progressbar(float progress)
        {
            if (this.progressBar1.InvokeRequired)
            {
                Set_progressbar_Callback d = new Set_progressbar_Callback(update_progressbar);
                this.Invoke(d, new object[] { progress });
            }
            else
            {
                int intprogress = (int)(progress * 1000);
                progressBar1.Value = intprogress;
            }
        }

        private void OnTimedEvent(int network_interface_index, PerformanceCounter performanceCounterSent, PerformanceCounter performanceCounterReceived, Stopwatch stopWatch)
        {
            float sent = performanceCounterSent.NextValue() / 1024;
            float received = performanceCounterReceived.NextValue() / 1024;
            add_point(sent, received, stopWatch.ElapsedMilliseconds);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if(running == false)
            {
                running = true;
                chart.Series[0].Points.Clear();
                chart.Series[1].Points.Clear();
                int network_interface_index = -1;
                PerformanceCounterCategory performanceCounterCategory = new PerformanceCounterCategory("Network Interface");
                string[] adapters = performanceCounterCategory.GetInstanceNames();
                for (int i = 0; i < adapters.Length; i++)
                {
                    if (adapters[i] == interface_label.Text)
                    {
                        network_interface_index = i;
                    }
                }
                if (network_interface_index != -1)
                {
                    string instance = performanceCounterCategory.GetInstanceNames()[network_interface_index];
                    PerformanceCounter performanceCounterSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
                    PerformanceCounter performanceCounterReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Reset();
                    stopWatch.Start();


                    aTimer = new System.Timers.Timer();
                    aTimer.Interval = (int)(float.Parse(refresh_rate_textbox.Text) * 1000);
                    aTimer.Elapsed += delegate { OnTimedEvent(network_interface_index, performanceCounterSent, performanceCounterReceived, stopWatch); };
                    aTimer.AutoReset = true;
                    aTimer.Enabled = true;
                }
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            aTimer.Stop();
            running = false;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Csv file|*.csv";
            saveFileDialog1.Title = "Save to csv";
            saveFileDialog1.ShowDialog();
            string data = "Time, Sent, Received";
            for(int i = 0; i < chart.Series[0].Points.Count; i++)
            {
                data += "\n" + chart.Series[0].Points[i].XValue.ToString() + "," + chart.Series[0].Points[i].YValues[0].ToString() + "," + chart.Series[1].Points[i].YValues[0].ToString();
            }
            System.IO.File.WriteAllText(saveFileDialog1.FileName, data);
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Png file|*.png";
            saveFileDialog1.Title = "Save to png";
            saveFileDialog1.ShowDialog();
            chart.SaveImage(saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Png);

        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(tabControl.SelectedTab.Name == "IP_list_tabpage")
            {
                update_iplabel();
            }
            else if(tabControl.SelectedTab.Name == "wifi_Tabpage")
            {
                toolStripButton2_Click_1(sender, e);
            }
        }

        private void update_iplabel()
        {
            if(System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IP_label.Text = ip.ToString();
                        string subnet = "";
                        int n_dots = 0;
                        for(int i = 0; i < ip.ToString().Length; i++)
                        {
                            if(ip.ToString()[i] == '.')
                            {
                                n_dots++;
                            }
                            if(n_dots < 3)
                            {
                                subnet = subnet + ip.ToString()[i];
                            }
                        }
                        IP_subnet_textbox.Text = subnet + ".1/23";
                    }
                }
            }


        }

        private void start_ip_search_button_Click(object sender, EventArgs e)
        {
            if(System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                List<string> dataToPassToThread = new List<string>();
                dataToPassToThread.Add(IP_label.Text);
                dataToPassToThread.Add(IP_subnet_textbox.Text);
                Thread ipthread = new Thread(new ParameterizedThreadStart(Send_pings));
                ipthread.Start(dataToPassToThread);
            }
            else
            {
                MessageBox.Show("Network not available");
            }
        }

        private void Send_pings(object data)
        {
            List<string> datapassed = (List<string>)data; 
            string IP_label = datapassed[0];
            string subnet = datapassed[1];
            bool validsubnet = false;
            List<string> Addresses = new List<string>();
            //Addresses.Add("aalsa007");
            try
            {
                IPNetwork net = IPNetwork.Parse(subnet);
                IPNetworkCollection ips = IPNetwork.Subnet(net, 32);
                validsubnet = true;
                foreach (IPNetwork ipnetwork in ips)
                {
                    Addresses.Add(ipnetwork.ToString().Substring(0, ipnetwork.ToString().Length - 3));
                }
            }
            catch
            {
                MessageBox.Show("Invalid Subnet");
            }
            if(validsubnet)
            {
                Set_IP_of_label("/ " + Addresses.Count.ToString());
                List<Task<PingReply>> pingTasks = new List<Task<PingReply>>();
                for (int i = 0; i < Addresses.Count; i++)
                {
                    pingTasks.Add(PingAsync(Addresses[i]));
                }
                List<string> finishedIDs = new List<string>();
                List<string> toadd = new List<string>();
                finishedIDs.Add("Not an ip");
                bool alldone = false;
                //while (finishedIDs.Count != Addresses.Count)
                while(!alldone)
                {
                    alldone = true;
                    foreach (var t in pingTasks.ToArray())
                    {
                        update_progressbar((float)finishedIDs.Count / (float)Addresses.Count);
                        if (t.IsCompleted)
                        {
                            if (t.Result != null)
                            {
                                bool isShown = false;
                                foreach (var ID in finishedIDs)
                                {
                                    if (t.Result.Address.ToString() == ID)
                                    {
                                        isShown = true;
                                    }
                                }
                                if (!isShown)
                                {
                                    if (t.Result.Address.ToString() != "0.0.0.0")
                                    {
                                        //Thread updateUEthread = new Thread(new ParameterizedThreadStart(add_IP));
                                        List<string> results = new List<string>();
                                        //add_IP(t.Result.RoundtripTime.ToString() + " ms", t.Result.Address.ToString(), GetHostName(t.Result.Address.ToString()), RDP);
                                        results.Add(t.Result.Address.ToString());
                                        results.Add("-");
                                        results.Add(t.Result.RoundtripTime.ToString() + " ms");
                                        results.Add("-");
                                        results.Add("-");
                                        results.Add("-");
                                        add_IP(results);
                                        toadd.Add(t.Result.Address.ToString());

                                        Thread hostname_thread = new Thread(new ParameterizedThreadStart(update_hostname));
                                        hostname_thread.Start(t.Result.Address.ToString());
                                        Thread RDP_thread = new Thread(new ParameterizedThreadStart(update_RDP));
                                        RDP_thread.Start(t.Result.Address.ToString());
                                        Thread SSH_thread = new Thread(new ParameterizedThreadStart(update_SSH));
                                        SSH_thread.Start(t.Result.Address.ToString());
                                        Thread HTTP_thread = new Thread(new ParameterizedThreadStart(update_HTTP));
                                        HTTP_thread.Start(t.Result.Address.ToString());
                                    }
                                }
                            }
                            foreach (var toaddID in toadd)
                            {
                                finishedIDs.Add(toaddID);
                            }
                            toadd.Clear();
                            //MessageBox.Show(t.Result.RoundtripTime.ToString() + t.Result.Address);
                        }
                        else
                        {
                            alldone = false;
                        }
                    }
                    if(alldone)
                    {
                        update_progressbar(1);
                    }
                    Thread.Sleep(10);
                }
            }
        }
        private void update_SSH(object data)
        {
            string address = (string)data;
            string RDP = "-";
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(address, 22);
                    RDP = "Available";
                }
                catch (Exception)
                {
                    RDP = "Not available";
                }
            }
            List<string> results = new List<string>();
            results.Add(address);
            results.Add("-");
            results.Add("-");
            results.Add("-");
            results.Add(RDP);
            results.Add("-");
            add_IP(results);
        }

        private void update_RDP(object data)
        {
            string address = (string)data;
            string RDP = "-";
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(address, 3389);
                    RDP = "Available";
                }
                catch (Exception)
                {
                    RDP = "Not available";
                }
            }
            List<string> results = new List<string>();
            results.Add(address);
            results.Add("-");
            results.Add("-");
            results.Add(RDP);
            results.Add("-");
            results.Add("-");
            add_IP(results);
        }

        private void update_HTTP(object data)
        {
            string address = (string)data;
            string HTTP = "-";
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(address, 443);
                    HTTP = "HTTPS Available";
                }
                catch (Exception)
                {
                    try
                    {
                        tcpClient.Connect(address, 80);
                        HTTP = "Available";
                    }
                    catch
                    {
                        HTTP = "Not available";
                    }
                }
            }
            List<string> results = new List<string>();
            results.Add(address);
            results.Add("-");
            results.Add("-");
            results.Add("-");
            results.Add("-");
            results.Add(HTTP);
            add_IP(results);
        }

        private void update_hostname(object data)
        {
            string address = (string)data;
            List<string> results = new List<string>();
            results.Add(address);
            results.Add(GetHostName(address));
            results.Add("-");
            results.Add("-");
            results.Add("-");
            results.Add("-");
            add_IP(results);
        }

        static Task<PingReply> PingAsync(string address)
        {
            var tcs = new TaskCompletionSource<PingReply>();
            Ping ping = new Ping();
            ping.PingCompleted += (obj, sender) =>
            {
                tcs.SetResult(sender.Reply);
            };
            ping.SendAsync(address, new object());
            return tcs.Task;
        }

        private void add_IP(List<string> values)
        {
            if (this.dataGridView1.InvokeRequired)
            {
                SetDatapointCallback d = new SetDatapointCallback(add_IP);
                this.Invoke(d, new object[] { values });
            }
            else
            {
                bool editrow = false;
                foreach(DataGridViewRow row in dataGridView1.Rows)
                {
                    if(row.Cells[0].Value.ToString() == values[0])
                    {
                        for(int i = 1; i < values.Count; i++)
                        {
                            if(values[i] != "-")
                            {
                                row.Cells[i].Value = values[i];
                            }
                            if(i == 3 && values[3] == "Available")
                            {
                                row.Cells[i].Style.BackColor = Color.LightGreen;
                            }
                            if (i == 4 && values[4] == "Available")
                            {
                                row.Cells[i].Style.BackColor = Color.LightGreen;
                            }
                            if(i == 5 && values[5] == "HTTPS Available")
                            {
                                row.Cells[i].Style.BackColor = Color.LightGreen;
                            }
                            if (i == 5 && values[5] == "Available")
                            {
                                row.Cells[i].Style.BackColor = Color.Yellow;
                            }
                        }
                        editrow = true;
                    }
                }
                if(!editrow)
                {
                    dataGridView1.Rows.Add(values[0], values[1], values[2], values[3], values[4], values[5]);
                }
            }
        }

        public string GetHostName(string ipAddress)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                if (entry != null)
                {
                    return entry.HostName;
                }
            }
            catch
            {
            }

            return("-");
        }

        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            while(dataGridView1.Rows.Count > 0)
            {
                dataGridView1.Rows.RemoveAt(0);
            }
            IP_count_label.Text = "0";
            IP_of_label.Text = "/-";
        }

        private void dataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            int columnIndex = dataGridView1.CurrentCell.ColumnIndex;
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            if(columnIndex == 3)
            {
                if(dataGridView1.Rows[rowIndex].Cells[1].Value.ToString() != "-")
                {
                    System.Diagnostics.Process.Start("mstsc.exe", "/v:" + dataGridView1.Rows[rowIndex].Cells[1].Value.ToString());
                }
                else
                {
                    System.Diagnostics.Process.Start("mstsc.exe", "/v:" + dataGridView1.Rows[rowIndex].Cells[0].Value.ToString());
                }
            }
            else if(columnIndex == 4)
            {
                if(System.IO.File.Exists("C:\\Program Files\\PuTTY\\putty.exe"))
                {
                    if (dataGridView1.Rows[rowIndex].Cells[1].Value.ToString() != "-")
                    {
                        System.Diagnostics.Process.Start("C:\\Program Files\\PuTTY\\putty.exe", "-ssh " + dataGridView1.Rows[rowIndex].Cells[1].Value.ToString());
                    }
                    else
                    {
                        System.Diagnostics.Process.Start("C:\\Program Files\\PuTTY\\putty.exe", "-ssh " + dataGridView1.Rows[rowIndex].Cells[0].Value.ToString());
                    }
                }
                else
                {
                    MessageBox.Show("Putty not found in default location\nC:\\Program Files\\PuTTY\\putty.exe");
                }
            }
            else if(columnIndex == 5)
            {
                if (dataGridView1.Rows[rowIndex].Cells[1].Value.ToString() != "Available")
                {
                    System.Diagnostics.Process.Start("http://" + dataGridView1.Rows[rowIndex].Cells[0].Value.ToString());
                }
                else if (dataGridView1.Rows[rowIndex].Cells[1].Value.ToString() != "HTTPS Available")
                {
                    System.Diagnostics.Process.Start("https://" + dataGridView1.Rows[rowIndex].Cells[0].Value.ToString());
                }
            }

        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1_CellContentDoubleClick(sender, e);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void toolStripButton2_Click_1(object sender, EventArgs e)
        {
            while(wifi_dataGridView.Rows.Count > 0)
            {
                wifi_dataGridView.Rows.RemoveAt(0);
            }
            WlanClient client = new WlanClient();
            try
            {
                foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
                {
                    Wlan.WlanBssEntry[] wlanBssEntries = wlanIface.GetNetworkBssList();
                    foreach (Wlan.WlanBssEntry network in wlanBssEntries)
                    {
                        int rss = network.rssi;
                        byte[] macAddr = network.dot11Bssid;
                        string tMac = "";
                        for (int i = 0; i < macAddr.Length; i++)
                        {
                            tMac += macAddr[i].ToString("x2").PadLeft(2, '0').ToUpper();
                        }
                        wifi_dataGridView.Rows.Add(false, System.Text.ASCIIEncoding.ASCII.GetString(network.dot11Ssid.SSID).ToString(), network.linkQuality + " %", network.dot11BssType, tMac, rss.ToString());
                        //MessageBox.Show(network.chCenterFrequency.ToString());
                        //MessageBox.Show(network.DOT);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            wifi_dataGridView.Columns[0].Width = 40;
        }

        private void toolStripButton3_Click_1(object sender, EventArgs e)
        {
            wifi_dataGridView.Rows.Clear();
        }

        private void wifi_dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void wifi_dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int columnIndex = wifi_dataGridView.CurrentCell.ColumnIndex;
            int rowIndex = wifi_dataGridView.CurrentCell.RowIndex;
            if (columnIndex == 0)
            {
                wifi_dataGridView.Rows[rowIndex].Cells[0].Value = !(bool)wifi_dataGridView.Rows[rowIndex].Cells[0].Value;
            }
        }

        private void toolStripButton4_Click_1(object sender, EventArgs e)
        {
            if(!wifi_running)
            {
                while(wifi_chart.Series.Count > 0)
                {
                    wifi_chart.Series.RemoveAt(0);
                }
                int n_charts = 0;
                List<string> mac_IDs = new List<string>();
                foreach (DataGridViewRow row in wifi_dataGridView.Rows)
                {
                    if ((bool)row.Cells[0].Value)
                    {
                        bool isinlist = false;
                        for (int i = 0; i < wifi_chart.Series.Count; i++)
                        {
                            if (wifi_chart.Series[i].Name == row.Cells[1].Value.ToString())
                            {
                                isinlist = true;
                                wifi_chart.Series.Add(row.Cells[1].Value.ToString() + " " + row.Cells[4].Value.ToString());
                            }
                        }
                        if (!isinlist)
                        {
                            wifi_chart.Series.Add(row.Cells[1].Value.ToString());
                        }
                        wifi_chart.Series[n_charts].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                        mac_IDs.Add(row.Cells[4].Value.ToString());
                        n_charts++;
                    }
                }
                if (mac_IDs.Count > 0)
                {
                    Stopwatch wifi_stopWatch = new Stopwatch();
                    wifi_stopWatch.Reset();
                    wifi_stopWatch.Start();
                    wifi_Timer = new System.Timers.Timer();
                    wifi_Timer.Interval = (int)(float.Parse(refresh_rate_textbox.Text) * 1000);
                    wifi_Timer.Elapsed += delegate { log_data(mac_IDs, wifi_stopWatch); };
                    wifi_Timer.AutoReset = true;
                    wifi_Timer.Enabled = true;
                    wifi_running = true;
                }
            }
        }




        private void log_data(object data, Stopwatch stopWatch)
        {
            List<string> mac_IDs = (List<string>)data;
            float[] rsrpid = new float[mac_IDs.Count];
            try
            {
                WlanClient client = new WlanClient();
                foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
                {
                    Wlan.WlanBssEntry[] wlanBssEntries = wlanIface.GetNetworkBssList();
                    foreach (Wlan.WlanBssEntry network in wlanBssEntries)
                    {
                        int rss = network.rssi;
                        byte[] macAddr = network.dot11Bssid;
                        string tMac = "";
                        for (int i = 0; i < macAddr.Length; i++)
                        {
                            tMac += macAddr[i].ToString("x2").PadLeft(2, '0').ToUpper();
                        }
                        for(int i = 0; i < mac_IDs.Count; i++)
                        {
                            if (mac_IDs[i] == tMac)
                            {
                                rsrpid[i] = (float)rss;
                            }
                        }
                        //wifi_dataGridView.Rows.Add(false, System.Text.ASCIIEncoding.ASCII.GetString(network.dot11Ssid.SSID).ToString(), network.linkQuality + " %", network.dot11BssType, tMac, rss.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                // Do nothing
            }
            add_wifi_point(rsrpid, stopWatch.ElapsedMilliseconds);
        }

        public void add_wifi_point(float[] points, long time)
        {
            if (this.chart.InvokeRequired)
            {
                Set_wifi_datapoint_Callback d = new Set_wifi_datapoint_Callback(add_wifi_point);
                this.Invoke(d, new object[] { points, time });
            }
            else
            {
                float timef = (float)time;
                for(int i = 0; i < points.Length; i++)
                {
                    if(points[i] != 0)
                    {
                        wifi_chart.Series[i].Points.AddXY(timef / 1000, points[i]);
                    }
                    else
                    {
                        wifi_chart.Series[i].Points.AddXY(timef / 1000, -300);
                    }
                }
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in wifi_dataGridView.Rows)
            {
                row.Cells[0].Value = !(bool)row.Cells[0].Value;
            }
        }

        private void wifi_stop_log_button_Click(object sender, EventArgs e)
        {
            if(wifi_running)
            {
                wifi_Timer.Stop();
                wifi_running = false;
            }
        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            IP_count_label.Text = (double.Parse(IP_count_label.Text) + 1).ToString();
        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {

        }
    }
}
