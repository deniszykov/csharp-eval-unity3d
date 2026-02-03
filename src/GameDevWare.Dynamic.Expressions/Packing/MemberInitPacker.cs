using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class MemberInitPacker
	{
		public static Dictionary<string, object> Pack(MemberInitExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			return new Dictionary<string, object>(3) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_MEMBER_INIT },
				{ Constants.NEW_ATTRIBUTE, AnyPacker.Pack(expression.NewExpression) },
				{ Constants.BINDINGS_ATTRIBUTE, Pack(expression.Bindings) }
			};
		}
		public static Dictionary<string, object> Pack(ReadOnlyCollection<MemberBinding> expressionBindings)
		{
			if (expressionBindings == null) throw new ArgumentNullException(nameof(expressionBindings));

			var bindingList = new Dictionary<string, object>(expressionBindings.Count);
			for (var i = 0; i < expressionBindings.Count; i++)
			{
				var key = Constants.GetIndexAsString(i);
				var binding = expressionBindings[i];
				switch (binding.BindingType)
				{
					case MemberBindingType.Assignment:
						bindingList.Add(key, Pack((MemberAssignment)binding));
						break;
					case MemberBindingType.MemberBinding:
						bindingList.Add(key, Pack((MemberMemberBinding)binding));
						break;
					case MemberBindingType.ListBinding:
						bindingList.Add(key, Pack((MemberListBinding)binding));
						break;
					default: throw new ArgumentOutOfRangeException();
				}
			}

			return bindingList;
		}
		public static Dictionary<string, object> Pack(MemberListBinding memberListBinding)
		{
			if (memberListBinding == null) throw new ArgumentNullException(nameof(memberListBinding));

			return new Dictionary<string, object>(3) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_LIST_BINDING },
				{ Constants.MEMBER_ATTRIBUTE, AnyPacker.Pack(memberListBinding.Member) },
				{ Constants.INITIALIZERS_ATTRIBUTE, Pack(memberListBinding.Initializers) }
			};
		}
		public static Dictionary<string, object> Pack(MemberMemberBinding memberMemberBinding)
		{
			if (memberMemberBinding == null) throw new ArgumentNullException(nameof(memberMemberBinding));

			return new Dictionary<string, object>(3) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_MEMBER_BINDING },
				{ Constants.MEMBER_ATTRIBUTE, AnyPacker.Pack(memberMemberBinding.Member) },
				{ Constants.BINDINGS_ATTRIBUTE, Pack(memberMemberBinding.Bindings) }
			};
		}
		public static Dictionary<string, object> Pack(MemberAssignment memberAssignment)
		{
			if (memberAssignment == null) throw new ArgumentNullException(nameof(memberAssignment));

			return new Dictionary<string, object> {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_ASSIGNMENT_BINDING },
				{ Constants.MEMBER_ATTRIBUTE, AnyPacker.Pack(memberAssignment.Member) },
				{ Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(memberAssignment.Expression) }
			};
		}
		public static Dictionary<string, object> Pack(ElementInit elementInit)
		{
			if (elementInit == null) throw new ArgumentNullException(nameof(elementInit));

			var arguments = elementInit.Arguments.ToArray();

			return new Dictionary<string, object>(2) {
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_ELEMENT_INIT_BINDING },
				{ Constants.METHOD_ATTRIBUTE, AnyPacker.Pack(elementInit.AddMethod) },
				{ Constants.ARGUMENTS_ATTRIBUTE, AnyPacker.Pack(arguments, null) }
			};
		}

		internal static Dictionary<string, object> Pack(ReadOnlyCollection<ElementInit> elementInitializers)
		{
			if (elementInitializers == null) throw new ArgumentNullException(nameof(elementInitializers));

			var initializers = new Dictionary<string, object>();
			for (var i = 0; i < elementInitializers.Count; i++)
			{
				var key = Constants.GetIndexAsString(i);
				var value = Pack(elementInitializers[i]);
				initializers[key] = value;
			}

			return initializers;
		}
	}
}
