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

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal sealed class TypeConversion
	{
		public const float QUALITY_SAME_TYPE = 1.0f;
		public const float QUALITY_INHERITANCE_HIERARCHY = 0.9f;
		public const float QUALITY_IN_PLACE_CONVERSION = 0.7f; // constant in-place conversion
		public const float QUALITY_IMPLICIT_CONVERSION = 0.5f; // operator
		public const float QUALITY_NUMBER_EXPANSION = 0.5f; // float to double, and int to int
		public const float QUALITY_PRECISION_CONVERSION = 0.4f; // int to float
		public const float QUALITY_EXPLICIT_CONVERSION = 0.0f;
		public const float QUALITY_NO_CONVERSION = 0.0f;

		private static readonly Dictionary<TypeTuple2, TypeConversion> Conversions;

		public readonly float Quality;
		public readonly bool IsNatural;
		public readonly MemberDescription Implicit;
		public readonly MemberDescription Explicit;

		static TypeConversion()
		{
			Conversions = new Dictionary<TypeTuple2, TypeConversion>(EqualityComparer<TypeTuple2>.Default);

			// typeof(char), typeof(string), typeof(float), typeof(double), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong)
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(float), typeof(double));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(byte), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(long), typeof(ulong), typeof(float), typeof(double));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(char), typeof(ushort), typeof(uint), typeof(int), typeof(long), typeof(ulong), typeof(float), typeof(double));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(ushort), typeof(uint), typeof(int), typeof(long), typeof(ulong), typeof(float), typeof(double));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(ulong), typeof(float), typeof(double));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(short), typeof(int), typeof(long), typeof(float), typeof(double));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(int), typeof(long), typeof(float), typeof(double));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(long), typeof(float), typeof(double));

			SetNaturalConversion(QUALITY_EXPLICIT_CONVERSION, typeof(double), typeof(float), typeof(long), typeof(ulong), typeof(int), typeof(uint), typeof(short), typeof(char), typeof(ushort), typeof(sbyte), typeof(byte));
			SetNaturalConversion(QUALITY_EXPLICIT_CONVERSION, typeof(float), typeof(long), typeof(ulong), typeof(int), typeof(uint), typeof(short), typeof(char), typeof(ushort), typeof(sbyte), typeof(byte));
			SetNaturalConversion(QUALITY_EXPLICIT_CONVERSION, typeof(byte), typeof(sbyte));
			SetNaturalConversion(QUALITY_EXPLICIT_CONVERSION, typeof(char), typeof(sbyte), typeof(byte));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(ushort), typeof(char), typeof(short), typeof(byte), typeof(sbyte));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(uint), typeof(int), typeof(char), typeof(ushort), typeof(short), typeof(byte), typeof(sbyte));
			SetNaturalConversion(QUALITY_IMPLICIT_CONVERSION, typeof(ulong), typeof(long), typeof(uint), typeof(int), typeof(char), typeof(ushort), typeof(short), typeof(byte), typeof(sbyte));
			SetNaturalConversion(QUALITY_EXPLICIT_CONVERSION, typeof(sbyte), typeof(ulong), typeof(uint), typeof(char), typeof(ushort), typeof(byte));
			SetNaturalConversion(QUALITY_EXPLICIT_CONVERSION, typeof(short), typeof(ulong), typeof(uint), typeof(char), typeof(ushort), typeof(sbyte), typeof(byte));
			SetNaturalConversion(QUALITY_EXPLICIT_CONVERSION, typeof(int), typeof(ulong), typeof(uint), typeof(short), typeof(char), typeof(ushort), typeof(sbyte), typeof(byte));
			SetNaturalConversion(QUALITY_EXPLICIT_CONVERSION, typeof(long), typeof(ulong), typeof(int), typeof(uint), typeof(short), typeof(char), typeof(ushort), typeof(sbyte), typeof(byte));

		}
		public TypeConversion(float quality, bool isNatural, MemberDescription implicitConversion = null, MemberDescription explicitConversion = null)
		{
			this.Quality = quality;
			this.IsNatural = isNatural;
			this.Implicit = implicitConversion;
			this.Explicit = explicitConversion;
		}

		public TypeConversion Expand(MemberDescription implicitConversion, MemberDescription explicitConversion)
		{
			implicitConversion = implicitConversion ?? this.Implicit;
			explicitConversion = explicitConversion ?? this.Explicit;

			if (implicitConversion == this.Implicit && explicitConversion == this.Explicit)
				return this;

			var newQuality = Math.Max(this.Quality, this.Implicit != null ? QUALITY_IMPLICIT_CONVERSION : QUALITY_EXPLICIT_CONVERSION);
			return new TypeConversion(newQuality, this.IsNatural, implicitConversion, explicitConversion);
		}

		public static bool TryGetTypeConversion(Type fromType, Type toType, out TypeConversion typeConversion)
		{
			if (fromType == null) throw new ArgumentNullException("fromType");
			if (toType == null) throw new ArgumentNullException("toType");

			var key = new TypeTuple2(fromType, toType);
			lock (Conversions)
				return Conversions.TryGetValue(key, out typeConversion);
		}

		internal static void UpdateConversions(IEnumerable<TypeDescription> typeDescriptions)
		{
			if (typeDescriptions == null) throw new ArgumentNullException("typeDescriptions");

			lock (Conversions)
			{
				foreach (var typeDescription in typeDescriptions)
				{
					foreach (var conversionMethod in typeDescription.Conversions)
					{
						var key = new TypeTuple2(conversionMethod.GetParameterType(0), conversionMethod.GetParameterType(-1));

						var explicitConversionMethod = conversionMethod.IsImplicitOperator ? default(MemberDescription) : conversionMethod;
						var implicitConversionMethod = conversionMethod.IsImplicitOperator ? conversionMethod : default(MemberDescription);
						var conversion = default(TypeConversion);
						var newConversion = default(TypeConversion);
						var cost = conversionMethod.IsImplicitOperator ? QUALITY_IMPLICIT_CONVERSION : QUALITY_EXPLICIT_CONVERSION;
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
						var cost = baseType == typeDescription ? QUALITY_SAME_TYPE : QUALITY_INHERITANCE_HIERARCHY;
						var conversion = default(TypeConversion);
						if (Conversions.TryGetValue(key, out conversion))
							conversion = new TypeConversion(cost, true, conversion.Implicit, conversion.Explicit);
						else
							conversion = new TypeConversion(cost, true, null, null);
						Conversions[key] = conversion;
					}

					foreach (var baseType in typeDescription.Interfaces)
					{
						var key = new TypeTuple2(typeDescription, baseType);
						var cost = baseType == typeDescription ? QUALITY_SAME_TYPE : QUALITY_INHERITANCE_HIERARCHY;
						var conversion = default(TypeConversion);
						if (Conversions.TryGetValue(key, out conversion))
							conversion = new TypeConversion(cost, true, conversion.Implicit, conversion.Explicit);
						else
							conversion = new TypeConversion(cost, true, null, null);
						Conversions[key] = conversion;
					}

					if (typeDescription.IsEnum)
					{
						var fromEnumKey = new TypeTuple2(typeDescription, typeDescription.UnderlyingType);
						var toEnumKey = new TypeTuple2(typeDescription.UnderlyingType, typeDescription);

						Conversions[fromEnumKey] = new TypeConversion(QUALITY_IN_PLACE_CONVERSION, true);
						Conversions[toEnumKey] = new TypeConversion(QUALITY_IN_PLACE_CONVERSION, true);
					}
				}
			}
		}
		private static void SetNaturalConversion(float quality, Type fromType, params Type[] toTypes)
		{
			if (fromType == null) throw new ArgumentNullException("fromType");
			if (toTypes == null) throw new ArgumentNullException("toTypes");

			foreach (var toType in toTypes)
			{
				var key = new TypeTuple2(fromType, toType);
				Conversions.Add(key, new TypeConversion(quality, isNatural: true));
			}
		}

		public override string ToString()
		{
			return string.Format("Quality: {0}, Is Natural: {1}, Implicit: {2}, Explicit: {3}", this.Quality.ToString(), this.IsNatural.ToString(), this.Implicit, this.Explicit);
		}
	}
}
