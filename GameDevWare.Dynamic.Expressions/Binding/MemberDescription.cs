using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal sealed class MemberDescription : IEquatable<MemberDescription>, IEquatable<MemberInfo>, IComparable<MemberDescription>
	{
		public const float QUALITY_EXACT_MATCH = 1.0f;
		public const float QUALITY_INCOMPATIBLE = 0.0f;

		private readonly int hashCode;
		private readonly MemberInfo member;
		private readonly ParameterInfo[] parameters;
		private readonly Dictionary<string, ParameterInfo> parametersByName;
		private readonly ParameterInfo returnParameter;

		public readonly string Name;
		public readonly Type ResultType;
		public readonly TypeDescription DeclaringType;
		public readonly bool IsCallable;
		public readonly bool IsStatic;
		public readonly bool IsImplicitOperator;
		public readonly int GenericArgumentsCount;

		public MemberDescription(TypeDescription declaringType, PropertyInfo property)
		{
			if (declaringType == null) throw new ArgumentNullException("declaringType");
			if (property == null) throw new ArgumentNullException("property");

			this.member = property;
			this.hashCode = property.GetHashCode();
			this.Name = property.Name;
			this.DeclaringType = declaringType;
			this.ResultType = property.PropertyType;
			this.IsCallable = typeof(Delegate).IsAssignableFrom(this.ResultType);
			this.IsStatic = (property.GetGetMethod(nonPublic: true) ?? property.GetSetMethod(nonPublic: true)).IsStatic;

			var getter = property.GetGetMethod(nonPublic: false);
			if (getter == null)
				return;

			this.member = getter;
			this.hashCode = getter.GetHashCode();
			this.parameters = getter.GetParameters();
			this.parametersByName = this.parameters.ToDictionary(p => p.Name);
			this.returnParameter = getter.ReturnParameter;

			this.IsCallable = this.IsCallable || this.parameters.Length > 0;
		}
		public MemberDescription(TypeDescription declaringType, FieldInfo field)
		{
			if (declaringType == null) throw new ArgumentNullException("declaringType");
			if (field == null) throw new ArgumentNullException("field");

			this.member = field;
			this.hashCode = field.GetHashCode();
			this.Name = field.Name;
			this.DeclaringType = declaringType;
			this.ResultType = field.FieldType;

			this.IsCallable = typeof(Delegate).IsAssignableFrom(this.ResultType);
			this.IsStatic = field.IsStatic;
		}
		public MemberDescription(TypeDescription declaringType, MethodInfo method)
		{
			if (declaringType == null) throw new ArgumentNullException("declaringType");
			if (method == null) throw new ArgumentNullException("method");

			this.Name = method.IsGenericMethod ? NameUtils.RemoveGenericSuffix(method.Name) : method.Name;
			this.DeclaringType = declaringType;
			this.ResultType = method.ReturnType;
			this.member = method;
			this.parameters = method.GetParameters();
			this.parametersByName = this.parameters.ToDictionary(p => p.Name);
			this.returnParameter = method.ReturnParameter;
			this.hashCode = method.GetHashCode();
			this.GenericArgumentsCount = method.IsGenericMethod ? method.GetGenericArguments().Length : 0;

			this.IsCallable = true;
			this.IsStatic = method.IsStatic;
			this.IsImplicitOperator = method.IsSpecialName && this.Name == "op_Implicit";
		}
		public MemberDescription(TypeDescription declaringType, ConstructorInfo constructor)
		{
			if (declaringType == null) throw new ArgumentNullException("declaringType");
			if (constructor == null) throw new ArgumentNullException("constructor");

			this.Name = constructor.Name;
			this.DeclaringType = declaringType;
			this.ResultType = declaringType;
			this.member = constructor;
			this.parameters = constructor.GetParameters();
			this.parametersByName = this.parameters.ToDictionary(p => p.Name);
			this.returnParameter = null;
			this.hashCode = constructor.GetHashCode();
			this.GenericArgumentsCount = constructor.IsGenericMethod ? constructor.GetGenericArguments().Length : 0;

			this.IsCallable = true;
			this.IsStatic = constructor.IsStatic;
		}

		public ParameterInfo GetParameter(int parameterIndex)
		{
			if (parameterIndex < -1 || parameterIndex >= this.GetParametersCount()) throw new ArgumentOutOfRangeException("parameterIndex");
			if (parameterIndex == -1)
				return this.returnParameter;
			return this.parameters[parameterIndex];
		}
		public int GetParametersCount()
		{
			if (this.parameters == null)
				return 0;
			else
				return this.parameters.Length;
		}

		public bool TryMakeAccessor(Expression target, out Expression expression)
		{
			if (target == null) throw new ArgumentNullException("target");

			var field = this.member as FieldInfo;
			var method = this.member as MethodInfo;
			if (field != null)
				expression = Expression.Field(target, field);
			else if (method != null && this.parameters.Length == 0)
				expression = Expression.Property(target, method);
			else
				expression = null;

			return expression != null;
		}
		public bool TryMakeConversion(Expression target, out Expression expression, bool checkedConversion)
		{
			expression = null;

			var method = this.member as MethodInfo;
			if (method == null)
				return false;

			expression = checkedConversion ?
				Expression.ConvertChecked(target, method.ReturnType, method) :
				Expression.Convert(target, method.ReturnType, method);
			return true;
		}
		public bool TryMakeCall(Expression target, ArgumentsTree argumentsTree, BindingContext bindingContext, out Expression expression, out float expressionQuality)
		{
			if (argumentsTree == null) throw new ArgumentNullException("argumentsTree");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");

			expression = null;
			expressionQuality = QUALITY_INCOMPATIBLE;

			if (this.member is MethodBase == false)
				return false;

			// check argument count
			if (argumentsTree.Count > this.parameters.Length)
				return false; // not all arguments are bound to parameters

			var requiredParametersCount = this.parameters.Length - this.parameters.Count(p => p.IsOptional);
			if (argumentsTree.Count < requiredParametersCount)
				return false; // not all required parameters has values

			if (this.parameters.Length == 0)
			{
				expressionQuality = QUALITY_EXACT_MATCH;
				return true;
			}

			// bind arguments
			var parametersQuality = 0.0f;
			var arguments = default(Expression[]);
			foreach (var argumentName in argumentsTree.Keys)
			{
				var parameter = default(ParameterInfo);
				var parameterIndex = 0;
				if (HasDigitsOnly(argumentName))
				{
					parameterIndex = int.Parse(argumentName, Constants.DefaultFormatProvider);
					if (parameterIndex >= this.parameters.Length)
						return false; // position out of range

					parameter = this.parameters[parameterIndex];

					if (argumentsTree.ContainsKey(parameter.Name))
						return false; // positional intersects named
				}
				else
				{
					if (this.parametersByName.TryGetValue(argumentName, out parameter) == false)
						return false; // parameter is not found
					parameterIndex = parameter.Position;
				}

				var expectedType = Metadata.GetTypeDescription(parameter.ParameterType);
				var argValue = default(Expression);
				var bindingError = default(Exception);
				if (AnyBinder.TryBind(argumentsTree[argumentName], bindingContext, expectedType, out argValue, out bindingError) == false)
					return false;
				var quality = ExpressionUtils.TryMorphType(ref argValue, expectedType);

				if (quality <= 0)
					return false;// failed to bind parameter

				parametersQuality += quality; // casted
				if (arguments == null) arguments = new Expression[this.parameters.Length];
				arguments[parameterIndex] = argValue;
			}

			if (arguments == null) arguments = new Expression[this.parameters.Length];
			for (var i = 0; i < arguments.Length; i++)
			{
				if (arguments[i] != null) continue;
				var parameter = this.parameters[i];
				if (parameter.IsOptional == false)
					return false; // missing required parameter

				var typeDescription = Metadata.GetTypeDescription(parameter.ParameterType);
				arguments[i] = typeDescription.DefaultExpression;
				parametersQuality += TypeConversion.QUALITY_SAME_TYPE;
			}

			expressionQuality = parametersQuality / this.parameters.Length;
			if (this.member is MethodInfo)
			{
				if (this.IsStatic)
					expression = Expression.Call((MethodInfo)this.member, arguments);
				else
					expression = Expression.Call(target, (MethodInfo)this.member, arguments);
				return true;
			}
			else if (this.member is ConstructorInfo)
			{
				expression = Expression.New((ConstructorInfo)this.member, arguments);
				return true;
			}

			expressionQuality = QUALITY_INCOMPATIBLE;
			return false;
		}

		private static bool HasDigitsOnly(string argName)
		{
			foreach (var @char in argName)
				if (char.IsDigit(@char) == false)
					return false;
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj is MemberDescription)
				return this.Equals(obj as MemberDescription);
			else if (obj is MemberInfo)
				return this.Equals(obj as MemberInfo);
			else
				return false;
		}
		public override int GetHashCode()
		{
			return this.hashCode;
		}
		public bool Equals(MemberInfo other)
		{
			if (other == null) return false;

			return this.member.Equals(other);
		}
		public bool Equals(MemberDescription other)
		{
			if (other == null) return false;

			return this.member.Equals(other.member);
		}
		public int CompareTo(MemberDescription other)
		{
			if (other == null) return 1;

			var cmp = other.GetParametersCount().CompareTo(this.GetParametersCount());
			return cmp != 0 ? cmp : string.CompareOrdinal(this.Name, other.Name);
		}

		public static implicit operator MemberInfo(MemberDescription memberDescription)
		{
			if (memberDescription == null) return null;
			return memberDescription.member;
		}


		public override string ToString()
		{
			return this.member.ToString();
		}
	}
}
