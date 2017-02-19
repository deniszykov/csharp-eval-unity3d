using System;

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

		public readonly float Quality;
		public readonly bool IsNatural;
		public readonly MemberDescription Implicit;
		public readonly MemberDescription Explicit;

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

			var newCost = Math.Max(this.Quality, this.Implicit != null ? QUALITY_IMPLICIT_CONVERSION : QUALITY_EXPLICIT_CONVERSION);
			return new TypeConversion(newCost, this.IsNatural, implicitConversion, explicitConversion);
		}
	}
}
