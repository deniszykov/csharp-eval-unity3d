using System;
using System.Collections.Generic;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class Metadata
	{
		private static readonly Dictionary<TypeTuple2, TypeConversion> Conversions = new Dictionary<TypeTuple2, TypeConversion>(EqualityComparer<TypeTuple2>.Default);
		private static readonly Dictionary<Type, TypeDescription> Types = new Dictionary<Type, TypeDescription>();

		public static TypeDescription GetTypeDescription(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var typeDescription = default(TypeDescription);
			lock (Types)
			{
				if (Types.TryGetValue(type, out typeDescription))
					return typeDescription;
			}

			var underlyingType = default(Type);
			if (type.IsEnum)
				underlyingType = Enum.GetUnderlyingType(type);
			else if (type.IsValueType)
				underlyingType = Nullable.GetUnderlyingType(type);

			var baseTypeDescription = type.BaseType != null ? GetTypeDescription(type.BaseType) : null;
			var underlyingTypeDescription = underlyingType != null ? GetTypeDescription(underlyingType) : null;
			var interfaceDescriptions = Array.ConvertAll(type.GetInterfaces(), GetTypeDescription);
			var genericArgumentsDescriptions = type.IsGenericType ? Array.ConvertAll(type.GetGenericArguments(), GetTypeDescription) : TypeDescription.EmptyTypes;

			var newTypeDescription = new TypeDescription(type, baseTypeDescription, underlyingTypeDescription, interfaceDescriptions, genericArgumentsDescriptions);

			lock (Types)
			{
				if (Types.TryGetValue(type, out typeDescription))
					return typeDescription;
				else
					Types.Add(type, newTypeDescription);
			}

			UpdateConversions(newTypeDescription);

			return newTypeDescription;
		}

		public static bool TryGetTypeConversion(Type fromType, Type toType, out TypeConversion typeConversion)
		{
			var key = new TypeTuple2(fromType, toType);
			lock (Conversions)
				return Conversions.TryGetValue(key, out typeConversion);
		}

		private static void UpdateConversions(TypeDescription typeDescription)
		{
			lock (Conversions)
			{
				foreach (var conversionMethod in typeDescription.Conversions)
				{
					var key = new TypeTuple2(conversionMethod.GetParameter(0).ParameterType, conversionMethod.GetParameter(-1).ParameterType);

					var explicitConversionMethod = conversionMethod.IsImplicitOperator ? default(MemberDescription) : conversionMethod;
					var implicitConversionMethod = conversionMethod.IsImplicitOperator ? conversionMethod : default(MemberDescription);
					var conversion = default(TypeConversion);
					var newConversion = default(TypeConversion);
					var cost = conversionMethod.IsImplicitOperator ? TypeConversion.QUALITY_IMPLICIT_CONVERSION : TypeConversion.QUALITY_EXPLICIT_CONVERSION;
					if (Conversions.TryGetValue(key, out conversion) == false)
						newConversion = new TypeConversion(cost, false, implicitConversionMethod, explicitConversionMethod);
					else
						newConversion = conversion.Expand(implicitConversionMethod, explicitConversionMethod);

					if (newConversion != conversion)
						Conversions[key] = newConversion;
				}

				foreach (var baseType in typeDescription.BaseTypes)
				{
					var key = new TypeTuple2(typeDescription, baseType);
					var cost = baseType == typeDescription ? TypeConversion.QUALITY_SAME_TYPE : TypeConversion.QUALITY_INHERITANCE_HIERARCHY;
					var conversion = default(TypeConversion);
					if (Conversions.TryGetValue(key, out conversion))
						conversion = new TypeConversion(cost, true, conversion.Implicit, conversion.Explicit);
					else
						conversion = new TypeConversion(cost, true, null, null);
				}

				foreach (var baseType in typeDescription.Interfaces)
				{
					var key = new TypeTuple2(typeDescription, baseType);
					var cost = baseType == typeDescription ? TypeConversion.QUALITY_SAME_TYPE : TypeConversion.QUALITY_INHERITANCE_HIERARCHY;
					var conversion = default(TypeConversion);
					if (Conversions.TryGetValue(key, out conversion))
						conversion = new TypeConversion(cost, true, conversion.Implicit, conversion.Explicit);
					else
						conversion = new TypeConversion(cost, true, null, null);
				}

				if (typeDescription.IsEnum || typeDescription.IsNullable)
				{
					var fromEnumKey = new TypeTuple2(typeDescription, typeDescription.UnderlyingType);
					var toEnumKey = new TypeTuple2(typeDescription.UnderlyingType, typeDescription);

					Conversions[fromEnumKey] = new TypeConversion(TypeConversion.QUALITY_IN_PLACE_CONVERSION, true);
					Conversions[toEnumKey] = new TypeConversion(TypeConversion.QUALITY_IN_PLACE_CONVERSION, true);
				}
			}
		}
	}
}
