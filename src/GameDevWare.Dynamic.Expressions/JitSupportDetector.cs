using System;
#if !NETSTANDARD1_3
using System.Reflection.Emit;
#endif

namespace GameDevWare.Dynamic.Expressions
{
	internal static class JitSupportDetector
	{
		private static bool? IsDynamicCompilationResult;

		public static bool IsDynamicCompilationAvailable()
		{
			if (AotCompilation.IsAotRuntime) return false;

			if (IsDynamicCompilationResult.HasValue) return IsDynamicCompilationResult.Value;

			try
			{
#if NETSTANDARD1_3
				// check lambdas are supported
				System.Linq.Expressions.Expression.Lambda<Func<bool>>(System.Linq.Expressions.Expression.Constant(true)).Compile();
#else

				// check dynamic methods are supported
				var voidDynamicMethod = new DynamicMethod("TestVoidMethod", typeof(void), Type.EmptyTypes, true);
				var il = voidDynamicMethod.GetILGenerator();
				il.Emit(OpCodes.Nop);
				voidDynamicMethod.CreateDelegate(typeof(Action));
#endif
				IsDynamicCompilationResult = true;
			}
			catch (Exception)
			{
				IsDynamicCompilationResult = false;
			}

			return IsDynamicCompilationResult.Value;
		}
	}
}
