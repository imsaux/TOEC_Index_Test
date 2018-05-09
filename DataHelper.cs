using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;

namespace TOEC_Index_Test
{

    /// <summary>
    /// 版权所有:天津光电高斯通信工程有限公司
    /// 内容摘要:此类主要是连接数据库。
    /// 创建日期：2017.04.10
    /// 版    本：V1.0.0.0
    /// 作    者：王俊杰	
    /// </summary>
    public class DataHelper
    {
        #region .net连接mysql数据库返回DataSet
        /// <summary>
        /// net连接mysql数据库返回DataSet
        /// </summary>
        /// <param name="cmdText"></param>
        /// <returns></returns>
        public static DataSet ExecuteMySqlDataSet(string cmdText)
        {

            DataSet dataSet = new DataSet();
            using (MySqlConnection conn = new MySqlConnection(Common.Default.ConStr))
            {
                try
                {
                    MySqlDataAdapter adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = new MySqlCommand(cmdText, conn);
                    adapter.SelectCommand.CommandTimeout = 600;
                    adapter.Fill(dataSet);
                }
                catch (Exception ex)
                {
                    dataSet = null;
                    throw ex;
                }
                finally
                {
                    try
                    {
                        conn.Close();
                    }
                    catch
                    { }
                }
            }
            return dataSet;

        }
        #endregion

        #region .net连接mysql数据库返回DataTable
        /// <summary>
        /// net连接mysql数据库返回DataSet
        /// </summary>
        /// <param name="cmdText"></param>
        /// <returns></returns>
        public static DataTable ExecuteMySqlDataTable(string cmdText)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Common.Default.ConStr))
                {
                    DataTable dataTable = new DataTable();
                    try
                    {
                        MySqlDataAdapter adapter = new MySqlDataAdapter();
                        adapter.SelectCommand = new MySqlCommand(cmdText, conn);
                        adapter.SelectCommand.CommandTimeout = 600;
                        adapter.Fill(dataTable);
                    }
                    catch (Exception ex)
                    {
                        dataTable = null;
                        throw ex;
                    }
                    finally
                    {
                        conn.Close();
                    }
                    return dataTable;
                }
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region .net连接mysql数据库返回bool(带参数)
        /// <summary>
        /// .net连接mysql数据库返回bool(带参数)
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static bool ExecuteMySqlBool(string cmdText, CommandType type, params MySqlParameter[] parameters)
        {
            bool bRet = false;
            using (MySqlConnection conn = new MySqlConnection(Common.Default.ConStr))
            {
                try
                {
                    conn.Open();
                    MySqlCommand commond = new MySqlCommand(cmdText, conn);
                    commond.CommandType = type;
                    if (parameters != null)
                    {
                        commond.Parameters.AddRange(parameters);
                    }
                    int i = commond.ExecuteNonQuery();

                    if (i != 0)
                    {
                        bRet = true;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                    //Logger.WriteLog(logName, "【异常】数据库执行时ExecuteMySql函数(带参数)发生异常:" + ex.Message);
                }
                finally
                {
                    try
                    {
                        conn.Close();
                    }
                    catch
                    { }
                }
            }
            return bRet;
        }
        #endregion

        #region .net连接mysql数据库返回bool
        /// <summary> 
        /// .net连接mysql数据库返回bool
        /// </summary>
        /// <param name="cmdText"></param>
        /// <returns></returns>
        public static bool ExecuteMySqlBool(string cmdText)
        {
            bool bRet = true;
            using (MySqlConnection conn = new MySqlConnection(Common.Default.ConStr))
            {
                try
                {
                    conn.Open();
                    MySqlCommand sc = new MySqlCommand(cmdText, conn);
                    sc.CommandTimeout = 600;
                    int num = sc.ExecuteNonQuery();
                    if (num != 0)
                        bRet = true;
                }
                catch (Exception ex)
                {
                    bRet = false;
                    throw ex;
                }
                finally
                {
                    try
                    {
                        conn.Close();
                    }
                    catch
                    { }
                }
            }
            return bRet;
        }
        #endregion
    }
}
