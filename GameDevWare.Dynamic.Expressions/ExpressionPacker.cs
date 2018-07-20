using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using GameDevWare.Dynamic.Expressions.Packing;

namespace GameDevWare.Dynamic.Expressions
{
    public static class ExpressionPacker
    {
		public static Dictionary<string, object> Pack(Expression expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			return AnyPacker.Pack(expression);
		}
    }
}
