using Heng;
using Heng.TransferFile;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
namespace TOEC_Index_Test
{
    public partial class frm_Main_IndexTest : Form
    {
        #region 初始化
        //0.0.0.0表示监听本机所有IP
        private static IPAddress myip = new IPAddress(new byte[] { 0, 0, 0, 0 });
        private static int myProt = 8004;
        private static Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//接受升级程序的 TCP

        private static List<LineInfo> LineList = new List<LineInfo>();

        public frm_Main_IndexTest()
        {
            InitializeComponent();
        }
        private void frm_Main_IndexTest_Load(object sender, EventArgs e)
        {
            #region 界面初始化
            ni.Visible = true;
            ni.Text = "TOEC Index Test【指标测试】";
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();

            #endregion
            InitLine();
            InitSocket();
            Thread myThread_ListenServer = new Thread(ListenClientConnect);
            myThread_ListenServer.IsBackground = true;
            myThread_ListenServer.Start();
        }

        private void InitSocket()
        {
            //与升级服务器通信
            s.Bind(new IPEndPoint(myip, myProt));  //绑定IP地址#端口
            s.Listen(1);    //设定最多10个排队连接请求
        }

        private void InitLine()
        {
            DataTable dt = DataHelper.ExecuteMySqlDataTable("SELECT l.LineID,l.LineCode FROM line l");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                LineList.Add(new LineInfo(Convert.ToInt16(dt.Rows[i]["LineID"]), Convert.ToString(dt.Rows[i]["LineCode"])));
            }
        }

        #endregion

