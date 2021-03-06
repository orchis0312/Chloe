﻿using Chloe.Annotations;
using Chloe.Core.Visitors;
using Chloe.DbExpressions;
using Chloe.InternalExtensions;
using Chloe.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Chloe.MySql
{
    public class DbExpressionOptimizer : DbExpressionOptimizerBase
    {
        static DbExpressionOptimizer _optimizer = new DbExpressionOptimizer();

        static KeyDictionary<MemberInfo> _toTranslateMembers = new KeyDictionary<MemberInfo>();
        static DbExpressionOptimizer()
        {
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_String_Length);

            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Now);
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_UtcNow);
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Today);
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Date);

            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Year);
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Month);
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Day);
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Hour);
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Minute);
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Second);
            /* MySql is not supports MILLISECOND */
            //_toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_Millisecond); 
            _toTranslateMembers.Add(UtilConstants.PropertyInfo_DateTime_DayOfWeek);

            _toTranslateMembers = _toTranslateMembers.Clone();
        }

        public static DbExpression Optimize(DbExpression exp)
        {
            return exp.Accept(_optimizer);
        }

        public override bool CanTranslateToSql(DbMemberExpression exp)
        {
            return _toTranslateMembers.Exists(exp.Member);
        }
        public override bool CanTranslateToSql(DbMethodCallExpression exp)
        {
            IMethodHandler methodHandler;
            if (SqlGenerator.MethodHandlers.TryGetValue(exp.Method.Name, out methodHandler))
            {
                if (methodHandler.CanProcess(exp))
                {
                    return true;
                }
            }

            return false;
        }

        public override DbExpression Visit(DbUpdateExpression exp)
        {
            if (!(exp is MySqlDbUpdateExpression))
            {
                return base.Visit(exp);
            }

            MySqlDbUpdateExpression ret = new MySqlDbUpdateExpression(exp.Table, this.MakeNewExpression(exp.Condition));

            foreach (var kv in exp.UpdateColumns)
            {
                ret.UpdateColumns.Add(kv.Key, this.MakeNewExpression(kv.Value));
            }

            ret.Limits = (exp as MySqlDbUpdateExpression).Limits;

            return ret;
        }
        public override DbExpression Visit(DbDeleteExpression exp)
        {
            if (!(exp is MySqlDbDeleteExpression))
            {
                return base.Visit(exp);
            }

            var ret = new MySqlDbDeleteExpression(exp.Table, this.MakeNewExpression(exp.Condition));
            ret.Limits = (exp as MySqlDbDeleteExpression).Limits;

            return ret;
        }

        DbExpression MakeNewExpression(DbExpression exp)
        {
            if (exp == null)
                return null;

            return exp.Accept(this);
        }
    }
}
