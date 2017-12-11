using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class MemberAssignmentsNode : ExecutionNode
	{
		public static readonly MemberAssignmentsNode Empty = new MemberAssignmentsNode(new ReadOnlyCollection<MemberBinding>(new MemberBinding[0]), new ConstantExpression[0], new ParameterExpression[0]);

		internal struct PreparedMemberAssignment
		{
			public readonly MemberInfo Member;
			public readonly ExecutionNode ValueNode;

			public PreparedMemberAssignment(MemberInfo member, ExecutionNode valueNode)
			{
				this.Member = member;
				this.ValueNode = valueNode;
			}
		}

		private readonly PreparedMemberAssignment[] memberAssignments;

		public MemberAssignmentsNode(ReadOnlyCollection<MemberBinding> bindings, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (bindings == null) throw new ArgumentNullException("bindings");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.memberAssignments = new PreparedMemberAssignment[bindings.Count(b => b is MemberAssignment)];
			var i = 0;
			foreach (var binding in bindings)
			{
				var memberAssignment = binding as MemberAssignment;
				if (memberAssignment == null)
					continue;

				this.memberAssignments[i++] = new PreparedMemberAssignment(memberAssignment.Member, AotCompiler.Compile(memberAssignment.Expression, constExpressions, parameterExpressions));
			}
		}
		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var instance = closure.Unbox<object>(closure.Locals[LOCAL_OPERAND1]);

			if (this.memberAssignments.Length == 0)
				return instance;

			foreach (var assignFn in this.memberAssignments)
			{
				var member = assignFn.Member;
				var valueFn = assignFn.ValueNode;
				var value = closure.Unbox<object>(valueFn.Run(closure));
				var fieldInfo = member as FieldInfo;
				var propertyInfo = member as PropertyInfo;

				if (instance == null)
					throw new NullReferenceException();

				if (fieldInfo != null)
					fieldInfo.SetValue(instance, value);
				else if (propertyInfo != null)
					propertyInfo.SetValue(instance, value, null);
				else
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_INVALIDMEMBERFOREXPRESSION, member));
			}

			return instance;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Join(", ", this.memberAssignments.Select(m => m.Member.Name).ToArray());
		}
	}
}
