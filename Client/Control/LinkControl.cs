﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using RestSharp;
using System.Threading;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Client.Control
{
    public partial class LinkControl : UserControl
    {
        private string _linkPath = Path.Combine(Application.CommonAppDataPath, "..\\link.dat");
        public LinkControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.Load += (s, e) =>
            {
                initLink();
            };

            //new Common.ConsoleHelpler(textBox1);
        }


        public void ReleaseLink()
        {

            if (!File.Exists(_linkPath))
            {
                try
                {

                    //File.WriteAllText("link.dat", file, Encoding.UTF8);
                    Stream sm = Assembly.GetExecutingAssembly().GetManifestResourceStream("Client.link.txt");
                    byte[] bs = new byte[sm.Length];
                    sm.Read(bs, 0, (int)sm.Length);
                    sm.Close();
                    File.WriteAllBytes(_linkPath, bs);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("外链创建失败", "提示");

                }
            }
        }

        private void initLink()
        {
            lv_link.Items.Clear();
            Task.Run(() =>
            {
                if (File.Exists(_linkPath))
                {
                    var urls = File.ReadAllLines(_linkPath);
                    for (int i = 0; i < urls.Length; i++)
                    {
                        lv_link.Invoke(new MethodInvoker(() =>
                        {
                            var item = new ListViewItem(new[] { i.ToString(), urls[i] });
                            lv_link.Items.Add(item);
                        }));
                    }
                }
            });
        }

        private void tbx_urls_TextChanged(object sender, EventArgs e)
        {
            label5.Text = $"网站列表共{tbx_urls.Lines.Length}个";
        }

        private CancellationTokenSource cancellationTokenSource;

        private void btn_pub_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            if (!File.Exists(_linkPath))
            {
                MessageBox.Show("外链不存在请新增外链", "提示");
                return;
            }
            var links = File.ReadAllLines(_linkPath);
            if (links.Length < 1)
            {
                MessageBox.Show("外链数量较少请新增外链", "提示");
                return;
            }

            btn_pub.Text = "取消发布";
            tbx_urls.Enabled = false;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Token.Register(() =>
            {
                tbx_urls.Enabled = true;
                btn_pub.Text = "开始发布";
            });

            var urls = tbx_urls.Lines.Where(m => Regex.IsMatch(m, @"^[a-z0-9\.\-]+$")).ToArray();
            progressBar1.Value = 0;
            progressBar1.Maximum = links.Length;
            progressBar2.Value = 0;
            progressBar2.Maximum = urls.Length;
            lb_total.Text = lb_current.Text = "0/0";

            Task.Run(() =>
            {
                try
                {
                    var client = new RestSharp.RestClient();
                    client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.100 Safari/537.36";

                    Parallel.ForEach(urls, new ParallelOptions() { CancellationToken = cancellationTokenSource.Token, MaxDegreeOfParallelism = Environment.ProcessorCount - 1 }, (u, loopstae) =>
                          {

                              if (cancellationTokenSource.IsCancellationRequested)
                                  loopstae.Stop(); ;
                              progressBar2.Invoke(new MethodInvoker(() =>
                              {
                                  progressBar1.Value = 0;
                                  progressBar2.PerformStep();
                                  lb_total.Text = $"{progressBar2.Value}/{progressBar2.Maximum}";
                              }));

                              int a = 0;

                              Parallel.ForEach(links, new ParallelOptions() { CancellationToken = cancellationTokenSource.Token, MaxDegreeOfParallelism = Environment.ProcessorCount - 1 }, (m, s) =>
                                 {
                                     try
                                     {
                                         var request = new RestSharp.RestRequest();
                                         if (cancellationTokenSource.IsCancellationRequested)
                                         {
                                             s.Stop();
                                             return;
                                         }
                                         var status = "";
                                         if (m.Contains("{url}") && (m.StartsWith("http://") || m.StartsWith("https://")))
                                         {
                                             request.Timeout = 5000;
                                             request.Resource = m.Replace("{url}", u);
                                             var resp = client.Get(request);
                                             status = resp.StatusCode.ToString();
                                         }
                                         a += 1;
                                         progressBar1.Invoke(new MethodInvoker(() =>
                                         {
                                             progressBar1.Value = a;
                                             lb_current.Text = $"{a}/{progressBar1.Maximum}";
                                          // toolTip1.ToolTipTitle = $"{u} {status}";
                                          //toolTip1.Show(request.Resource, label8);
                                          label9.Text = $"{u} {status}\r\n{request.Resource}";
                                         }));

                                     }
                                     catch (Exception ex)
                                     {
                                         Console.WriteLine(ex);
                                     }
                                 });
                              Task.Delay(2000);

                          });

                    tbx_urls.Invoke(new MethodInvoker(() =>
                    {
                        tbx_urls.Enabled = true;
                        btn_pub.Text = "开始发布";
                        label9.Text = "当前任务完成";
                    }));
                }
                catch (Exception)
                {

                }
            }, cancellationTokenSource.Token);


        }

        private void btn_add_Click(object sender, EventArgs e)
        {
            var links = tbx_outlink.Lines;
            int add = 0;
            var sb = new StringBuilder();
            foreach (var item in links)
            {
                if ((item.StartsWith("http://") || item.StartsWith("https://")) && item.Contains("{url}"))
                {
                    sb.AppendLine(item);
                    add += 1;
                }
            }
            if (File.Exists(_linkPath))
            {
                File.AppendAllText(_linkPath, sb.ToString());
            }

            MessageBox.Show($"新增有效外链{add}个,重新启动软件加载最新外链", "提示", MessageBoxButtons.OK);
            //  lv_link.Items.Clear();
            // initLink();


        }

        private void btn_reLink_Click(object sender, EventArgs e)
        {
            try
            {
                File.Delete(_linkPath);
                ReleaseLink();
                MessageBox.Show("操作成功,重新启动软件加载最新外链", "提示");

            }
            catch (Exception)
            {
                MessageBox.Show("重置链接失败", "提示");

            }
        }

        private void btn_clearLink_Click(object sender, EventArgs e)
        {
            if (File.Exists(_linkPath))
            {
                try
                {
                    File.WriteAllText(_linkPath, "");
                    MessageBox.Show("操作成功", "提示");
                    lv_link.Items.Clear();
                }
                catch (Exception)
                {
                    MessageBox.Show("清空失败", "提示");
                }
            }
        }
    }
}
