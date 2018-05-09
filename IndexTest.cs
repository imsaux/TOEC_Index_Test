using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;

namespace TOEC_Index_Test
{
    /// <summary>
    /// 版权所有:天津光电高斯通信工程有限公司
    /// 内容摘要:此类主要是为TOEC(天津光电)的TRAD（火车接发列车）项目的指标考核体系提供数据源的类文件。
    /// 创建日期:2017.04.10
    /// 版    本:V1.0.0.0
    /// 作    者:王俊杰
    /// 
    /// 修改日期:2017.05.26
    /// 版    本:V1.0.0.1
    /// 作    者:王俊杰
    /// 备    注:修改了序号26，及序号37
    /// </summary>

    public class IndexTest
    {
        #region /// 序号6 获得轴异常的列车比率（丢轴或者多轴）
        /// <summary>
        /// 序号6 获得轴异常的列车比率（丢轴或者多轴）
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">过车线路标识</param>
        /// <returns>异常轴列车的入库比率</returns>
        public static double GetUnusualAlexTrainRatio(string str_bgnTime, string str_endTime, string lineID)
        {
            double ratio = 0;
            try
            {
                string sql = "SELECT Train_ID,Train_ComeDate,Line_ID from train where Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' ;";
                if (!string.IsNullOrEmpty(lineID))
                {
                    sql = "SELECT Train_ID,Train_ComeDate,Line_ID from train where Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' and Line_ID='" + lineID + "' ;";
                }
                int ImportTrainNum = 0;

                #region 获得数据库中时间段内的所有列车（组织分母）
                DataTable dt = DataHelper.ExecuteMySqlDataTable(sql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    ImportTrainNum = dt.Rows.Count;
                }
                #endregion

                string sqlUnusualAlex = "SELECT DISTINCT Train_ID FROM alarmdetail where AlgResult='-11' "
                                      + "and Train_ID in (SELECT Train_ID from train where  Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' ";
                if (!string.IsNullOrEmpty(lineID))
                {
                    sqlUnusualAlex = sqlUnusualAlex + "and Line_ID='" + lineID + "'";

                }
                sqlUnusualAlex = sqlUnusualAlex + ");";
                int ImportUnusualAlex = 0;

                DataTable dtUnusualAlex = DataHelper.ExecuteMySqlDataTable(sqlUnusualAlex);
                if (dtUnusualAlex != null && dtUnusualAlex.Rows.Count > 0)
                {
                    ImportUnusualAlex = dtUnusualAlex.Rows.Count;
                }

                if (ImportTrainNum != 0)
                {
                    ratio = Math.Round(double.Parse(ImportUnusualAlex.ToString()) / ImportTrainNum, 3) * 100;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return ratio;
        }
        #endregion

        #region ///序号7 车号识别率
        /// <summary>
        ///序号7 车号识别率
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">过车线路标识</param>
        /// <returns>车厢车号识别率</returns>
        public static double GetVisionCarNoRatio(string str_bgnTime, string str_endTime, string lineID)
        {
            double ratio = 0;
            try
            {
                StringBuilder strAll = new StringBuilder();
                strAll.Append("SELECT td.TrainDetail_No from ");
                strAll.Append("(SELECT * from train where  Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strAll.Append("and Line_ID='" + lineID + "'");
                }
                strAll.Append(") as t LEFT JOIN traindetail AS td ");
                strAll.Append("on t.Train_ID=td.Train_ID ; ");

                int ImportAllCar = 0;

                DataTable dtAll = DataHelper.ExecuteMySqlDataTable(strAll.ToString());
                if (dtAll != null && dtAll.Rows.Count > 0)
                {
                    ImportAllCar = dtAll.Rows.Count;
                }

                int ImportVisionCarNo = 0;
                StringBuilder strVisionCarNo = new StringBuilder();

                strVisionCarNo.Append("SELECT td.TrainDetail_No from ");
                strVisionCarNo.Append("(SELECT * from train where  Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strVisionCarNo.Append("and Line_ID='" + lineID + "'");
                }
                strVisionCarNo.Append(") as t LEFT JOIN traindetail AS td ");
                strVisionCarNo.Append("on t.Train_ID=td.Train_ID  ");
                strVisionCarNo.Append("where td.TrainDetail_No<>'' and td.TrainDetail_No is not NULL;");

                DataTable dtVisionCarNo = DataHelper.ExecuteMySqlDataTable(strVisionCarNo.ToString());
                if (dtVisionCarNo != null && dtVisionCarNo.Rows.Count > 0)
                {
                    ImportVisionCarNo = dtVisionCarNo.Rows.Count;
                }

                if (ImportAllCar != 0)
                {
                    ratio = Math.Round(double.Parse(ImportVisionCarNo.ToString()) / ImportAllCar, 4, MidpointRounding.AwayFromZero) * 100;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return ratio;

        }
        #endregion

        #region  ///序号11 统计周期内过车图像的检测率
        /// <summary>
        ///序号11 统计周期内过车图像的检测率
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">列车线路</param>
        /// <returns>周期内的过车的统计结果集</returns>
        public static double[] GetImageCheckRatio(string str_bgnTime, string str_endTime, string lineID)
        {
            DataTable dt = null;
            try
            {
                StringBuilder strB = new StringBuilder();
                strB.Append(@"  SELECT SucCheckImageNum,
                                CASE WHEN StaggerBgnOrderNo<>0 AND StaggerEndOrderNo<>0 THEN
                                CASE WHEN OneCarCheckImagesNums=4 THEN
                                CASE WHEN StaggerBgnOrderNo=1 THEN AllCarNum * OneCarCheckImagesNums-(OneCarCheckImagesNums/2)*(StaggerEndOrderNo+1-StaggerBgnOrderNo)-1
                                ELSE AllCarNum * OneCarCheckImagesNums-(OneCarCheckImagesNums/2)*(StaggerEndOrderNo+1-StaggerBgnOrderNo)-2
                                END
                                ELSE AllCarNum * OneCarCheckImagesNums-(OneCarCheckImagesNums/2)*(StaggerEndOrderNo+1-StaggerBgnOrderNo)
                                END
                                ELSE CASE WHEN OneCarCheckImagesNums=4 THEN AllCarNum * OneCarCheckImagesNums-2
                                ELSE AllCarNum * OneCarCheckImagesNums
                                END
                                END AS ShouldCheckImageNum
                                FROM statistics_image");
                strB.Append(@"  WHERE TrainComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strB.Append(" AND LineID='" + lineID + "' ");
                }
                strB.Append(";");
                dt = DataHelper.ExecuteMySqlDataTable(strB.ToString());

                double res = 0, SucceedCheck = 0, ShouldCheck = 0;

                if (dt != null && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        SucceedCheck += double.Parse(dt.Rows[i]["SucCheckImageNum"].ToString());
                        ShouldCheck += double.Parse(dt.Rows[i]["ShouldCheckImageNum"].ToString());
                    }
                    res = ShouldCheck != 0 ? Math.Round(SucceedCheck / ShouldCheck, 4, MidpointRounding.AwayFromZero) * 100 : 0;
                }
                return new double[] { SucceedCheck, ShouldCheck, res };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region ///序号:21，22 已入库列车信息按线路和标识入库比率
        /// <summary>
        ///序号:21，22 已入库列车信息按线路和标识入库比率
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="importFlag">入库标识含“SOCKET”和“INDEX”</param>
        /// <param name="lineID">过车线路标识</param>
        /// <returns>分子，分母</returns>
        public static double[] GetTrainInfoImportDataByFlagRatio(string str_bgnTime, string str_endTime, string importFlag, string lineID)
        {
            double ImportTrainNum = 0; //分母
            double ImportTrainNumByFlag = 0;//分子
            try
            {
                StringBuilder sql = new StringBuilder();

                #region 获得数据库中时间段内的所有列车（组织分母）
                sql.Append("SELECT COUNT(0) FROM train WHERE Train_ComeDate BETWEEN '" + str_bgnTime + "' AND '" + str_endTime + "' ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    sql.Append("AND Line_ID='" + lineID + "' ");
                }
                sql.Append(";");
                DataTable dt = DataHelper.ExecuteMySqlDataTable(sql.ToString());
                if (dt != null && dt.Rows.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(dt.Rows[0][0].ToString()))
                    {
                        ImportTrainNum = Convert.ToInt16(dt.Rows[0][0]);
                    }
                    else
                    {
                        ImportTrainNum = 0;
                    }
                }
                #endregion

                sql.Clear();

                #region 获得数据库中时间段内的某种入库标识的列车数量（组织分子）


                sql.Append("SELECT COUNT(0) FROM train WHERE Train_ComeDate BETWEEN '" + str_bgnTime + "' AND '" + str_endTime + "' AND source='" + importFlag + "' ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    sql.Append("AND Line_ID='" + lineID + "' ");
                }
                sql.Append(";");
                DataTable dtFalg = DataHelper.ExecuteMySqlDataTable(sql.ToString());
                if (dtFalg != null && dtFalg.Rows.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(dtFalg.Rows[0][0].ToString()))
                    {
                        ImportTrainNumByFlag = Convert.ToInt16(dtFalg.Rows[0][0]);
                    }
                    else
                    {
                        ImportTrainNumByFlag = 0;
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return new double[] { ImportTrainNumByFlag, ImportTrainNum };
        }
        #endregion

        #region ///序号:24 热轮接入率
        /// <summary>
        ///序号:24 热轮接入率
        /// </summary>
        /// <param name="st">起始时间</param>
        /// <param name="ed">结束时间</param>
        /// <param name="LineFolderName">202.202.202.2/202.202.202.3</param>
        public static double[] GetHotWheelFileStatisicsRatio(string st, string ed, string LineFolderName)
        {
            try
            {
                //接入了的热轮
                double fileNum = GetFileNum(st, ed, Common.Default.Path_Hotwheel + LineFolderName, ".HotWheel");

                //总过车数
                DirectoryInfo ImgFolder = new DirectoryInfo(Path.Combine(Common.Default.Path_Pic, LineFolderName));
                long lst = long.Parse(st.Replace("-", "").Replace(" ", "").Replace(":", ""));
                long led = long.Parse(ed.Replace("-", "").Replace(" ", "").Replace(":", ""));
                double trainNum = 0;
                foreach (DirectoryInfo trainFolder in ImgFolder.GetDirectories())
                {
                    long ltmp = 0;
                    if (long.TryParse(trainFolder.Name, out ltmp))
                    {
                        if (ltmp >= lst && ltmp <= led)
                        {
                            trainNum++;
                        }
                    }
                    else continue;
                }
                return new double[] { fileNum, trainNum };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region ///序号25 热轮匹配率
        /// <summary>
        /// 热轮匹配率
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="ed">结束时间</param>
        /// <param name="LineFolderName">202.202.202.2/202.202.202.3</param>
        public static double[] GetHotWheelFileMatchRatio(string st, string ed, string LineFolderName)
        {
            try
            {
                //找线路下匹配成功的热轮原始文件
                double sucFileNum = GetFileNum(st, ed, Common.Default.Path_Hotwheel + LineFolderName, ".HotWheel", SearchOption.TopDirectoryOnly);
                double allFileNum = GetFileNum(st, ed, Common.Default.Path_Hotwheel + LineFolderName, ".HotWheel");

                return new double[] { sucFileNum, allFileNum };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region //判断字符串是否为纯数字
        /// <summary>
        ///判断字符串是否为纯数字  
        /// </summary>
        /// <param name="str">字符串类型入参</param>
        /// <param name="LogName">日志名称</param>
        /// <returns></returns>
        public static bool IsNumber(string str)
        {
            bool res = false;
            try
            {
                if (str == null || str.Length == 0)
                    return false;
                ASCIIEncoding ascii = new ASCIIEncoding();
                byte[] bytestr = ascii.GetBytes(str);

                foreach (byte c in bytestr)
                {
                    if (c < 48 || c > 57)
                    {
                        return res;
                    }
                }
                res = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return res;
        }
        #endregion

        #region ///序号26 确报接入率
        /// <summary>
        ///序号26 确报接入率
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">过车线路标识</param>
        /// <returns>确报接入率</returns>
        public static double[] GetQBRatio(string str_bgnTime, string str_endTime, string lineID)
        {
            try
            {
                StringBuilder strAll = new StringBuilder();
                strAll.Append("SELECT Train_ID,Train_ComeDate,Line_ID from train ");
                strAll.Append("where Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' and locate('客',Train_type)<=0 AND Train_type IS NOT NULL AND Train_type <>'' ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strAll.Append(" and Line_ID='" + lineID + "'; ");
                }

                double ImportAllTrain = 0;//去除客车的过车列数

                DataTable dtAll = DataHelper.ExecuteMySqlDataTable(strAll.ToString());
                if (dtAll != null && dtAll.Rows.Count > 0)
                {
                    ImportAllTrain = dtAll.Rows.Count;
                }

                StringBuilder strqb = new StringBuilder();
                strqb.Append("SELECT * FROM  ");
                strqb.Append("(SELECT Train_ID,Train_ComeDate,Line_ID from train where (locate('客',Train_type)<=0 or Train_Count>30 )AND Train_type IS NOT NULL AND Train_type <>'' AND Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "'  ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strqb.Append("and Line_ID='" + lineID + "' ");
                }
                strqb.Append(") as t ");
                strqb.Append(" JOIN qbmaster as qm on t.Train_ID=qm.TrainID ;");

                double ImportQBTrain = 0;//确报主表中的数值
                DataTable dtqb = DataHelper.ExecuteMySqlDataTable(strqb.ToString());
                if (dtqb != null && dtqb.Rows.Count > 0)
                {
                    ImportQBTrain = dtqb.Rows.Count;
                }
                return new double[] { ImportQBTrain, ImportAllTrain };
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        #endregion

        #region ///序号:28 确报车号利用率
        /// <summary>
        ///序号:28 确报车号利用率
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">列车线路</param>
        /// <returns></returns>
        public static double GetQBUseRatio(string str_bgnTime, string str_endTime, string lineID)
        {
            DataTable dt = null;
            double FenZi = 0, FenMu = 0, res = 0;
            try
            {
                StringBuilder strB = new StringBuilder();
                strB.Append("SELECT LineID,TrainComeDate,AllCarNum-RecvCarInfoNum as CarInfoMissNum ,UseQBCarInfoNum ,");
                strB.Append(" CASE WHEN (AllCarNum-RecvCarInfoNum=0)THEN 0 ");
                strB.Append(" ELSE UseQBCarInfoNum/(AllCarNum-RecvCarInfoNum) end AS UseQBRatio ");
                strB.Append(" FROM statistics_image ");
                strB.Append(" where TrainComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "'");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strB.Append("and LineID='" + lineID + "'");
                }
                strB.Append(";");

                dt = DataHelper.ExecuteMySqlDataTable(strB.ToString());
                if (dt != null && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        FenZi += Convert.ToDouble(dt.Rows[i]["UseQBCarInfoNum"]);
                        FenMu += Convert.ToDouble(dt.Rows[i]["CarInfoMissNum"]);
                    }
                    res = Math.Round(FenZi / FenMu, 4, MidpointRounding.AwayFromZero) * 100;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return res;
        }
        #endregion

        #region ///序号:29 取得周期内的列车数量
        /// <summary>
        ///序号:29 取得周期内的列车数量
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">过车线路标识</param>
        /// <returns>返回周期内的对应线路的过车列车数量</returns>
        public static int GetTrainNum(string str_bgnTime, string str_endTime, string lineID)
        {
            int num = 0;
            try
            {
                StringBuilder strB = new StringBuilder();
                strB.Append("SELECT COUNT(*) as TrainNum from train where Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "'");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strB.Append(" and Line_ID='" + lineID + "' ");
                }
                strB.Append(";");
                DataTable dt = DataHelper.ExecuteMySqlDataTable(strB.ToString());
                if (dt != null && dt.Rows.Count > 0)
                {
                    num = int.Parse(dt.Rows[0]["TrainNum"].ToString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return num;

        }
        #endregion

        #region  /// 序号:30 /*按列车类型统计列车数量*/
        /// <summary>
        /// 序号:30 /*按列车类型统计列车数量*/
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">列车线路</param>
        /// <returns>按列车类型统计出的列车数量的数据源</returns>
        public static DataTable GetTyainNumByTrainType(string str_bgnTime, string str_endTime, string lineID)
        {
            DataTable dt = null;

            try
            {
                StringBuilder strB = new StringBuilder();
                strB.Append(" SELECT case WHEN ISNULL(Train_type) || LENGTH(Train_type)<1 THEN 'NoTrainType' ELSE Train_type end as Train_type");
                strB.Append(" ,COUNT(*) as TrainNum from train");
                strB.Append(" where Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strB.Append("and Line_ID='" + lineID + "'");
                }
                strB.Append(" GROUP BY Train_type;");
                dt = DataHelper.ExecuteMySqlDataTable(strB.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;
        }
        #endregion

        #region  ///序号:31 /*周期内的车厢辆数*/
        /// <summary>
        ///序号:31 /*周期内的车厢辆数*/
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">列车线路</param>
        /// <returns>全部/对应线路的车厢数量</returns>
        public static int GetCarNumByPeriod(string str_bgnTime, string str_endTime, string lineID)
        {
            int CarNum = 0;
            try
            {
                StringBuilder strB = new StringBuilder();
                strB.Append("SELECT COUNT(td.TrainDetail_ID) as CarNum from train as t LEFT JOIN traindetail as td   ");
                strB.Append(" on t.Train_ID=td.Train_ID  where t.Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strB.Append("and Line_ID='" + lineID + "'");
                }
                DataTable dt = DataHelper.ExecuteMySqlDataTable(strB.ToString());
                if (dt != null && dt.Rows.Count > 0)
                {
                    CarNum = int.Parse(dt.Rows[0]["CarNum"].ToString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return CarNum;
        }
        #endregion

        #region ///序号:32 /*按车厢类型统计车厢辆数*/
        /// <summary>
        ///序号:32 /*按车厢类型统计车厢辆数*/
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">列车线路</param>
        /// <returns>对应各类型的车厢数量</returns>
        public static DataTable GetCarNumByCarKind(string str_bgnTime, string str_endTime, string lineID)
        {
            DataTable dt = null;
            try
            {
                StringBuilder strB = new StringBuilder();
                strB.Append(" SELECT CASE WHEN ISNULL(td.vehicletype) || LENGTH(td.vehicletype)<1 THEN '车型为空' ELSE td.vehicletype end as vehicletype");
                strB.Append(" ,count(td.vehicletype) as KindNum");
                strB.Append(" from train as t LEFT JOIN traindetail as td  on t.Train_ID=td.Train_ID");
                strB.Append(" where t.Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' ");
                if (!string.IsNullOrEmpty(lineID))
                {
                    strB.Append("and Line_ID='" + lineID + "'");
                }
                strB.Append(" GROUP BY td.vehicletype;");
                dt = DataHelper.ExecuteMySqlDataTable(strB.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;
        }
        #endregion

        #region ///序号33，34，35，36 取文件数
        /// <summary>
        /// 序号33，34，35，36 取文件数
        /// 朱恒修改 2018-04-17
        /// </summary>
        /// <param name="st">起始时间</param>
        /// <param name="ed">结束时间</param>
        /// <param name="linefolder">线路目录</param>
        /// <param name="Filter">文件类型过滤（图像监听‘.jpg’;视频监听‘.mp4’;音频监听‘.aac’;热轮监听‘.HotWheel’）</param>
        /// <returns></returns>
        public static int GetFileNum(string st, string ed, string linefolder, string Filter, SearchOption opt = SearchOption.AllDirectories)
        {
            long lst = long.Parse(st.Replace("-", "").Replace(" ", "").Replace(":", ""));
            long led = long.Parse(ed.Replace("-", "").Replace(" ", "").Replace(":", ""));
            DirectoryInfo folder = new DirectoryInfo(linefolder);//文件的上级目录
            if (!Directory.Exists(folder.FullName)) { return 0; }
            int total = 0;
            switch (Filter)
            {
                case ".jpg":
                    foreach (DirectoryInfo TrainFolder in folder.GetDirectories())//遍历列车文件夹
                    {
                        long tmpName = 0;
                        if (long.TryParse(TrainFolder.Name, out tmpName))
                        {
                            if (tmpName >= lst && tmpName <= led)
                            {
                                total += TrainFolder.GetFiles("*" + Filter).Length;
                            }
                        }
                    }
                    return total;
                case ".mp4":
                case ".aac":
                case ".HotWheel":
                    foreach (FileInfo f in folder.GetFiles("*" + Filter, opt))//遍历音视频文件
                    {
                        long tmpName = 0;
                        string tmpn = f.Name.Replace(f.Extension, "");
                        tmpn = Regex.Replace(tmpn, "[a-zA-Z.]", "");
                        if (long.TryParse(tmpn, out tmpName))
                        {
                            if (tmpName >= lst && tmpName <= led)
                            {
                                total += 1;
                            }
                        }
                    }
                    return total;
                default: return -1;
            }
        }
        #endregion

        #region ///序号:37 取周期内具有确报的车厢总数 确报总数
        /// <summary>
        ///序号:37 取周期内具有确报的车厢总数
        /// </summary>
        /// <param name="st">起始时间</param>
        /// <param name="ed">结束时间</param>
        /// <param name="lineID">列车线路</param>
        /// <returns></returns>
        public static DataTable GetAllQBNumByPeriod(string str_bgnTime, string str_endTime, string lineID)
        {
            DataTable dt = null;
            try
            {
                StringBuilder strB = new StringBuilder();
                strB.Append(" SELECT count(0) AS ALLQBTrainNum FROM ");
                strB.Append(" (SELECT Train_ID, Train_ComeDate, Line_ID from train ");
                strB.Append(" where (locate('客',Train_type)<=0  OR Train_Count>30)");
                strB.Append(" AND Train_type IS NOT NULL AND Train_type <>''");
                strB.Append(" AND Train_ComeDate BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "' ");
                if (!string.IsNullOrWhiteSpace(lineID))
                {
                    strB.Append(" and Line_ID='" + lineID + "' ");
                }
                strB.Append(" ) as t JOIN qbmaster as qm on t.Train_ID=qm.TrainID ;");
                dt = DataHelper.ExecuteMySqlDataTable(strB.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;

        }
        #endregion

        #region ///序号:38，39 所有预警总数 和 各类别报警数量 和 获去真实报警或误报数量
        /// <summary>
        ///序号:38，39 所有预警总数 和 各类别报警数量
        /// </summary>
        /// <param name="str_bgnTime">起始时间</param>
        /// <param name="str_endTime">结束时间</param>
        /// <param name="lineID">列车线路</param>
        /// <param name="AlarmOrFail">（true:报警数据；false:图像算法检测失败数据）</param>
        /// <returns></returns>
        public static DataTable GetAlarmNum(string str_bgnTime, string str_endTime, string lineID, bool AlarmOrFail)
        {
            DataTable dt = null;
            try
            {
                StringBuilder strB = new StringBuilder();
                strB.Append(" SELECT a.ProblemType,COUNT(a.ProblemType) as AlarmNum FROM ");
                strB.Append(" (SELECT * FROM train as t where t.Train_ComeDate ");
                strB.Append(" BETWEEN '" + str_bgnTime + "' and '" + str_endTime + "'");
                if (!string.IsNullOrEmpty(lineID))
                    strB.Append(" and Line_ID='" + lineID + "'");
                strB.Append(" ) as newT");
                strB.Append(" RIGHT  JOIN alarmdetail as a on newT.Train_ID=a.Train_ID WHERE newT.Train_ID IS NOT NULL");
                if (AlarmOrFail)
                {
                    strB.Append(" and (a.AlgResult=0 OR a.AlgResult is Null)");
                }
                else
                {
                    strB.Append(" and a.AlgResult<0");
                }
                strB.Append(" GROUP BY a.ProblemType");

                dt = DataHelper.ExecuteMySqlDataTable(strB.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;
        }
        /// <summary>
        /// 获去真实报警或误报数量
        /// </summary>
        /// <param name="StartTime"></param>
        /// <param name="EndTime"></param>
        /// <param name="lineID"></param>
        /// <param name="RealAlarmOrFalseAlarm">真实，误报【true,false】</param>
        /// <param name="AlarmOrFail">【true:正常算法返回0,false:算法检测为异常图像】</param>
        /// <param name="AlarmSource">来源类型【pic,video,hotwheel,voice,miss】</param>
        /// <returns></returns>
        public static decimal GetAlarmNum(string StartTime, string EndTime, string lineID, Nullable<bool> RealAlarmOrFalseAlarm, Nullable<bool> AlarmOrFail, string AlarmSource)
        {
            DataTable dt = null;
            decimal res = 0;
            try
            {
                StringBuilder SQLcmd = new StringBuilder();
                SQLcmd.Append(@"SELECT COUNT(0) FROM alarmdetail ad LEFT JOIN train t ON t.Train_ID=ad.Train_ID WHERE 1=1 ");
                if (AlarmOrFail != null)
                {
                    if (AlarmOrFail == true)
                    {
                        SQLcmd.Append(" and (ad.AlgResult=0 OR ad.AlgResult is Null)");
                    }
                    else
                    {
                        SQLcmd.Append(" and ad.AlgResult<0");
                    }
                }
                if (!string.IsNullOrWhiteSpace(AlarmSource))
                { SQLcmd.Append("AND ad.Source='" + AlarmSource + "' "); }
                if (RealAlarmOrFalseAlarm != null)
                {
                    if (RealAlarmOrFalseAlarm == true)
                    { SQLcmd.Append("AND ad.HandleResult = '0' "); }
                    else
                    { SQLcmd.Append("AND ad.HandleResult = '1' "); }
                }
                if (!string.IsNullOrWhiteSpace(lineID))
                { SQLcmd.Append("AND t.Line_ID='" + lineID + "' "); }
                if (!string.IsNullOrWhiteSpace(StartTime))
                { SQLcmd.Append("AND t.Train_ComeDate >= '" + StartTime + "' "); }
                if (!string.IsNullOrWhiteSpace(EndTime))
                { SQLcmd.Append("AND t.Train_ComeDate <= '" + EndTime + "' "); }

                dt = DataHelper.ExecuteMySqlDataTable(SQLcmd.ToString());
                if (dt != null && dt.Rows.Count > 0)
                {
                    decimal.TryParse(dt.Rows[0][0].ToString(), out res);
                }
                return res;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 图像算法调用及时性
        /// <summary>
        /// 图像算法调用及时性
        /// </summary>
        /// <param name="path"></param>
        /// <param name="Start"></param>
        /// <param name="End"></param>
        public static string GetTimely(string st, string ed)
        {
            long Start = long.Parse(st.Replace("-", "").Replace(" ", "").Replace(":", ""));
            long End = long.Parse(ed.Replace("-", "").Replace(" ", "").Replace(":", ""));
            string[] path = Common.Default.Path_SucceedXMLFolder.Split("#".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //获去时间差列表
            DataTable dt = Test_TimelyOf_CallAlgorithm(path, Start, End);
            try
            {
                StringBuilder sb = new StringBuilder();
                if (dt.Rows.Count == 0) { throw new Exception("没有符合的XML文件！"); }
                Decimal NormalTotal = 0.0M, NormalNum = 0.0M, DelayTotal = 0.0M, DelayNum = 0.0M;
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        string InTime = row["InTime"].ToString();
                        if (InTime.IndexOf("-") >= 0) { continue; }
                        Decimal thisInTime = Convert.ToDecimal(InTime);
                        if (thisInTime > 0.0M && thisInTime < 60)
                        {
                            NormalTotal = NormalTotal + thisInTime;
                            NormalNum = NormalNum + 1;
                        }
                        else if (thisInTime > 60)//60秒之后算延时
                        {
                            DelayTotal = DelayTotal + thisInTime;
                            DelayNum = DelayNum + 1;
                        }
                    }
                    catch { continue; }
                }
                Decimal NormalAvgRate = NormalNum > 0 ? Math.Round(NormalTotal / NormalNum, 4, MidpointRounding.AwayFromZero) : 0;
                Decimal DelayAvgRate = DelayNum > 0 ? Math.Round(DelayTotal / DelayNum, 4, MidpointRounding.AwayFromZero) : 0;
                Decimal DelayRate = Math.Round(DelayNum / (DelayNum + NormalNum), 4, MidpointRounding.AwayFromZero) * 100;

                sb.Append("非滞后【" + NormalNum + "】平均:" + Convert.ToString(NormalAvgRate) + "s\r\n");
                sb.Append("滞后【" + DelayNum + "】平均:" + Convert.ToString(DelayAvgRate) + "s\r\n");
                sb.Append("滞后调用率:" + Convert.ToString(DelayRate) + "%\r\n");
                //sb.Append("数据明细: \r\n");
                //for (int i = 0; i < dt.Rows.Count; i++)
                //{
                //    sb.Append("XML文件:【" + dt.Rows[i]["XmlFile"] + "】【" + dt.Rows[i]["XmlTime"]
                //        + "】,图像文件【" + dt.Rows[i]["PicFile"] + "】【" + dt.Rows[i]["PicTime"]
                //        + "】 差值【" + dt.Rows[i]["InTime"] + "】 \r\n");
                //}
                sb.Append("公式:每个“XML文件”与“图像文件”创建时间差值的平局值");
                return sb.ToString();
            }
            catch (Exception ex) { throw ex; }
        }
        private static DataTable Test_TimelyOf_CallAlgorithm(string[] Str_FolderPaths, long Start, long End)
        {
            try
            {
                string EachPicFileFullName = "";
                DataTable dt = new DataTable();
                dt.Columns.Add("XmlFile");
                dt.Columns.Add("XmlTime");
                dt.Columns.Add("PicFile");
                dt.Columns.Add("PicTime");
                dt.Columns.Add("InTime");
                foreach (string Str_item in Str_FolderPaths)
                {
                    try
                    {
                        DirectoryInfo dir = new DirectoryInfo(Str_item);
                        FileInfo[] file_xmls = dir.GetFiles("*-TXSB.xml", SearchOption.TopDirectoryOnly);
                        foreach (FileInfo fi in file_xmls)
                        {
                            string[] array = fi.Name.Split('-');
                            if (array.Length > 0)
                            {
                                Decimal theT = Convert.ToDecimal(array[1]);
                                if (theT <= Start || theT >= End)
                                {
                                    continue;
                                }
                            }
                            DataRow dr = dt.NewRow();
                            dr["XmlFile"] = fi.FullName;
                            dr["XmlTime"] = fi.CreationTime.ToString();
                            string file = Path.GetFileNameWithoutExtension(fi.FullName);
                            string[] arry = file.Split('-');
                            //组成jpg文件名
                            EachPicFileFullName = Common.Default.Path_Pic + arry[0] + "\\" + arry[1] + "\\" + arry[2] + "_" + int.Parse(arry[2].Substring(1, 3)).ToString() + ".jpg";
                            dr["PicFile"] = EachPicFileFullName;
                            if (File.Exists(EachPicFileFullName))//文件存在
                            {
                                FileInfo picInfo = new FileInfo(EachPicFileFullName);
                                dr["PicTime"] = picInfo.CreationTime.ToString();
                                TimeSpan span = (TimeSpan)(fi.CreationTime - picInfo.CreationTime);
                                dr["InTime"] = Math.Round(Convert.ToDecimal(span.TotalSeconds), 4, MidpointRounding.AwayFromZero);
                                dt.Rows.Add(dr);
                            }
                            else
                                continue;
                        }
                    }
                    catch { continue; }
                }
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 丢图，丢Index检测
        /// <summary>
        /// 检测丢图
        /// </summary>
        /// <param name="Start"></param>
        /// <param name="End"></param>
        public static void CountMissingPics(string st, string ed, string linefolder, out int Count_Dir, out List<DirectoryInfo> list_MissIndex, out List<FileInfo> list_MissPic)
        {

            Count_Dir = 0;
            long Start = long.Parse(st.Replace("-", "").Replace(" ", "").Replace(":", ""));
            long End = long.Parse(ed.Replace("-", "").Replace(" ", "").Replace(":", ""));
            long Tmp_Date;

            list_MissIndex = new List<DirectoryInfo>();
            list_MissPic = new List<FileInfo>();
            try
            {
                DirectoryInfo LineFolder = new DirectoryInfo(linefolder);
                if (!Directory.Exists(LineFolder.FullName)) { return; }
                //遍历列车文件夹
                foreach (DirectoryInfo dir_train in LineFolder.GetDirectories())
                {
                    if (!long.TryParse(dir_train.Name, out Tmp_Date)) { continue; }
                    if (Tmp_Date >= Start && Tmp_Date <= End)
                    {
                        Count_Dir += 1;
                        int carCount = ReadIndex_ForTrainCount(dir_train.FullName + @"\index.txt");
                        if (carCount == -1)
                        {
                            list_MissIndex.Add(dir_train);
                        }
                        else
                        {
                            //遍历每一节车厢
                            for (int car = 1; car <= carCount; car++)
                            {
                                string[] ns = { "L", "R" };
                                string[] zs = { "ZL", "ZR" };
                                string[] Side = linefolder.Contains("ZXGQPics") ? zs : ns;

                                //遍历每一张图片
                                foreach (string s in Side)
                                {
                                    FileInfo tmp = new FileInfo(dir_train.FullName + "\\" + s + car.ToString().PadLeft(3, '0') + "_" + car + ".jpg");
                                    if (!File.Exists(tmp.FullName)) { list_MissPic.Add(tmp); }
                                }
                            }
                        }
                    }
                    else { continue; }
                }
            }
            catch (Exception ex) { throw ex; }
        }
        /// <summary>
        /// 读Index文件，提取车厢总数
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns>-1表示文件不存在，否则返回车厢数量</returns>
        private static int ReadIndex_ForTrainCount(string FilePath)
        {
            try
            {
                if (!File.Exists(FilePath)) return -1;
                var file = File.Open(FilePath, FileMode.Open);
                string LastLine = "-1";
                using (var stream = new StreamReader(file))
                {
                    while (!stream.EndOfStream)
                    {
                        LastLine = stream.ReadLine();
                    }
                }
                string[] tmp = LastLine.Split("	".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                LastLine = tmp[0].Trim();
                file.Close();
                int res = -1;
                if (int.TryParse(LastLine, out res))//Index为空也算丢失
                {
                    return res;
                }
                else { return -1; }

            }
            catch { return -1; }
        }
        #endregion

        #region 音频生成率
        public static void GenerateRate_Audio_Video(string st, string ed, string LinePath, out List<string> MissingAudio, out List<string> MissingVideo, out List<string> MissingVideo2, out int DirCount)
        {
            try
            {
                MissingAudio = new List<string>();
                MissingVideo = new List<string>();
                MissingVideo2 = new List<string>();
                DirCount = 0;//文件夹数量（过车数量）

                DirectoryInfo lineDir = new DirectoryInfo(LinePath);
                if (!Directory.Exists(lineDir.FullName)) { return; }

                StringBuilder sb = new StringBuilder();
                long lst = long.Parse(st.Replace("-", "").Replace(" ", "").Replace(":", ""));
                long led = long.Parse(ed.Replace("-", "").Replace(" ", "").Replace(":", ""));

                //计算线面阵合计文件夹数【分母】
                long Tmp_Date;
                foreach (DirectoryInfo trainDir in lineDir.GetDirectories())
                {
                    if (!long.TryParse(trainDir.Name, out Tmp_Date)) { continue; }
                    if (Tmp_Date >= lst && Tmp_Date <= led)
                    {
                        DirCount += 1;
                        string AudioName = Common.Default.Path_Video + lineDir.Name + "\\" + trainDir.Name + ".aac";
                        string VideoName = Common.Default.Path_Video + lineDir.Name + "\\" + trainDir.Name + ".mp4";
                        string VideoName2 = Common.Default.Path_Video + lineDir.Name + "\\" + trainDir.Name + "_1.mp4";

                        if (!File.Exists(AudioName)) { MissingAudio.Add(AudioName); }
                        if (!File.Exists(VideoName)) { MissingVideo.Add(VideoName); }
                        if (!File.Exists(VideoName2)) { MissingVideo2.Add(VideoName2); }
                    }
                    else { continue; }
                }
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion
    }

    public class LineFileCls
    {
        public int sx_FileNum { get; set; }
        public int xx_FileNum { get; set; }
    }
}
