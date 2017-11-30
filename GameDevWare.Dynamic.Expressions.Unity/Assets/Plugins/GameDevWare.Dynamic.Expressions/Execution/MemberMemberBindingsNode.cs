using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class MemberMemberBindingsNode : ExecutionNode
	{
		public static readonly MemberMemberBindingsNode Empty = new MemberMemberBindingsNode(new ReadOnlyCollection<MemberBinding>(new MemberBinding[0]), new ConstantExpression[0], new ParameterExpression[0]);

		internal struct PreparedMemberBinding
		{
			public readonly MemberInfo Member;
			public readonly MemberAssignmentsNode MemberAssignments;
			public readonly MemberListBindingsNode MemberListBindings;
			public readonly MemberMemberBindingsNode MemberMemberBindings;

			public PreparedMemberBinding
			(
				MemberInfo member,
				MemberAssignmentsNode memberAssignments,
				MemberListBindingsNode memberListBindings,
				MemberMemberBindingsNode memberMemberBindings
			)
			{
				this.Member = member;
				this.MemberAssignments = memberAssignments;
				this.MemberListBindings = memberListBindings;
				this.MemberMemberBindings = memberMemberBindings;
			}
		}

		private readonly ILookup<MemberInfo, PreparedMemberBinding> bindingsByMember;

		public MemberMemberBindingsNode(ReadOnlyCollection<MemberBinding> bindings, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (bindings == null) throw new ArgumentNullException("bindings");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			var memberBindings = new PreparedMemberBinding[bindings.Count(b => b is MemberMemberBinding)];
			var i = 0;
			foreach (var binding in bindings)
			{
				var memberMemberBinding = binding as MemberMemberBinding;
				if (memberMemberBinding == null)
					continue;

				var memberAssignments = memberMemberBinding.Bindings.Any(b => b is MemberAssignment) ?
					new MemberAssignmentsNode(memberMemberBinding.Bindings, constExpressions, parameterExpressions) :
					MemberAssignmentsNode.Empty;

				var listBindings = memberMemberBinding.Bindings.Any(b => b is MemberListBinding) ?
					new MemberListBindingsNode(memberMemberBinding.Bindings, constExpressions, parameterExpressions) :
					MemberListBindingsNode.Empty;

				var memberMemberBindings = memberMemberBinding.Bindings.Any(b => b is MemberMemberBinding) ?
					new MemberMemberBindingsNode(memberMemberBinding.Bindings, constExpressions, parameterExpressions) :
					Empty;

				memberBindings[i++] = new PreparedMemberBinding(memberMemberBinding.Member, memberAssignments, listBindings, memberMemberBindings);
			}

			this.bindingsByMember = memberBindings.ToLookup(b => b.Member);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var target = closure.Unbox<object>(closure.Locals[LOCAL_OPERAND1]);
			if (this.bindingsByMember.Count == 0)
				return target;

			foreach (var bindings in this.bindingsByMember)
			{
				var member = bindings.Key;
				var fieldInfo = member as FieldInfo;
				var propertyInfo = member as PropertyInfo;
				var bindTarget = default(object);

				if (fieldInfo != null)
				{
					if (fieldInfo.IsStatic == false && target == null)
						throw new NullReferenceException();
					bindTarget = fieldInfo.GetValue(target);
				}
				else if (propertyInfo != null)
				{
					var getMethod = propertyInfo.GetGetMethod(nonPublic: true);
					if (getMethod.IsStatic == false && target == null)
						throw new NullReferenceException();

					bindTarget = propertyInfo.GetValue(target, null);
				}
				else
				{
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_INVALIDMEMBERFOREXPRESSION, member));
				}

				foreach (var bind in bindings)
				{
					if (ReferenceEquals(bind.MemberAssignments, MemberAssignmentsNode.Empty) == false)
					{
						closure.Locals[LOCAL_OPERAND1] = bindTarget;
						bind.MemberAssignments.Run(closure);
					}

					if (ReferenceEquals(bind.MemberListBindings, MemberListBindingsNode.Empty) == false)
					{
						closure.Locals[LOCAL_OPERAND1] = bindTarget;
						bind.MemberListBindings.Run(closure);
					}

					if (ReferenceEquals(bind.MemberMemberBindings, Empty) == false)
					{
						closure.Locals[LOCAL_OPERAND1] = bindTarget;
						bind.MemberMemberBindings.Run(closure);
					}
				}
			}
			return target;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Join(", ", this.bindingsByMember.Select(m => m.Key.Name).ToArray());
		}
	}
}
