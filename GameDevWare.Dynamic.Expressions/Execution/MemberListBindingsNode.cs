using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class MemberListBindingsNode : ExecutionNode
	{
		public static readonly MemberListBindingsNode Empty = new MemberListBindingsNode(new ReadOnlyCollection<MemberBinding>(new MemberBinding[0]), new ConstantExpression[0], new ParameterExpression[0]);

		internal struct PreparedListBinding
		{
			public readonly MemberInfo Member;
			public readonly MethodInfo AddMethod;
			public readonly ExecutionNode[] AddMethodArguments;

			public PreparedListBinding(MemberInfo member, MethodInfo addMethod, ExecutionNode[] addArguments)
			{
				this.Member = member;
				this.AddMethod = addMethod;
				this.AddMethodArguments = addArguments;
			}
		}

		private readonly ILookup<MemberInfo, PreparedListBinding> bindingsByMember;

		public MemberListBindingsNode(ReadOnlyCollection<MemberBinding> bindings, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (bindings == null) throw new ArgumentNullException("bindings");
			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			var listBindings = new PreparedListBinding[bindings.Sum(b => b is MemberListBinding ? ((MemberListBinding)b).Initializers.Count : 0)];
			var i = 0;
			foreach (var binding in bindings)
			{
				var memberListBinding = binding as MemberListBinding;
				if (memberListBinding == null)
					continue;

				foreach (var elementInitializer in memberListBinding.Initializers)
				{
					var arguments = new ExecutionNode[elementInitializer.Arguments.Count];
					for (var a = 0; a < arguments.Length; a++)
						arguments[a] = AotCompiler.Compile(elementInitializer.Arguments[a], constExpressions, parameterExpressions);

					listBindings[i++] = new PreparedListBinding(memberListBinding.Member, elementInitializer.AddMethod, arguments);
				}
			}

			this.bindingsByMember = listBindings.ToLookup(b => b.Member);
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var instance = closure.Unbox<object>(closure.Locals[LOCAL_OPERAND1]);
			if (this.bindingsByMember.Count == 0)
				return instance;

			foreach (var bindings in this.bindingsByMember)
			{
				var member = bindings.Key;
				var addTarget = default(object);
				var fieldInfo = member as FieldInfo;
				var propertyInfo = member as PropertyInfo;

				if (fieldInfo != null)
				{
					if (fieldInfo.IsStatic == false && instance == null)
						throw new NullReferenceException();
					addTarget = fieldInfo.GetValue(instance);
				}
				else if (propertyInfo != null)
				{
					if (propertyInfo.IsStatic() == false && instance == null)
						throw new NullReferenceException();

					addTarget = propertyInfo.GetValue(instance, null);
				}
				else
				{
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_EXECUTION_INVALIDMEMBERFOREXPRESSION, member));
				}

				foreach (var bindGroup in bindings)
				{
					var addMethod = bindGroup.AddMethod;
					var addArgumentNodes = bindGroup.AddMethodArguments;
					var addArguments = new object[addArgumentNodes.Length];
					for (var i = 0; i < addArgumentNodes.Length; i++)
						addArguments[i] = closure.Unbox<object>(addArgumentNodes[i].Run(closure));

					addMethod.Invoke(addTarget, addArguments);
				}
			}
			return instance;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Join(", ", this.bindingsByMember.Select(m => m.Key.Name).ToArray());
		}
	}
}
