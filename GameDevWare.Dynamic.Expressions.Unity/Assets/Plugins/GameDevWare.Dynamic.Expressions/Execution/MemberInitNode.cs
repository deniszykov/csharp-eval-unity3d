using System;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class MemberInitNode : ExecutionNode
	{
		private readonly MemberInitExpression memberInitExpression;
		private readonly NewNode newNode;
		private readonly MemberAssignmentsNode memberAssignmentNode;
		private readonly MemberListBindingsNode listBindingNode;
		private readonly MemberMemberBindingsNode memberMemberBindingNode;

		public MemberInitNode(MemberInitExpression memberInitExpression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (memberInitExpression == null) throw new ArgumentNullException("memberInitExpression");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			this.memberInitExpression = memberInitExpression;

			this.newNode = new NewNode(memberInitExpression.NewExpression, constExpressions, parameterExpressions);
			this.memberAssignmentNode = memberInitExpression.Bindings.Any(b => b is MemberAssignment) ?
				new MemberAssignmentsNode(memberInitExpression.Bindings, constExpressions, parameterExpressions) :
				MemberAssignmentsNode.Empty;

			this.listBindingNode = memberInitExpression.Bindings.Any(b => b is MemberListBinding) ?
				new MemberListBindingsNode(memberInitExpression.Bindings, constExpressions, parameterExpressions) :
				MemberListBindingsNode.Empty;

			this.memberMemberBindingNode = memberInitExpression.Bindings.Any(b => b is MemberMemberBinding) ?
				new MemberMemberBindingsNode(memberInitExpression.Bindings, constExpressions, parameterExpressions) :
				MemberMemberBindingsNode.Empty;
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var instance = closure.Unbox<object>(this.newNode.Run(closure));

			if (ReferenceEquals(this.memberAssignmentNode, MemberAssignmentsNode.Empty) == false)
			{
				closure.Locals[LOCAL_OPERAND1] = instance;
				this.memberAssignmentNode.Run(closure);
			}

			if (ReferenceEquals(this.listBindingNode, MemberListBindingsNode.Empty) == false)
			{
				closure.Locals[LOCAL_OPERAND1] = instance;
				this.listBindingNode.Run(closure);
			}

			if (ReferenceEquals(this.memberMemberBindingNode, MemberMemberBindingsNode.Empty) == false)
			{
				closure.Locals[LOCAL_OPERAND1] = instance;
				this.memberMemberBindingNode.Run(closure);
			}

			closure.Locals[LOCAL_OPERAND1] = null;

			return instance;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.memberInitExpression.ToString();
		}
	}
}
