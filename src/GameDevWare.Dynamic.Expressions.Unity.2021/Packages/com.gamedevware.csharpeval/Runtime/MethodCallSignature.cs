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
using System.Reflection;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions
{
	internal class MethodCallSignature
	{
		public readonly int Count;
		private readonly int hashCode;

		public readonly string Parameter1Name;

		public readonly Type Parameter1Type;
		public readonly string Parameter2Name;
		public readonly Type Parameter2Type;
		public readonly string Parameter3Name;
		public readonly Type Parameter3Type;
		public readonly string Parameter4Name;
		public readonly Type Parameter4Type;

		public readonly Type ReturnType;

		public MethodCallSignature(Type returnType)
		{
			if (returnType == null) throw new ArgumentNullException(nameof(returnType));

			this.ReturnType = returnType;
			this.hashCode = this.ComputeHashCode();
			this.Count = 0;
		}
		public MethodCallSignature(Type parameter1Type, string parameter1Name, Type returnType)
		{
			if (parameter1Type == null) throw new ArgumentNullException(nameof(parameter1Type));
			if (parameter1Name == null) throw new ArgumentNullException(nameof(parameter1Name));
			if (returnType == null) throw new ArgumentNullException(nameof(returnType));

			this.Parameter1Type = parameter1Type;
			this.Parameter1Name = parameter1Name;

			this.ReturnType = returnType;
			this.hashCode = this.ComputeHashCode();
			this.Count = 1;
		}
		public MethodCallSignature(Type parameter1Type, string parameter1Name, Type parameter2Type, string parameter2Name, Type returnType)
		{
			if (parameter1Type == null) throw new ArgumentNullException(nameof(parameter1Type));
			if (parameter2Type == null) throw new ArgumentNullException(nameof(parameter2Type));
			if (parameter1Name == null) throw new ArgumentNullException(nameof(parameter1Name));
			if (parameter2Name == null) throw new ArgumentNullException(nameof(parameter2Name));
			if (returnType == null) throw new ArgumentNullException(nameof(returnType));

			this.Parameter1Type = parameter1Type;
			this.Parameter2Type = parameter2Type;

			this.Parameter1Name = parameter1Name;
			this.Parameter2Name = parameter2Name;

			this.ReturnType = returnType;
			this.hashCode = this.ComputeHashCode();
			this.Count = 2;
		}
		public MethodCallSignature
			(Type parameter1Type, string parameter1Name, Type parameter2Type, string parameter2Name, Type parameter3Type, string parameter3Name, Type returnType)
		{
			if (parameter1Type == null) throw new ArgumentNullException(nameof(parameter1Type));
			if (parameter2Type == null) throw new ArgumentNullException(nameof(parameter2Type));
			if (parameter3Type == null) throw new ArgumentNullException(nameof(parameter3Type));
			if (parameter1Name == null) throw new ArgumentNullException(nameof(parameter1Name));
			if (parameter2Name == null) throw new ArgumentNullException(nameof(parameter2Name));
			if (parameter3Name == null) throw new ArgumentNullException(nameof(parameter3Name));
			if (returnType == null) throw new ArgumentNullException(nameof(returnType));

			this.Parameter1Type = parameter1Type;
			this.Parameter2Type = parameter2Type;
			this.Parameter3Type = parameter3Type;

			this.Parameter1Name = parameter1Name;
			this.Parameter2Name = parameter2Name;
			this.Parameter3Name = parameter3Name;

			this.ReturnType = returnType;
			this.hashCode = this.ComputeHashCode();
			this.Count = 3;
		}
		public MethodCallSignature
		(
			Type parameter1Type,
			string parameter1Name,
			Type parameter2Type,
			string parameter2Name,
			Type parameter3Type,
			string parameter3Name,
			Type parameter4Type,
			string parameter4Name,
			Type returnType)
		{
			if (parameter1Type == null) throw new ArgumentNullException(nameof(parameter1Type));
			if (parameter2Type == null) throw new ArgumentNullException(nameof(parameter2Type));
			if (parameter3Type == null) throw new ArgumentNullException(nameof(parameter3Type));
			if (parameter4Type == null) throw new ArgumentNullException(nameof(parameter4Type));
			if (parameter1Name == null) throw new ArgumentNullException(nameof(parameter1Name));
			if (parameter2Name == null) throw new ArgumentNullException(nameof(parameter2Name));
			if (parameter3Name == null) throw new ArgumentNullException(nameof(parameter3Name));
			if (parameter4Name == null) throw new ArgumentNullException(nameof(parameter4Name));
			if (returnType == null) throw new ArgumentNullException(nameof(returnType));

			this.Parameter1Type = parameter1Type;
			this.Parameter2Type = parameter2Type;
			this.Parameter3Type = parameter3Type;
			this.Parameter4Type = parameter4Type;

			this.Parameter1Name = parameter1Name;
			this.Parameter2Name = parameter2Name;
			this.Parameter3Name = parameter3Name;
			this.Parameter4Name = parameter4Name;

			this.ReturnType = returnType;
			this.hashCode = this.ComputeHashCode();
			this.Count = 4;
		}
		public MethodCallSignature(MethodInfo method, bool includeParameterNames = true)
		{
			if (method == null) throw new ArgumentNullException(nameof(method));

			var parameter = method.GetParameters();
			switch (parameter.Length)
			{
				case 4:

					this.Parameter4Name = includeParameterNames ? parameter[3].Name : "";
					this.Parameter4Type = parameter[3].ParameterType;
					goto case 3;
				case 3:
					this.Parameter3Name = includeParameterNames ? parameter[2].Name : "";
					this.Parameter3Type = parameter[2].ParameterType;
					goto case 2;
				case 2:
					this.Parameter2Name = includeParameterNames ? parameter[1].Name : "";
					this.Parameter2Type = parameter[1].ParameterType;
					goto case 1;
				case 1:
					this.Parameter1Name = includeParameterNames ? parameter[0].Name : "";
					this.Parameter1Type = parameter[0].ParameterType;
					goto case 0;
				case 0:
					this.ReturnType = method.ReturnType;
					break;
				default:
					throw new ArgumentException(Resources.EXCEPTION_UNBOUNDEXPR_INVALIDPARAMCOUNT, nameof(method));
			}

			this.Count = parameter.Length;
			this.hashCode = this.ComputeHashCode();
		}

		public override bool Equals(object obj)
		{
			var parameters = obj as MethodCallSignature;
			if (parameters == null) return false;
			if (ReferenceEquals(parameters, this)) return true;

			return this.Parameter1Type == parameters.Parameter1Type &&
				this.Parameter2Type == parameters.Parameter2Type &&
				this.Parameter3Type == parameters.Parameter3Type &&
				this.Parameter4Type == parameters.Parameter4Type &&
				this.Parameter1Name == parameters.Parameter1Name &&
				this.Parameter2Name == parameters.Parameter2Name &&
				this.Parameter3Name == parameters.Parameter3Name &&
				this.Parameter4Name == parameters.Parameter4Name &&
				this.ReturnType == parameters.ReturnType;
		}
		public override int GetHashCode()
		{
			return this.hashCode;
		}
		private int ComputeHashCode()
		{
			return unchecked
			(
				(this.Parameter1Type?.GetHashCode() ?? 0) * 17 +
				(this.Parameter2Type?.GetHashCode() ?? 0) * 17 +
				(this.Parameter3Type?.GetHashCode() ?? 0) * 17 +
				(this.Parameter4Type?.GetHashCode() ?? 0) * 17 +
				(this.Parameter1Name?.GetHashCode() ?? 0) * 17 +
				(this.Parameter2Name?.GetHashCode() ?? 0) * 17 +
				(this.Parameter3Name?.GetHashCode() ?? 0) * 17 +
				(this.Parameter4Name?.GetHashCode() ?? 0) * 17 +
				this.ReturnType.GetHashCode()
			);
		}

		public override string ToString()
		{
			return this.Parameter1Type + ", " + this.Parameter2Type + ", " + this.Parameter3Type + ", " + this.Parameter4Type + ", " + this.ReturnType;
		}
	}
}
