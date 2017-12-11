/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "C# Eval()" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY,
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.

	This source code is distributed via Unity Asset Store,
	to use it in your project you should accept Terms of Service and EULA
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private readonly Dictionary<TypeTuple, MemberDescription> methodInstantiations;
		private readonly MemberDescription genericDefinition;

		public readonly string Name;
		public readonly Type ResultType;
		public readonly TypeDescription DeclaringType;
		public readonly bool IsMethod;
		public readonly bool IsConstructor;
		public readonly bool IsPropertyOrField;
		public readonly bool IsStatic;
		public readonly bool IsImplicitOperator;
		public readonly Type[] GenericArguments;
		public readonly int GenericArgumentsCount;
		public readonly Expression ConstantValueExpression;

		public MemberDescription(TypeDescription declaringType, PropertyInfo property)
		{
			if (declaringType == null) throw new ArgumentNullException("declaringType");
			if (property == null) throw new ArgumentNullException("property");

			this.member = property;
			this.hashCode = property.GetHashCode();
			this.Name = property.Name;
			this.DeclaringType = declaringType;
			this.ResultType = property.PropertyType;
			this.IsStatic = property.IsStatic();
			this.IsPropertyOrField = true;
			var constantValue = (property.Attributes & PropertyAttributes.HasDefault) != 0 ? property.GetConstantValue() : null;
			this.ConstantValueExpression = constantValue != null ? Expression.Constant(constantValue) : null;

			var getter = property.GetPublicGetter();
			if (getter == null)
				return;

			this.member = getter;
			this.hashCode = getter.GetHashCode();
			this.parameters = getter.GetParameters();
			this.parametersByName = this.parameters.ToDictionary(GetParameterName, StringComparer.Ordinal);
			this.returnParameter = getter.ReturnParameter;
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
			var constantValue = (field.Attributes & (FieldAttributes.HasDefault | FieldAttributes.Literal)) != 0 ?
#if NETSTANDARD
				(field.IsStatic ? field.GetValue(null) : null) :
#else
				field.GetRawConstantValue() :
#endif
				null;
			if (declaringType.IsEnum && constantValue != null)
				this.ConstantValueExpression = Expression.Constant(Enum.ToObject(declaringType, constantValue), declaringType);
			else
				this.ConstantValueExpression = constantValue != null ? Expression.Constant(constantValue) : null;

			this.IsStatic = field.IsStatic;
			this.IsPropertyOrField = true;
		}
		public MemberDescription(TypeDescription declaringType, MethodInfo method, MemberDescription genericMethodDefinition = null)
		{
			if (declaringType == null) throw new ArgumentNullException("declaringType");
			if (method == null) throw new ArgumentNullException("method");

			this.Name = method.IsGenericMethod ? NameUtils.RemoveGenericSuffix(method.Name) : method.Name;
			this.DeclaringType = declaringType;
			this.ResultType = method.ReturnType;
			this.member = method;
			this.parameters = method.GetParameters();
			this.parametersByName = this.parameters.ToDictionary(GetParameterName, StringComparer.Ordinal);
			this.returnParameter = method.ReturnParameter;
			this.hashCode = method.GetHashCode();
			if (method.IsGenericMethod)
			{
				this.GenericArguments = method.GetGenericArguments();
				this.GenericArgumentsCount = this.GenericArguments.Length;
				if (method.IsGenericMethodDefinition)
				{
					this.methodInstantiations = new Dictionary<TypeTuple, MemberDescription>();
					this.genericDefinition = this;
				}
				else
				{
					if (genericMethodDefinition == null) throw new ArgumentNullException("genericMethodDefinition");

					this.methodInstantiations = genericMethodDefinition.methodInstantiations;
					this.genericDefinition = genericMethodDefinition;
				}
			}
			this.IsMethod = true;
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
			this.parametersByName = this.parameters.ToDictionary(GetParameterName, StringComparer.Ordinal);
			this.returnParameter = null;
			this.hashCode = constructor.GetHashCode();
			this.GenericArgumentsCount = constructor.IsGenericMethod ? constructor.GetGenericArguments().Length : 0;

			this.IsConstructor = true;
			this.IsStatic = constructor.IsStatic;
		}

		public ParameterInfo GetParameter(int parameterIndex)
		{
			if (parameterIndex < -1 || parameterIndex >= this.GetParametersCount()) throw new ArgumentOutOfRangeException("parameterIndex");
			if (parameterIndex == -1)
				return this.returnParameter;
			return this.parameters[parameterIndex];
		}
		public Type GetParameterType(int parameterIndex)
		{
			if (parameterIndex < -1 || parameterIndex >= this.GetParametersCount()) throw new ArgumentOutOfRangeException("parameterIndex");
			if (parameterIndex == -1)
				return this.returnParameter != null ? this.returnParameter.ParameterType : null;

			return this.parameters[parameterIndex].ParameterType;
		}
		public string GetParameterName(int parameterIndex)
		{
			if (parameterIndex < -1 || parameterIndex >= this.GetParametersCount()) throw new ArgumentOutOfRangeException("parameterIndex");
			if (parameterIndex == -1)
				return this.returnParameter != null ? GetParameterName(this.returnParameter) : null;

			return GetParameterName(this.parameters[parameterIndex]);
		}
		public int GetParametersCount()
		{
			if (this.parameters == null)
				return 0;
			else
				return this.parameters.Length;
		}

		public MemberDescription MakeGenericMethod(Type[] genericArguments)
		{
			if (genericArguments == null) throw new ArgumentNullException("genericArguments");
			if (this.IsMethod == false) throw new InvalidOperationException(string.Format("Can't instantiate not method '{0}'.", this.member));
			if (this.GenericArgumentsCount <= 0) throw new InvalidOperationException(string.Format("Can't instantiate non-generic method '{0}'.", this.member));

			var key = new TypeTuple(genericArguments);
			var instantiatedMethodDescription = default(MemberDescription);
			lock (this.methodInstantiations)
			{
				if (this.methodInstantiations.TryGetValue(key, out instantiatedMethodDescription))
					return instantiatedMethodDescription;
			}
			var instantiatedMethod = ((MethodInfo)this.genericDefinition).MakeGenericMethod(genericArguments);
			instantiatedMethodDescription = new MemberDescription(this.DeclaringType, instantiatedMethod, this.genericDefinition);
			lock (this.methodInstantiations)
				this.methodInstantiations[key] = instantiatedMethodDescription;
			return instantiatedMethodDescription;
		}

		public bool TryMakeAccessor(Expression target, out Expression expression)
		{
			if (!this.IsStatic && target == null) throw new ArgumentNullException("target");

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
		public bool TryMakeConversion(Expression valueExpression, out Expression expression, bool checkedConversion)
		{
			if (valueExpression == null) throw new ArgumentNullException("valueExpression");

			expression = null;

			var method = this.member as MethodInfo;
			if (method == null)
				return false;

			var valueType = TypeDescription.GetTypeDescription(valueExpression.Type);
			var resultType = TypeDescription.GetTypeDescription(method.ReturnType);
			var liftedConversion = valueType.IsNullable;

			if (resultType.CanBeNull == false)
				resultType = liftedConversion ? resultType.GetNullableType() : resultType;

			expression = checkedConversion ?
				Expression.ConvertChecked(valueExpression, resultType, method) :
				Expression.Convert(valueExpression, resultType, method);
			return true;
		}
		public bool TryMakeCall(Expression target, ArgumentsTree argumentsTree, BindingContext bindingContext, out Expression expression, out float expressionQuality)
		{
			if (argumentsTree == null) throw new ArgumentNullException("argumentsTree");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (!this.IsStatic && !this.IsConstructor && target == null) throw new ArgumentNullException("target");

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

				var expectedType = TypeDescription.GetTypeDescription(parameter.ParameterType);
				var argValue = default(Expression);
				var bindingError = default(Exception);
				if (AnyBinder.TryBindInNewScope(argumentsTree[argumentName], bindingContext, expectedType, out argValue, out bindingError) == false)
					return false;

				Debug.Assert(argValue != null, "argValue != null");

				var quality = 0.0f;
				if (ExpressionUtils.TryMorphType(ref argValue, expectedType, out quality) == false || quality <= 0)
					return false;// failed to bind parameter

				parametersQuality += quality; // casted
				if (arguments == null) arguments = new Expression[this.parameters.Length];
				arguments[parameterIndex] = argValue;
			}

			if (this.parameters.Length > 0)
			{
				if (arguments == null) arguments = new Expression[this.parameters.Length];
				for (var i = 0; i < arguments.Length; i++)
				{
					if (arguments[i] != null) continue;
					var parameter = this.parameters[i];
					if (parameter.IsOptional == false)
						return false; // missing required parameter

					var typeDescription = TypeDescription.GetTypeDescription(parameter.ParameterType);
					arguments[i] = typeDescription.DefaultExpression;
					parametersQuality += TypeConversion.QUALITY_SAME_TYPE;
				}

				expressionQuality = parametersQuality / this.parameters.Length;
			}
			else
			{
				expressionQuality = QUALITY_EXACT_MATCH;
			}


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
		private static string GetParameterName(ParameterInfo parameter)
		{
			if (parameter == null) throw new ArgumentNullException("parameter");
			var name = parameter.Name;
			if (string.IsNullOrEmpty(name))
				name = parameter.Member.Name + "_" + parameter.Position.ToString();
			return name;
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
		public static implicit operator MethodInfo(MemberDescription memberDescription)
		{
			if (memberDescription == null) return null;
			return (MethodInfo)memberDescription.member;
		}

		public static bool operator ==(MemberDescription member1, MemberDescription member2)
		{
			if (ReferenceEquals(member1, member2)) return true;
			if (ReferenceEquals(member1, null) || ReferenceEquals(member2, null)) return false;

			return member1.Equals(member2);
		}
		public static bool operator !=(MemberDescription type1, MemberDescription type2)
		{
			return !(type1 == type2);
		}

		public override string ToString()
		{
			return this.member.ToString();
		}
	}
}