        #region 控件事件
        /// <summary>
        /// 开机自启设置函数
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        private static bool SetAutoStart(bool term)
        {
            try
            {
                if (term) //设置开机自启动  
                {
                    string path = Application.ExecutablePath;
                    RegistryKey rk = Registry.LocalMachine;
                    RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                    rk2.SetValue("ITShutdown", path);
                    rk2.Close();
                    rk.Close();
                }
                else //取消开机自启动  
                {
                    string path = Application.ExecutablePath;
                    RegistryKey rk = Registry.LocalMachine;
                    RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                    rk2.DeleteValue("ITShutdown", false);
                    rk2.Close();
                    rk.Close();
                }
                Common.Default.AutoStart = term;
                Common.Default.Save();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        /// <summary>
        /// 开机自启按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tms_AutoStart_Click(object sender, EventArgs e)
        {
            if (tms_AutoStart.Checked == true)
            {
                MessageBox.Show("取消开机自启动", "提示【需要允许修改注册表】");
                tms_AutoStart.Checked = false;
                SetAutoStart(false);
            }
            else
            {
                MessageBox.Show("设置开机自启动", "提示【需要允许修改注册表】");
                tms_AutoStart.Checked = true;
                SetAutoStart(true);
            }
        }
        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsm_Close_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要关闭TOEC指标测试程序", "谨慎", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) ==
        DialogResult.OK)
            {
                SocketExit(ref s);
                Application.Exit();
            }
        }
        /// <summary>
        /// 菜单打开刷新显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cms_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Common.Default.AutoStart == true)
            {
                tms_AutoStart.Checked = true;
            }
            else
            {
                tms_AutoStart.Checked = false;
            }
        }
        #endregion

        #region 停止全局套接字
        public static void SocketExit(ref Socket skt)
        {
            if (skt.Connected)
            {
                skt.Shutdown(SocketShutdown.Both);
            }
            skt.Close();
            skt = null;
        }

        #endregion

        #region Socket#监听接收测试指令
        /// <summary>
        /// 监听
        /// </summary>
        private static void ListenClientConnect()
        {
            while (true)
            {
                if (s != null)
                {
                    try
                    {
                        Socket clientSocket = s.Accept();
                        //设置缓冲区为2M
                        clientSocket.ReceiveBufferSize = 1024 * 1024 * 2;
                        clientSocket.SendBufferSize = 1024 * 1024 * 2;
                        //接收超时设置为10秒钟
                        clientSocket.ReceiveTimeout = 1000 * 10;
                        clientSocket.NoDelay = true;
                        Thread receiveThread = new Thread(Excute);
                        receiveThread.IsBackground = true;
                        receiveThread.Start(clientSocket);
                    }
                    catch (Exception ex)
                    {
                        if (s != null)
                            continue;
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region 独立线程#执行指标测试指令
        /// <summary>
        /// 执行指标测试指令
        /// </summary>
        /// <param name="clientSocket"></param>
        private static void Excute(object clientSocket)
        {
            Socket client = clientSocket as Socket;
            byte[] ID_buffer = new byte[40];
            try
            {
                UpdateLog.ClearLog();
                Thread.Sleep(100);
                while (true)
                {
                    if (client.Available > 0)
                    {
                        UpdateLog.Log("➤ 查询参数");
                        byte[] res = TransferFiles.ReceiveVarData(client);
                        string Str_NameAndMission = Encoding.UTF8.GetString(res);
                        string stationname = "", Commend = "", st = "", ed = "";
                        if (Str_NameAndMission != "GetConfig" && Str_NameAndMission != "SetConfig")
                        {
                            string[] array = Str_NameAndMission.Split("#".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            UpdateLog.Log("站名#" + array[0]);
                            UpdateLog.Log("指令#" + array[1]);
                            stationname = array[0];
                            Commend = array[1];
                            st = array[2];
                            ed = array[3];
                            UpdateLog.Log("时间#" + array[2].Replace("#", " ") + " 至 " + array[3].Replace("#", " "));
                        }
                        else
                        {
                            //若是【获去配置】或【修改配置】则只包含命令
                            Commend = Str_NameAndMission;
                        }
                        switch (Commend)
                        {
                            #region 基础数据
                            case "BaseCount":
                                try
                                {
                                    try
                                    {
                                        UpdateLog.Log("➤ 列车总数");
                                        int total = 0;
                                        foreach (LineInfo li in LineList)
                                        {
                                            int tmp = IndexTest.GetTrainNum(st, ed, li.lid.ToString());
                                            total += tmp;
                                            UpdateLog.Log("线路" + li.lid + "#" + tmp);
                                        }
                                        UpdateLog.Log("列车总数#" + (total));
                                        UpdateLog.Log("数据来源#Train表");
                                    }
                                    catch (Exception ex) { UpdateLog.Error("列车总数#" + ex.Message); }
                                    try
                                    {
                                        UpdateLog.Log("➤ 各类列车数量");
                                        foreach (LineInfo li in LineList)
                                        {
                                            DataTable tmp = IndexTest.GetTyainNumByTrainType(st, ed, li.lid.ToString());
                                            UpdateLog.Log("线路" + li.lid);
                                            if (tmp != null && tmp.Rows.Count > 0)
                                            {
                                                for (int i = 0; i < tmp.Rows.Count; i++)
                                                {
                                                    UpdateLog.Log(tmp.Rows[i][0].ToString().Replace("NoTrainType", "列车类型为空") + "#" + tmp.Rows[i][1].ToString());
                                                }
                                            }
                                        }
                                        UpdateLog.Log("数据来源#Train表");
                                    }
                                    catch (Exception ex) { UpdateLog.Error("各类列车数量#" + ex.Message); }
                                    try
                                    {
                                        UpdateLog.Log("➤ 车厢总数");
                                        int total = 0;
                                        foreach (LineInfo li in LineList)
                                        {
                                            int tmp = IndexTest.GetCarNumByPeriod(st, ed, li.lid.ToString());
                                            UpdateLog.Log("线路" + li.lid + "#" + tmp);
                                            total += tmp;
                                        }
                                        UpdateLog.Log("车厢总数#" + total);
                                        UpdateLog.Log("数据来源#TrainDetail表");
                                    }
                                    catch (Exception ex) { UpdateLog.Error("车厢总数#" + ex.Message); }
                                    try
                                    {
                                        UpdateLog.Log("➤ 各类车厢数量");
                                        foreach (LineInfo li in LineList)
                                        {
                                            UpdateLog.Log("线路" + li.lid);
                                            DataTable tmp = IndexTest.GetCarNumByCarKind(st, ed, li.lid.ToString());
                                            if (tmp != null && tmp.Rows.Count > 0)
                                            {
                                                for (int i = 0; i < tmp.Rows.Count; i++)
                                                {
                                                    UpdateLog.Log(tmp.Rows[i]["vehicletype"].ToString() + "#" + tmp.Rows[i]["KindNum"].ToString());
                                                }
                                            }
                                        }
                                        UpdateLog.Log("数据来源#TrainDetail表");
                                    }
                                    catch (Exception ex) { UpdateLog.Error("各类车厢数量#" + ex.Message); }
                                    try
                                    {
                                        UpdateLog.Log("➤ 图像总数");
                                        int total = 0;
                                        foreach (LineInfo li in LineList)
                                        {
                                            int tmp = IndexTest.GetFileNum(st, ed, Common.Default.Path_Pic + li.FolderName + "\\", ".jpg");
                                            total += tmp;
                                            UpdateLog.Log("线路" + li.lid + "#" + tmp);
                                        }
                                        UpdateLog.Log("图像总数#" + total);
                                        UpdateLog.Log("数据来源#.jpg文件个数");
                                    }
                                    catch (Exception ex) { UpdateLog.Error("图像总数#" + ex.Message); }
                                    try
                                    {
                                        UpdateLog.Log("➤ 走行部图像总数");
                                        int total = 0;
                                        foreach (LineInfo li in LineList)
                                        {
                                            int tmp = IndexTest.GetFileNum(st, ed, Common.Default.Path_ZXPic + li.FolderName + "\\", ".jpg");
                                            total += tmp;
                                            UpdateLog.Log("线路" + li.lid + "#" + tmp);
                                        }
                                        UpdateLog.Log("走行部图像总数#" + total);
                                        UpdateLog.Log("数据来源#.jpg文件个数");
                                    }
                                    catch (Exception ex) { UpdateLog.Error("图像总数#" + ex.Message); }
                                    try
                                    {
                                        UpdateLog.Log("➤ 视频总数");
                                        int total = 0;
                                        foreach (LineInfo li in LineList)
                                        {
                                            int tmp = IndexTest.GetFileNum(st, ed, Common.Default.Path_Video + li.FolderName + "\\", ".mp4");
                                            total += tmp;
                                            UpdateLog.Log("线路" + li.lid + "#" + tmp);
                                        }
                                        UpdateLog.Log("视频总数#" + total);
                                        UpdateLog.Log("数据来源#.mp4文件个数");
                                    }
                                    catch (Exception ex) { UpdateLog.Error("视频总数#" + ex.Message); }
                                    try
                                    {
                                        UpdateLog.Log("➤ 声音总数");
                                        int total = 0;
                                        foreach (LineInfo li in LineList)
                                        {
                                            int tmp = IndexTest.GetFileNum(st, ed, Common.Default.Path_Voice + li.FolderName + "\\", ".aac");
                                            total += tmp;
                                            UpdateLog.Log("线路" + li.lid + "#" + tmp);
                                        }
                                        UpdateLog.Log("声音总数#" + total);
                                        UpdateLog.Log("数据来源#.aac文件个数");
                                    }
                                    catch (Exception ex) { UpdateLog.Error("声音总数#" + ex.Message); }
                                    try
                                    {
                                        UpdateLog.Log("➤ 所有预警总数");
                                        decimal total = 0;
                                        foreach (LineInfo li in LineList)
                                        {
                                            decimal tmp = IndexTest.GetAlarmNum(st, ed, li.lid.ToString(), null, null, null);
                                            UpdateLog.Log("线路" + li.lid + "#" + tmp);
                                            total += tmp;
                                        }
                                        UpdateLog.Log("预警总数#" + total);
                                    }
                                    catch (Exception ex) { UpdateLog.Error("预警总数#" + ex.Message); }
                                    try
                                    {
                                        UpdateLog.Log("➤ 各类别预警数");
                                        int sum_Alarm = 0, sum_Fail = 0;
                                        foreach (LineInfo li in LineList)
                                        {
                                            UpdateLog.Log("线路" + li.lid + "");
                                            UpdateLog.Log("===正常报警:");
                                            DataTable alarm = IndexTest.GetAlarmNum(st, ed, li.lid.ToString(), true);
                                            if (alarm != null && alarm.Rows.Count > 0)
                                            {
                                                for (int i = 0; i < alarm.Rows.Count; i++)
                                                {
                                                    UpdateLog.Log(alarm.Rows[i]["ProblemType"].ToString() + "#" + alarm.Rows[i]["AlarmNum"].ToString());
                                                    sum_Alarm += int.Parse(alarm.Rows[i]["AlarmNum"].ToString());
                                                }
                                            }
                                            UpdateLog.Log("===检测失败:");
                                            DataTable fail = IndexTest.GetAlarmNum(st, ed, li.lid.ToString(), false);
                                            if (fail != null && fail.Rows.Count > 0)
                                            {
                                                for (int i = 0; i < fail.Rows.Count; i++)
                                                {
                                                    UpdateLog.Log(fail.Rows[i]["ProblemType"].ToString() + "#" + fail.Rows[i]["AlarmNum"].ToString());
                                                    sum_Fail += int.Parse(fail.Rows[i]["AlarmNum"].ToString());
                                                }
                                            }
                                        }
                                        UpdateLog.Log("【报警总数】#" + sum_Alarm);
                                        UpdateLog.Log("【检测失败】#" + sum_Fail);
                                    }
                                    catch (Exception ex) { UpdateLog.Error("各类别预警数" + ex.Message); }
                                }
                                catch (Exception ex)
                                {
                                    UpdateLog.Error("基础数据异常#" + ex.Message);
                                }
                                break;
                            #endregion

                            #region 丢文件
                            case "MissFiles":
                                try
                                {
                                    UpdateLog.Log("➤ 丢文件检测");
                                    foreach (LineInfo li in LineList)
                                    {
                                        UpdateLog.Log("=====【线路" + li.lid + "】=====");
                                        int Count_Dir;
                                        List<DirectoryInfo> list_MissingIndex;
                                        List<FileInfo> list_MissingPics;
                                        IndexTest.CountMissingPics(st, ed, Common.Default.Path_Pic + li.FolderName + "\\", out Count_Dir, out list_MissingIndex, out list_MissingPics);
                                        UpdateLog.Log("共检测了文件夹#" + Count_Dir);
                                        UpdateLog.Log("共丢失Index#" + list_MissingIndex.Count);
                                        UpdateLog.Log("共丢失图片#" + list_MissingPics.Count);
                                        if (list_MissingIndex.Count > 0)
                                        {
                                            UpdateLog.Log("===Index丢失明细===");
                                            foreach (DirectoryInfo dir in list_MissingIndex)
                                            {
                                                UpdateLog.Log("Index丢失#" + dir.FullName);
                                            }
                                        }
                                        if (list_MissingPics.Count > 0)
                                        {
                                            UpdateLog.Log("===图片丢失明细===");
                                            foreach (FileInfo fi in list_MissingPics)
                                            {
                                                UpdateLog.Log("图片丢失#" + fi.FullName);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex) { UpdateLog.Error(ex.Message); }
                                try
                                {
                                    UpdateLog.Log("➤ 丢文件检测(走形)");
                                    foreach (LineInfo li in LineList)
                                    {
                                        UpdateLog.Log("=====【线路" + li.lid + "】=====");
                                        int Count_Dir;
                                        List<DirectoryInfo> list_MissingIndex;
                                        List<FileInfo> list_MissingPics;
                                        IndexTest.CountMissingPics(st, ed, Common.Default.Path_ZXPic + li.FolderName + "\\", out Count_Dir, out list_MissingIndex, out list_MissingPics);
                                        UpdateLog.Log("共检测了文件夹#" + Count_Dir);
                                        UpdateLog.Log("共丢失Index#" + list_MissingIndex.Count);
                                        UpdateLog.Log("共丢失图片#" + list_MissingPics.Count);
                                        if (list_MissingIndex.Count > 0)
                                        {
                                            UpdateLog.Log("===Index丢失明细===");
                                            foreach (DirectoryInfo dir in list_MissingIndex)
                                            {
                                                UpdateLog.Log("Index丢失#" + dir.FullName);
                                            }
                                        }
                                        if (list_MissingPics.Count > 0)
                                        {
                                            UpdateLog.Log("===图片丢失明细===");
                                            foreach (FileInfo fi in list_MissingPics)
                                            {
                                                UpdateLog.Log("图片丢失#" + fi.FullName);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex) { UpdateLog.Error(ex.Message); }
                                break;
                            #endregion

                            #region 入库率
                            case "DBIndex":
                                try
                                {
                                    UpdateLog.Log("➤ 列车实时入库率");
                                    double[] all = IndexTest.GetTrainInfoImportDataByFlagRatio(st, ed, "SOCKET", null);
                                    foreach (LineInfo li in LineList)
                                    {
                                        double[] tmp = IndexTest.GetTrainInfoImportDataByFlagRatio(st, ed, "SOCKET", li.lid.ToString());
                                        UpdateLog.Log("线路" + li.lid + "入库率#" + tmp[0] + "/" + tmp[1] + " * 100 = " + (tmp[1] != 0 ? Math.Round(tmp[0] / tmp[1], 4, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                    }
                                    UpdateLog.Log("实时入库率（Socket）#" + all[0] + "/" + all[1] + " * 100 = " + (all[1] != 0 ? Math.Round(all[0] / all[1], 4, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                    UpdateLog.Log("公式#实时入库列车数/过车总数*100%");
                                }
                                catch (Exception ex) { UpdateLog.Error("热轮总数#" + ex.Message); }
                                try
                                {
                                    UpdateLog.Log("➤ 列车补入库率");
                                    double[] all = IndexTest.GetTrainInfoImportDataByFlagRatio(st, ed, "INDEX", null);
                                    foreach (LineInfo li in LineList)
                                    {
                                        double[] tmp = IndexTest.GetTrainInfoImportDataByFlagRatio(st, ed, "INDEX", li.lid.ToString());
                                        UpdateLog.Log("线路" + li.lid + "入库率#" + tmp[0] + "/" + tmp[1] + " * 100 = " + (tmp[1] != 0 ? Math.Round(tmp[0] / tmp[1], 4, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                    }
                                    UpdateLog.Log("补入库率（Index）#" + all[0] + "/" + all[1] + " * 100 = " + (all[1] != 0 ? Math.Round(all[0] / all[1], 4, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                    UpdateLog.Log("公式#Index补入库列车数/过车总数*100%");
                                }
                                catch (Exception ex) { UpdateLog.Error("入库率#" + ex.Message); }
                                break;
                            #endregion

                            #region 热轮指标
                            case "HotwheelIndex":
                                try
                                {
                                    UpdateLog.Log("➤ 热轮总数");
                                    int total = 0;
                                    foreach (LineInfo li in LineList)
                                    {
                                        int tmp = IndexTest.GetFileNum(st, ed, Common.Default.Path_Hotwheel + li.FolderName + "\\", ".HotWheel");
                                        total += tmp;
                                        UpdateLog.Log("线路" + li.lid + "#" + tmp);
                                    }
                                    UpdateLog.Log("热轮总数#" + total);
                                    UpdateLog.Log("数据来源#.HotWheel文件个数");
                                }
                                catch (Exception ex) { UpdateLog.Error("热轮总数#" + ex.Message); }
                                try
                                {
                                    UpdateLog.Log("➤ 热轮接入率");
                                    double[] total = new double[2];
                                    foreach (LineInfo li in LineList)
                                    {
                                        try
                                        {
                                            double[] tmp = IndexTest.GetHotWheelFileStatisicsRatio(st, ed, li.FolderName);
                                            UpdateLog.Log("线路" + li.lid + "接入率#" + tmp[0] + "/" + tmp[1] + " * 100 = " + (tmp[1] != 0 ? Math.Round(tmp[0] / tmp[1], 2, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                            total[0] += tmp[0]; total[1] += tmp[1];
                                        }
                                        catch (Exception e) { UpdateLog.Error(e.Message); }
                                    }
                                    UpdateLog.Log("热轮接入率#" + total[0] + "/" + total[1] + " * 100 = " + (total[0] != 0 ? Math.Round((total[0]) / (total[1]), 2, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                    UpdateLog.Log("公式#接入热轮文件数/过车总数*100%");
                                }
                                catch (Exception ex) { UpdateLog.Error("热轮接入率#" + ex.Message); }
                                try
                                {
                                    UpdateLog.Log("➤ 热轮匹配率");
                                    double[] total = new double[2];
                                    foreach (LineInfo li in LineList)
                                    {
                                        try
                                        {
                                            double[] tmp = IndexTest.GetHotWheelFileMatchRatio(st, ed, li.FolderName);
                                            UpdateLog.Log("线路" + li.lid + "匹配率#" + tmp[0] + "/" + tmp[1] + " * 100 = " + (tmp[1] != 0 ? Math.Round(tmp[0] / tmp[1], 2, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                            total[0] += tmp[0]; total[1] += tmp[1];
                                        }
                                        catch (Exception e) { UpdateLog.Error(e.Message); }
                                    }
                                    UpdateLog.Log("热轮匹配率#" + total[0] + "/" + total[1] + " * 100 = " + (total[0] != 0 ? Math.Round((total[0]) / (total[1]), 2, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                    UpdateLog.Log("公式#匹配成功的热轮文件数/接入的热轮文件数*100%");
                                }
                                catch (Exception ex) { UpdateLog.Error("热轮匹配率#" + ex.Message); }
                                break;
                            #endregion

                            #region 确报指标
                            case "QBIndex":
                                try
                                {
                                    UpdateLog.Log("➤ 确报总数");
                                    int total = 0;
                                    foreach (LineInfo li in LineList)
                                    {
                                        DataTable tmpdt = IndexTest.GetAllQBNumByPeriod(st, ed, li.lid.ToString());
                                        int tmp = 0;
                                        int.TryParse(tmpdt.Rows[0][0]?.ToString(), out tmp);
                                        total += tmp;
                                        UpdateLog.Log("线路" + li.lid + "#" + tmp);
                                    }
                                    UpdateLog.Log("确报总数#" + total);
                                }
                                catch (Exception ex) { UpdateLog.Error("确报总数#" + ex.Message); }
                                try
                                {
                                    UpdateLog.Log("➤ 确报接入率");
                                    foreach (LineInfo li in LineList)
                                    {
                                        double[] tmp = IndexTest.GetQBRatio(st, ed, li.lid.ToString());
                                        UpdateLog.Log("线路" + li.lid + "#" + tmp[0] + "/" + tmp[1] + " * 100 = " + (tmp[1] != 0 ? Math.Round(tmp[0] / tmp[1], 2, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                    }
                                    double[] all = IndexTest.GetQBRatio(st, ed, null);
                                    UpdateLog.Log("确报总接入率#" + all[0] + "/" + all[1] + " * 100 = " + (all[1] != 0 ? Math.Round(all[0] / all[1], 2, MidpointRounding.AwayFromZero) * 100 : 0) + "%");
                                    UpdateLog.Log("公式#确报主表中的数值/非客过车总数*100%");
                                }
                                catch (Exception ex) { UpdateLog.Error("确报接入率#" + ex.Message); }
                                try
                                {
                                    UpdateLog.Log("➤ 确报车号利用率");

                                    foreach (LineInfo li in LineList)
                                    {
                                        double tmp = IndexTest.GetQBUseRatio(st, ed, li.lid.ToString());
                                        UpdateLog.Log("线路" + li.lid + "利用率#" + tmp + "%");
                                    }
                                    double all = IndexTest.GetQBUseRatio(st, ed, null);
                                    UpdateLog.Log("确报车号利用率#" + all + "%");
                                    UpdateLog.Log("公式#确报弥补的车号数/车号识别短少的车号数*100%");
                                }
                                catch (Exception ex) { UpdateLog.Error("确报车号利用率#" + ex.Message); }
                                break;
                            #endregion

                            #region 车号识别率
                            case "CheckTrainNum":
                                try
                                {
                                    UpdateLog.Log("➤ 车号识别率");
                                    foreach (LineInfo li in LineList)
                                    {
                                        double tmp = IndexTest.GetVisionCarNoRatio(st, ed, li.lid.ToString());
                                        UpdateLog.Log("线路" + li.lid + "识别率#" + tmp + "%");
                                    }
                                    double all = IndexTest.GetVisionCarNoRatio(st, ed, null);
                                    UpdateLog.Log("车号识别率#" + all + "%");
                                    UpdateLog.Log("公式#车号正常车辆数/所有过车车辆数*100% [依据数据库计算]");
                                }
                                catch (Exception ex) { UpdateLog.Error(ex.Message); }
                                break;
                            #endregion

                            #region 丢轴
                            case "MissAxle":
                                try
                                {
                                    UpdateLog.Log("➤ 丢轴");
                                    foreach (LineInfo li in LineList)
                                    {
                                        double tmp = IndexTest.GetUnusualAlexTrainRatio(st, ed, li.lid.ToString());
                                        UpdateLog.Log("线路" + li.lid + "丢轴率#" + tmp + "%");
                                    }
                                    double all = IndexTest.GetUnusualAlexTrainRatio(st, ed, null);
                                    UpdateLog.Log("总丢轴率#" + all + "%");
                                    UpdateLog.Log("公式#丟轴、多轴列数/实际过车列数*100%");
                                    UpdateLog.Log("*注#以算法返回的-11为基准，业务上发生的丢轴、多轴均视为丢轴");
                                }
                                catch (Exception ex) { UpdateLog.Error(ex.Message); }
                                break;
                            #endregion

                            #region 音视频生成率
                            case "GenerateRate_Audio_Video":
                                try
                                {
                                    UpdateLog.Log("➤ 音频生成率 和 视频生成率");
                                    decimal total_audio = 0, total_video = 0, total_video2 = 0, total_Dir = 0;
                                    foreach (LineInfo li in LineList)
                                    {
                                        UpdateLog.Log("【线路" + li.lid + "】");
                                        List<string> MissingAudio, MissingVideo, MissingVideo2; int DirCount;
                                        IndexTest.GenerateRate_Audio_Video(st, ed, Common.Default.Path_Pic + li.FolderName, out MissingAudio, out MissingVideo, out MissingVideo2, out DirCount);
                                        UpdateLog.Log("检测文件夹数#" + DirCount);
                                        total_Dir += DirCount;
                                        decimal rate_audio = DirCount != 0 ? Math.Round((DirCount - MissingAudio.Count) / Convert.ToDecimal(DirCount), 4, MidpointRounding.AwayFromZero) * 100 : 0;
                                        total_audio += DirCount - MissingAudio.Count;
                                        UpdateLog.Log("音频生成率#" + rate_audio);
                                        UpdateLog.Log("公式#音频生成率 = 生成主音频文件数/列车文件夹数*100%\r\n");
                                        decimal rate_video = DirCount != 0 ? Math.Round((DirCount - MissingVideo.Count) / Convert.ToDecimal(DirCount), 4, MidpointRounding.AwayFromZero) * 100 : 0;
                                        total_video += DirCount - MissingVideo.Count;
                                        UpdateLog.Log("视频生成率#" + rate_video);
                                        UpdateLog.Log("公式#视频生成率 = 生成主视频文件数/列车文件夹数*100%\r\n");
                                        decimal rate_video2 = DirCount != 0 ? Math.Round((DirCount - MissingVideo2.Count) / Convert.ToDecimal(DirCount), 4, MidpointRounding.AwayFromZero) * 100 : 0;
                                        total_video2 += DirCount - MissingVideo2.Count;
                                        UpdateLog.Log("视频生成率(辅)#" + rate_video2);
                                        UpdateLog.Log("公式#视频生成率 = 生成主视频文件数/列车文件夹数*100%\r\n");
                                        UpdateLog.Log("丢失音频明细");
                                        foreach (string i in MissingAudio) { UpdateLog.Log(i); }
                                        UpdateLog.Log("丢失视频明细");
                                        foreach (string i in MissingVideo) { UpdateLog.Log(i); }
                                        UpdateLog.Log("丢失视频明细(辅)");
                                        foreach (string i in MissingVideo2) { UpdateLog.Log(i); }
                                    }
                                    UpdateLog.Log("音频总生成率#" + Math.Round(total_audio / total_Dir, 4, MidpointRounding.AwayFromZero) * 100);
                                    UpdateLog.Log("视频总生成率#" + Math.Round(total_video / total_Dir, 4, MidpointRounding.AwayFromZero) * 100);
                                    UpdateLog.Log("视频总生成率(辅)#" + Math.Round(total_video / total_Dir, 4, MidpointRounding.AwayFromZero) * 100);
                                }
                                catch (Exception ex) { UpdateLog.Error("音频生成率 和 视频生成率异常#" + ex.Message); }
                                break;
                            #endregion

                            #region 图像检测率
                            case "CallRate_Image":
                                try
                                {
                                    UpdateLog.Log("➤ 图像检测率");
                                    foreach (LineInfo li in LineList)
                                    {
                                        double[] tmp = IndexTest.GetImageCheckRatio(st, ed, li.lid.ToString());
                                        UpdateLog.Log("线路" + li.lid + "检测率#" + tmp[0] + "/" + tmp[1] + "=" + tmp[2] + "%");
                                    }
                                    double[] all_Rate = IndexTest.GetImageCheckRatio(st, ed, null);
                                    UpdateLog.Log("图像检测率#" + all_Rate[0] + "/" + all_Rate[1] + "=" + all_Rate[2] + "%");
                                    UpdateLog.Log("公式#主服务成功调用算法的个数/所有过车图片数*100%");
                                }
                                catch (Exception ex) { UpdateLog.Error("图像检测率#" + ex.Message); }
                                break;
                            #endregion

                            #region 图像算法调用及时性
                            case "Timely_Image":
                                try
                                {
                                    UpdateLog.Log(IndexTest.GetTimely(st, ed));
                                }
                                catch (Exception ex) { UpdateLog.Error("图像算法调用的及时性异常#" + ex.Message); }
                                break;
                            #endregion

                            #region 图像报警准确率
                            case "AccuracyRate_Image":
                                try
                                {
                                    foreach (LineInfo li in LineList)
                                    {
                                        UpdateLog.Log("【线路" + li.lid + "】");
                                        decimal true_pic = IndexTest.GetAlarmNum(st, ed, li.lid.ToString(), true, true, "pic");
                                        decimal false_pic = IndexTest.GetAlarmNum(st, ed, li.lid.ToString(), false, true, "pic");
                                        decimal all_pic = IndexTest.GetAlarmNum(st, ed, li.lid.ToString(), null, true, "pic");
                                        UpdateLog.Log("➤ 真实报警率#" + true_pic + "/" + all_pic + "=" + (all_pic != 0 ? (Math.Round(true_pic / all_pic, 4) * 100) : 0) + "%");
                                        UpdateLog.Log("➤ 误报率#" + false_pic + "/" + all_pic + "=" + (all_pic != 0 ? (Math.Round(false_pic / all_pic, 4) * 100) : 0) + "%");
                                        UpdateLog.Log("*注#基于人工处理后数据才有意义");
                                    }
                                }
                                catch (Exception ex) { UpdateLog.Error("图像报警准确率#" + ex.Message); }
                                break;
                            #endregion

                            #region 视频检测率
                            case "CheckRate_Video":
                                try { UpdateLog.Log("此功能暂不实现"); }
                                catch (Exception ex) { UpdateLog.Error("视频检测率#" + ex.Message); }
                                break;
                            #endregion

                            #region 视频算法调用的及时性
                            case "Timely_Video":
                                try { UpdateLog.Log("此功能暂不实现"); }
                                catch (Exception ex) { UpdateLog.Error("视频算法调用的及时性#" + ex.Message); }
                                break;
                            #endregion

                            #region 9个检测项独立统计【报喜不报忧模式】
                            case "Custom4Client":
                                try
                                {
                                    UpdateLog.Log("➤ 各类检测项统计");
                                    int total_Alarm = 0;
                                    foreach (LineInfo li in LineList)
                                    {
                                        UpdateLog.Log("【线路" + li.lid + "】");
                                        DataTable tmp = IndexTest.GetAlarmNum(st, ed, li.lid.ToString(), true);
                                        if (tmp != null && tmp.Rows.Count > 0) {
                                            for (int i = 0; i < tmp.Rows.Count; i++)
                                            {
                                                UpdateLog.Log(tmp.Rows[i]["ProblemType"].ToString() + "#" + tmp.Rows[i]["AlarmNum"].ToString());
                                                total_Alarm += int.Parse(tmp.Rows[i]["AlarmNum"]?.ToString());
                                            }
                                        }
                                    }
                                    UpdateLog.Log("报警总数#" + total_Alarm);
                                }
                                catch (Exception ex) { UpdateLog.Error("各类别预警数" + ex.Message); }
                                break;
                            #endregion

                            default: break;
                        }
                        TransferFiles.SendVarData(client, Encoding.UTF8.GetBytes(UpdateLog.OutputLog_IntoString(false)));
                        break;
                    }
                    else
                    {
                        Thread.Sleep(0);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                ///当网络异常中断#拔网线等触发，接受为0字节，但是并不认为是异常，因此需要添加心跳机制；
                UpdateLog.Error("接受失败#" + ex.Message);
            }
            finally
            {
                //输出日志文件
                UpdateLog.OutputLog(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TOEC Index Test"), "TOEC Index Test");
                //关闭套接字
                client.Close();
            }
        }
        #endregion
    }
}
