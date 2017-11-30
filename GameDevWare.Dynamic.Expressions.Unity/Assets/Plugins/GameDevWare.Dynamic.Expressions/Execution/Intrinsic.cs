using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable RedundantOverflowCheckingContext
// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantCast
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedMethodReturnValue.Local

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal static class Intrinsic
	{
		public delegate object BinaryOperation(Closure closure, object left, object right);
		public delegate object UnaryOperation(Closure closure, object operand);

		private static readonly Dictionary<Type, Dictionary<int, Delegate>> Operations;
		private static readonly Dictionary<Type, Dictionary<Type, Delegate>> Conversions;
		private static readonly ExecutionNode[] UnaryOperationArgumentNodes = { LocalNode.Operand1 };
		private static readonly ExecutionNode[] BinaryOperationArgumentNodes = { LocalNode.Operand1, LocalNode.Operand2 };

		static Intrinsic()
		{
			var expressionTypeNames = Enum.GetNames(typeof(ExpressionType));
			Array.Sort(expressionTypeNames, StringComparer.Ordinal);

			Operations = new Dictionary<Type, Dictionary<int, Delegate>>();
			foreach (var opType in typeof(Intrinsic).GetNestedTypes(BindingFlags.NonPublic))
			{
				if (opType.Name.StartsWith("op_", StringComparison.Ordinal) == false) continue;
				var type = Type.GetType("System." + opType.Name.Substring(3), false);
				if (type == null) continue;

				var delegatesByExpressionType = default(Dictionary<int, Delegate>);
				if (Operations.TryGetValue(type, out delegatesByExpressionType) == false)
					Operations[type] = delegatesByExpressionType = new Dictionary<int, Delegate>();

				foreach (var method in opType.GetMethods(BindingFlags.Public | BindingFlags.Static))
				{
					if (Array.BinarySearch(expressionTypeNames, method.Name) < 0)
						continue;

					var expressionType = (ExpressionType)Enum.Parse(typeof(ExpressionType), method.Name);
					var methodParams = method.GetParameters();
					var fn = methodParams.Length == 3 ? (Delegate)CreateBinaryOperationFn(method) :
						methodParams.Length == 2 ? (Delegate)CreateUnaryOperationFn(method) : null;

					delegatesByExpressionType[(int)expressionType] = fn;
				}
			}

			Conversions = new Dictionary<Type, Dictionary<Type, Delegate>>();
			foreach (var opType in typeof(Intrinsic).GetNestedTypes(BindingFlags.NonPublic))
			{
				if (opType.Name.StartsWith("op_", StringComparison.Ordinal) == false) continue;
				var type = Type.GetType("System." + opType.Name.Substring(3), false);
				if (type == null) continue;

				var convertorsByType = default(Dictionary<Type, Delegate>);
				if (Conversions.TryGetValue(type, out convertorsByType) == false)
					Conversions[type] = convertorsByType = new Dictionary<Type, Delegate>();

				foreach (var method in opType.GetMethods(BindingFlags.Public | BindingFlags.Static))
				{
					if (method.Name.StartsWith("To", StringComparison.Ordinal) == false)
						continue;

					var fn = (Delegate)CreateBinaryOperationFn(method);
					var toType = Type.GetType("System." + method.Name.Substring(2), false);
					if (toType == null)
						continue;

					convertorsByType[toType] = fn;
				}
			}
		}

		public static object InvokeBinaryOperation
		(
			Closure closure,
			object left,
			object right,
			ExpressionType binaryOperationType,
			BinaryOperation userDefinedBinaryOperation
		)
		{
			if (closure == null) throw new ArgumentNullException("closure");

			var type = left != null ? closure.GetType(left) : closure.GetType(right);
			var operationsForType = default(Dictionary<int, Delegate>);
			var func = default(Delegate);

			if (Operations.TryGetValue(type, out operationsForType) && operationsForType.TryGetValue((int)binaryOperationType, out func))
				return ((BinaryOperation)func)(closure, left, right);

			if (binaryOperationType == ExpressionType.Equal)
				userDefinedBinaryOperation = (BinaryOperation)Operations[typeof(object)][(int)ExpressionType.Equal];
			else if (binaryOperationType == ExpressionType.NotEqual)
				userDefinedBinaryOperation = (BinaryOperation)Operations[typeof(object)][(int)ExpressionType.NotEqual];

			if (userDefinedBinaryOperation == null)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_NOBINARYOPONTYPE, binaryOperationType, type));

			return userDefinedBinaryOperation(closure, left, right);
		}

		public static object InvokeUnaryOperation
		(
			Closure closure,
			object operand,
			ExpressionType unaryOperationType,
			UnaryOperation userDefinedUnaryOperation
		)
		{
			if (closure == null) throw new ArgumentNullException("closure");

			var type = closure.GetType(operand);
			var operationsForType = default(Dictionary<int, Delegate>);
			var func = default(Delegate);

			if (Operations.TryGetValue(type, out operationsForType) && operationsForType.TryGetValue((int)unaryOperationType, out func))
				return ((UnaryOperation)func)(closure, operand);

			if (userDefinedUnaryOperation == null)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_NOUNARYOPONTYPE, unaryOperationType, type));

			return userDefinedUnaryOperation(closure, operand);
		}

		public static object InvokeConversion
		(
			Closure closure,
			object value,
			Type toType,
			ExpressionType convertType,
			UnaryOperation userDefinedConvertOperation
		)
		{
			if (closure == null) throw new ArgumentNullException("closure");
			if (toType == null) throw new ArgumentNullException("toType");

			var type = closure.GetType(value);
			var dictionary = default(Dictionary<Type, Delegate>);
			var func = default(Delegate);

			if (Conversions.TryGetValue(type, out dictionary) && dictionary.TryGetValue(toType, out func))
				return ((BinaryOperation)func)(closure, value, convertType == ExpressionType.Convert ? bool.FalseString : bool.TrueString);

			if (userDefinedConvertOperation == null)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_NOCONVERTIONBETWEENTYPES, type, toType));

			return userDefinedConvertOperation(closure, value);
		}

		public static UnaryOperation CreateUnaryOperationFn(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException("method");

			return (UnaryOperation)Delegate.CreateDelegate(typeof(UnaryOperation), method, true);
		}
		public static BinaryOperation CreateBinaryOperationFn(MethodInfo method)
		{
			if (method == null) throw new ArgumentNullException("method");

			return (BinaryOperation)Delegate.CreateDelegate(typeof(BinaryOperation), method, true);
		}
		public static UnaryOperation WrapUnaryOperation(Type type, string methodName)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (methodName == null) throw new ArgumentNullException("methodName");

			var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
			return WrapUnaryOperation(method);
		}
		public static UnaryOperation WrapUnaryOperation(MethodInfo method)
		{
			if (method == null) return null;

			var invoker = FastCall.TryCreate(method);
			if (invoker != null)
			{
				return (closure, operand) =>
				{
					closure.Locals[ExecutionNode.LOCAL_OPERAND1] = operand;

					var result = invoker(closure, UnaryOperationArgumentNodes);

					closure.Locals[ExecutionNode.LOCAL_OPERAND1] = null;

					return result;
				};
			}
			else
			{
				return (closure, operand) => method.Invoke(null, new[] { operand });
			}
		}
		public static BinaryOperation WrapBinaryOperation(Type type, string methodName)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (methodName == null) throw new ArgumentNullException("methodName");

			var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
			if (method == null) return null;

			return WrapBinaryOperation(method);
		}
		public static BinaryOperation WrapBinaryOperation(MethodInfo method)
		{
			if (method == null) return null;


			var invoker = FastCall.TryCreate(method);
			if (invoker != null)
			{

				return (closure, left, right) =>
				{
					closure.Locals[ExecutionNode.LOCAL_OPERAND1] = left;
					closure.Locals[ExecutionNode.LOCAL_OPERAND2] = right;

					var result = invoker(closure, BinaryOperationArgumentNodes);

					closure.Locals[ExecutionNode.LOCAL_OPERAND1] = null;
					closure.Locals[ExecutionNode.LOCAL_OPERAND2] = null;

					return result;
				};
			}
			else
			{
				return (closure, left, right) => method.Invoke(null, new[] { left, right });
			}
		}

		internal static class op_Object
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(object));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box(Equals(closure.Unbox<object>(left), closure.Unbox<object>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box(!Equals(closure.Unbox<object>(left), closure.Unbox<object>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToBoolean(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<bool>(left));
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<sbyte>(left));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<byte>(left));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<short>(left));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<ushort>(left));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<int>(left));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<uint>(left));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<long>(left));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<ulong>(left));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<float>(left));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<double>(left));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				return closure.Box(closure.Unbox<decimal>(left));
			}
		}
		internal static class op_Boolean
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(bool));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box(!closure.Unbox<bool>(operand));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box(Equals(closure.Unbox<bool>(left), closure.Unbox<bool>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box(!Equals(closure.Unbox<bool>(left), closure.Unbox<bool>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<bool>(left) | closure.Unbox<bool>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<bool>(left) & closure.Unbox<bool>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<bool>(left) ^ closure.Unbox<bool>(right)));
			}
			public static object AndAlso(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<bool>(left) && closure.Unbox<bool>(right)));
			}
			public static object OrElse(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<bool>(left) || closure.Unbox<bool>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToBoolean(Closure closure, object left, object _)
			{
				return closure.Box(closure.Unbox<bool>(left));
			}
		}
		internal static class op_Byte
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(byte));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((byte)unchecked(-closure.Unbox<byte>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((byte)checked(-closure.Unbox<byte>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((byte)unchecked(+closure.Unbox<byte>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((byte)~closure.Unbox<byte>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((byte)unchecked(closure.Unbox<byte>(left) + closure.Unbox<byte>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((byte)checked(closure.Unbox<byte>(left) + closure.Unbox<byte>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) & closure.Unbox<byte>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) / closure.Unbox<byte>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) == closure.Unbox<byte>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) ^ closure.Unbox<byte>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) > closure.Unbox<byte>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) >= closure.Unbox<byte>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((byte)Math.Pow(closure.Unbox<byte>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) < closure.Unbox<byte>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) <= closure.Unbox<byte>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) % closure.Unbox<byte>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((byte)unchecked(closure.Unbox<byte>(left) * closure.Unbox<byte>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((byte)checked(closure.Unbox<byte>(left) * closure.Unbox<byte>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<byte>(left) != closure.Unbox<byte>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((byte)(closure.Unbox<byte>(left) | closure.Unbox<byte>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((byte)unchecked(closure.Unbox<byte>(left) - closure.Unbox<byte>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((byte)checked(closure.Unbox<byte>(left) - closure.Unbox<byte>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<byte>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<byte>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<byte>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<byte>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<byte>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<byte>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<byte>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<byte>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<byte>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<byte>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<byte>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<byte>(left)));
			}
		}
		internal static class op_SByte
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(sbyte));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((sbyte)unchecked(-closure.Unbox<sbyte>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((sbyte)checked(-closure.Unbox<sbyte>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((sbyte)unchecked(+closure.Unbox<sbyte>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((sbyte)~closure.Unbox<sbyte>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)unchecked(closure.Unbox<sbyte>(left) + closure.Unbox<sbyte>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)checked(closure.Unbox<sbyte>(left) + closure.Unbox<sbyte>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) & closure.Unbox<sbyte>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) / closure.Unbox<sbyte>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) == closure.Unbox<sbyte>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) ^ closure.Unbox<sbyte>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) > closure.Unbox<sbyte>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) >= closure.Unbox<sbyte>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)Math.Pow(closure.Unbox<sbyte>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) < closure.Unbox<sbyte>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) <= closure.Unbox<sbyte>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) % closure.Unbox<sbyte>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)unchecked(closure.Unbox<sbyte>(left) * closure.Unbox<sbyte>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)checked(closure.Unbox<sbyte>(left) * closure.Unbox<sbyte>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<sbyte>(left) != closure.Unbox<sbyte>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
#pragma warning disable 0675
				return closure.Box((sbyte)(closure.Unbox<sbyte>(left) | closure.Unbox<sbyte>(right)));
#pragma warning restore 0675
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)unchecked(closure.Unbox<sbyte>(left) - closure.Unbox<sbyte>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((sbyte)checked(closure.Unbox<sbyte>(left) - closure.Unbox<sbyte>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<sbyte>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<sbyte>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<sbyte>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<sbyte>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<sbyte>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<sbyte>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<sbyte>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<sbyte>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<sbyte>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<sbyte>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<sbyte>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<sbyte>(left)));
			}
		}
		internal static class op_Int16
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(short));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((short)unchecked(-closure.Unbox<short>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((short)checked(-closure.Unbox<short>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((short)unchecked(+closure.Unbox<short>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((short)~closure.Unbox<short>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((short)unchecked(closure.Unbox<short>(left) + closure.Unbox<short>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((short)checked(closure.Unbox<short>(left) + closure.Unbox<short>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) & closure.Unbox<short>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) / closure.Unbox<short>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) == closure.Unbox<short>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) ^ closure.Unbox<short>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) > closure.Unbox<short>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) >= closure.Unbox<short>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((short)Math.Pow(closure.Unbox<short>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) < closure.Unbox<short>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) <= closure.Unbox<short>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) % closure.Unbox<short>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((short)unchecked(closure.Unbox<short>(left) * closure.Unbox<short>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((short)checked(closure.Unbox<short>(left) * closure.Unbox<short>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<short>(left) != closure.Unbox<short>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((short)(closure.Unbox<short>(left) | closure.Unbox<short>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((short)unchecked(closure.Unbox<short>(left) - closure.Unbox<short>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((short)checked(closure.Unbox<short>(left) - closure.Unbox<short>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<short>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<short>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<short>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<short>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<short>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<short>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<short>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<short>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<short>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<short>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<short>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<short>(left)));
			}
		}
		internal static class op_UInt16
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(ushort));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((ushort)unchecked(-closure.Unbox<ushort>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((ushort)checked(-closure.Unbox<ushort>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((ushort)unchecked(+closure.Unbox<ushort>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((ushort)~closure.Unbox<ushort>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((ushort)unchecked(closure.Unbox<ushort>(left) + closure.Unbox<ushort>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((ushort)checked(closure.Unbox<ushort>(left) + closure.Unbox<ushort>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) & closure.Unbox<ushort>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) / closure.Unbox<ushort>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) == closure.Unbox<ushort>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) ^ closure.Unbox<ushort>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) > closure.Unbox<ushort>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) >= closure.Unbox<ushort>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((ushort)Math.Pow(closure.Unbox<ushort>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) < closure.Unbox<ushort>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) <= closure.Unbox<ushort>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) % closure.Unbox<ushort>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((ushort)unchecked(closure.Unbox<ushort>(left) * closure.Unbox<ushort>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((ushort)checked(closure.Unbox<ushort>(left) * closure.Unbox<ushort>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ushort>(left) != closure.Unbox<ushort>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((ushort)(closure.Unbox<ushort>(left) | closure.Unbox<ushort>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((ushort)unchecked(closure.Unbox<ushort>(left) - closure.Unbox<ushort>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((ushort)checked(closure.Unbox<ushort>(left) - closure.Unbox<ushort>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<ushort>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<ushort>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<ushort>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<ushort>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<ushort>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<ushort>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<ushort>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<ushort>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<ushort>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<ushort>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<ushort>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<ushort>(left)));
			}
		}
		internal static class op_Int32
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(int));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((int)unchecked(-closure.Unbox<int>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((int)checked(-closure.Unbox<int>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((int)unchecked(+closure.Unbox<int>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((int)~closure.Unbox<int>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((int)unchecked(closure.Unbox<int>(left) + closure.Unbox<int>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((int)checked(closure.Unbox<int>(left) + closure.Unbox<int>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) & closure.Unbox<int>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) / closure.Unbox<int>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) == closure.Unbox<int>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) ^ closure.Unbox<int>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) > closure.Unbox<int>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) >= closure.Unbox<int>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((int)Math.Pow(closure.Unbox<int>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) < closure.Unbox<int>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) <= closure.Unbox<int>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) % closure.Unbox<int>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((int)unchecked(closure.Unbox<int>(left) * closure.Unbox<int>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((int)checked(closure.Unbox<int>(left) * closure.Unbox<int>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<int>(left) != closure.Unbox<int>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((int)(closure.Unbox<int>(left) | closure.Unbox<int>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((int)unchecked(closure.Unbox<int>(left) - closure.Unbox<int>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((int)checked(closure.Unbox<int>(left) - closure.Unbox<int>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<int>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<int>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<int>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<int>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<int>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<int>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<int>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<int>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<int>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<int>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<int>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<int>(left)));
			}
		}
		internal static class op_UInt32
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(uint));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((uint)unchecked(-closure.Unbox<uint>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((uint)checked(-closure.Unbox<uint>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((uint)unchecked(+closure.Unbox<uint>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((uint)~closure.Unbox<uint>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((uint)unchecked(closure.Unbox<uint>(left) + closure.Unbox<uint>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((uint)checked(closure.Unbox<uint>(left) + closure.Unbox<uint>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) & closure.Unbox<uint>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) / closure.Unbox<uint>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) == closure.Unbox<uint>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) ^ closure.Unbox<uint>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) > closure.Unbox<uint>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) >= closure.Unbox<uint>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((uint)Math.Pow(closure.Unbox<uint>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) < closure.Unbox<uint>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) <= closure.Unbox<uint>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) % closure.Unbox<uint>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((uint)unchecked(closure.Unbox<uint>(left) * closure.Unbox<uint>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((uint)checked(closure.Unbox<uint>(left) * closure.Unbox<uint>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<uint>(left) != closure.Unbox<uint>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((uint)(closure.Unbox<uint>(left) | closure.Unbox<uint>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((uint)unchecked(closure.Unbox<uint>(left) - closure.Unbox<uint>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((uint)checked(closure.Unbox<uint>(left) - closure.Unbox<uint>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<uint>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<uint>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<uint>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<uint>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<uint>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<uint>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<uint>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<uint>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<uint>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<uint>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<uint>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<uint>(left)));
			}
		}
		internal static class op_Int64
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(long));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((long)unchecked(-closure.Unbox<long>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((long)checked(-closure.Unbox<long>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((long)unchecked(+closure.Unbox<long>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((long)~closure.Unbox<long>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((long)unchecked(closure.Unbox<long>(left) + closure.Unbox<long>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((long)checked(closure.Unbox<long>(left) + closure.Unbox<long>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) & closure.Unbox<long>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) / closure.Unbox<long>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) == closure.Unbox<long>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) ^ closure.Unbox<long>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) > closure.Unbox<long>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) >= closure.Unbox<long>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((long)Math.Pow(closure.Unbox<long>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) < closure.Unbox<long>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) <= closure.Unbox<long>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) % closure.Unbox<long>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((long)unchecked(closure.Unbox<long>(left) * closure.Unbox<long>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((long)checked(closure.Unbox<long>(left) * closure.Unbox<long>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<long>(left) != closure.Unbox<long>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((long)(closure.Unbox<long>(left) | closure.Unbox<long>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((long)unchecked(closure.Unbox<long>(left) - closure.Unbox<long>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((long)checked(closure.Unbox<long>(left) - closure.Unbox<long>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<long>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<long>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<long>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<long>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<long>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<long>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<long>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<long>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<long>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<long>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<long>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<long>(left)));
			}
		}
		internal static class op_UInt64
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(ulong));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((ulong)unchecked(+closure.Unbox<ulong>(operand)));
			}
			public static object Not(Closure closure, object operand)
			{
				return closure.Box((ulong)~closure.Unbox<ulong>(operand));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((ulong)unchecked(closure.Unbox<ulong>(left) + closure.Unbox<ulong>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((ulong)checked(closure.Unbox<ulong>(left) + closure.Unbox<ulong>(right)));
			}
			public static object And(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) & closure.Unbox<ulong>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) / closure.Unbox<ulong>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) == closure.Unbox<ulong>(right)));
			}
			public static object ExclusiveOr(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) ^ closure.Unbox<ulong>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) > closure.Unbox<ulong>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) >= closure.Unbox<ulong>(right)));
			}
			public static object LeftShift(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) << closure.Unbox<int>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((ulong)Math.Pow(closure.Unbox<ulong>(left), closure.Unbox<double>(right)));
			}
			public static object RightShift(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) >> closure.Unbox<int>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) < closure.Unbox<ulong>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) <= closure.Unbox<ulong>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) % closure.Unbox<ulong>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((ulong)unchecked(closure.Unbox<ulong>(left) * closure.Unbox<ulong>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((ulong)checked(closure.Unbox<ulong>(left) * closure.Unbox<ulong>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<ulong>(left) != closure.Unbox<ulong>(right)));
			}
			public static object Or(Closure closure, object left, object right)
			{
				return closure.Box((ulong)(closure.Unbox<ulong>(left) | closure.Unbox<ulong>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((ulong)unchecked(closure.Unbox<ulong>(left) - closure.Unbox<ulong>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((ulong)checked(closure.Unbox<ulong>(left) - closure.Unbox<ulong>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<ulong>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<ulong>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<ulong>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<ulong>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<ulong>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<ulong>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<ulong>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<ulong>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<ulong>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<ulong>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<ulong>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<ulong>(left)));
			}
		}
		internal static class op_Single
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(float));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((float)unchecked(-closure.Unbox<float>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((float)checked(-closure.Unbox<float>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((float)unchecked(+closure.Unbox<float>(operand)));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((float)unchecked(closure.Unbox<float>(left) + closure.Unbox<float>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((float)checked(closure.Unbox<float>(left) + closure.Unbox<float>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((float)(closure.Unbox<float>(left) / closure.Unbox<float>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) == closure.Unbox<float>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) > closure.Unbox<float>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) >= closure.Unbox<float>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((float)Math.Pow(closure.Unbox<float>(left), closure.Unbox<double>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) < closure.Unbox<float>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) <= closure.Unbox<float>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((float)(closure.Unbox<float>(left) % closure.Unbox<float>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((float)unchecked(closure.Unbox<float>(left) * closure.Unbox<float>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((float)checked(closure.Unbox<float>(left) * closure.Unbox<float>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<float>(left) != closure.Unbox<float>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((float)unchecked(closure.Unbox<float>(left) - closure.Unbox<float>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((float)checked(closure.Unbox<float>(left) - closure.Unbox<float>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<float>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<float>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<float>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<float>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<float>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<float>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<float>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<float>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<float>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<float>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<float>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<float>(left)));
			}
		}
		internal static class op_Double
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(double));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((double)unchecked(-closure.Unbox<double>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((double)checked(-closure.Unbox<double>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((double)unchecked(+closure.Unbox<double>(operand)));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((double)unchecked(closure.Unbox<double>(left) + closure.Unbox<double>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((double)checked(closure.Unbox<double>(left) + closure.Unbox<double>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((double)(closure.Unbox<double>(left) / closure.Unbox<double>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) == closure.Unbox<double>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) > closure.Unbox<double>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) >= closure.Unbox<double>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((double)Math.Pow(closure.Unbox<double>(left), closure.Unbox<double>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) < closure.Unbox<double>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) <= closure.Unbox<double>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((double)(closure.Unbox<double>(left) % closure.Unbox<double>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((double)unchecked(closure.Unbox<double>(left) * closure.Unbox<double>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((double)checked(closure.Unbox<double>(left) * closure.Unbox<double>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<double>(left) != closure.Unbox<double>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((double)unchecked(closure.Unbox<double>(left) - closure.Unbox<double>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((double)checked(closure.Unbox<double>(left) - closure.Unbox<double>(right)));
			}

			public static object ToObject(Closure closure, object left, object _)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<double>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<double>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<double>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<double>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<double>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<double>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<double>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<double>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<double>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<double>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<double>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<double>(left)));
			}
		}
		internal static class op_Decimal
		{
			public static object Default(Closure closure)
			{
				return closure.Box(default(decimal));
			}
			public static object Negate(Closure closure, object operand)
			{
				return closure.Box((decimal)unchecked(-closure.Unbox<decimal>(operand)));
			}
			public static object NegateChecked(Closure closure, object operand)
			{
				return closure.Box((decimal)checked(-closure.Unbox<decimal>(operand)));
			}
			public static object UnaryPlus(Closure closure, object operand)
			{
				return closure.Box((decimal)unchecked(+closure.Unbox<decimal>(operand)));
			}
			public static object Add(Closure closure, object left, object right)
			{
				return closure.Box((decimal)unchecked(closure.Unbox<decimal>(left) + closure.Unbox<decimal>(right)));
			}
			public static object AddChecked(Closure closure, object left, object right)
			{
				return closure.Box((decimal)checked(closure.Unbox<decimal>(left) + closure.Unbox<decimal>(right)));
			}
			public static object Divide(Closure closure, object left, object right)
			{
				return closure.Box((decimal)(closure.Unbox<decimal>(left) / closure.Unbox<decimal>(right)));
			}
			public static object Equal(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) == closure.Unbox<decimal>(right)));
			}
			public static object GreaterThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) > closure.Unbox<decimal>(right)));
			}
			public static object GreaterThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) >= closure.Unbox<decimal>(right)));
			}
			public static object Power(Closure closure, object left, object right)
			{
				return closure.Box((decimal)Math.Pow((double)closure.Unbox<decimal>(left), closure.Unbox<double>(right)));
			}
			public static object LessThan(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) < closure.Unbox<decimal>(right)));
			}
			public static object LessThanOrEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) <= closure.Unbox<decimal>(right)));
			}
			public static object Modulo(Closure closure, object left, object right)
			{
				return closure.Box((decimal)(closure.Unbox<decimal>(left) % closure.Unbox<decimal>(right)));
			}
			public static object Multiply(Closure closure, object left, object right)
			{
				return closure.Box((decimal)unchecked(closure.Unbox<decimal>(left) * closure.Unbox<decimal>(right)));
			}
			public static object MultiplyChecked(Closure closure, object left, object right)
			{
				return closure.Box((decimal)checked(closure.Unbox<decimal>(left) * closure.Unbox<decimal>(right)));
			}
			public static object NotEqual(Closure closure, object left, object right)
			{
				return closure.Box((bool)(closure.Unbox<decimal>(left) != closure.Unbox<decimal>(right)));
			}
			public static object Subtract(Closure closure, object left, object right)
			{
				return closure.Box((decimal)unchecked(closure.Unbox<decimal>(left) - closure.Unbox<decimal>(right)));
			}
			public static object SubtractChecked(Closure closure, object left, object right)
			{
				return closure.Box((decimal)checked(closure.Unbox<decimal>(left) - closure.Unbox<decimal>(right)));
			}

			public static object ToObject(Closure closure, object left, object isChecked)
			{
				return closure.Box(left);
			}
			public static object ToSByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((sbyte)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((sbyte)closure.Unbox<decimal>(left)));
			}
			public static object ToByte(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((byte)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((byte)closure.Unbox<decimal>(left)));
			}
			public static object ToInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((short)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((short)closure.Unbox<decimal>(left)));
			}
			public static object ToUInt16(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ushort)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((ushort)closure.Unbox<decimal>(left)));
			}
			public static object ToInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((int)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((int)closure.Unbox<decimal>(left)));
			}
			public static object ToUInt32(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((uint)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((uint)closure.Unbox<decimal>(left)));
			}
			public static object ToInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((long)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((long)closure.Unbox<decimal>(left)));
			}
			public static object ToUInt64(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((ulong)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((ulong)closure.Unbox<decimal>(left)));
			}
			public static object ToSingle(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((float)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((float)closure.Unbox<decimal>(left)));
			}
			public static object ToDouble(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((double)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((double)closure.Unbox<decimal>(left)));
			}
			public static object ToDecimal(Closure closure, object left, object isChecked)
			{
				if (ReferenceEquals(isChecked, bool.TrueString))
					return checked(closure.Box((decimal)closure.Unbox<decimal>(left)));
				else
					return unchecked(closure.Box((decimal)closure.Unbox<decimal>(left)));
			}
		}
	}
}
