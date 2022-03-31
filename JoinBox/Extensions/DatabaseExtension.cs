using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acap = Autodesk.AutoCAD.ApplicationServices.Application;

namespace JoinBox.Extensions
{
    public static class DatabaseExtension
    {
        /// <summary>
        /// 事务处理器的封装,无返回值
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="act">delegate函数</param>
        /// <param name="commit">是否提交,默认提交</param>
        public static void Action(this Database db, Action<Transaction> act, bool commit = true)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    act.Invoke(tr);//事务就被传出去了
                }
                catch (Exception e)
                {
                    var st = new StackTrace(new StackFrame(true));
                    var sf = st.GetFrame(0);
                    var sb = new StringBuilder();
                    sb.Append("\n文件:" + sf.GetFileName());                   //文件名
                    sb.Append("\n方法:" + sf.GetMethod().Name);                //函数名
                    sb.Append("\n行号:" + sf.GetFileLineNumber().ToString());  //文件行号
                    sb.Append("\n列号:" + sf.GetFileColumnNumber().ToString());//文件列号
                    sb.Append("\n事务处理拦截了错误" + e.Message);
                    Debug.WriteLine(sb);
                    Debugger.Break();
                }
                finally
                {
                    if (tr != null && !tr.IsDisposed)
                    {
                        if (commit && tr.TransactionManager.NumberOfActiveTransactions != 0)//防止重复提交
                            tr.Commit();
                        tr.Dispose();
                    }
                }
            }
        }
        /// <summary>
        /// 事务处理器的封装,有返回值
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="db">数据库对象</param>
        /// <param name="func">delegate函数</param>
        /// <param name="commit">是否提交,默认提交</param>
        /// <returns>泛型</returns>
        public static T Func<T>(this Database db, Func<Transaction, T> func, bool commit = true)
        {
            T rtn = default; //泛型返回
            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    rtn = func.Invoke(tr);//接收了返回值
                }
                catch (Exception e)
                {
                    var st = new StackTrace(new StackFrame(true));
                    var sf = st.GetFrame(0);
                    var sb = new StringBuilder();
                    sb.Append("\n文件:" + sf.GetFileName());                   //文件名
                    sb.Append("\n方法:" + sf.GetMethod().Name);                //函数名
                    sb.Append("\n行号:" + sf.GetFileLineNumber().ToString());  //文件行号
                    sb.Append("\n列号:" + sf.GetFileColumnNumber().ToString());//文件列号
                    sb.Append("\n事务处理拦截了错误" + e.Message);
                    Acap.ShowAlertDialog(sb.ToString());
                    Debugger.Break();
                }
                finally
                {
                    if (tr != null && !tr.IsDisposed)
                    {
                        if (commit && tr.TransactionManager.NumberOfActiveTransactions != 0)//防止重复提交
                            tr.Commit();
                        tr.Dispose();
                    }
                }
            }
            return rtn; //返回接收到的
        }

    }
}
