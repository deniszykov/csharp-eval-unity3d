using System;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions
{
	public static class DelegateUtils
	{
		public static Delegate CreateDelegate(Type delegateType, MethodInfo method, bool throwOnBindingFailure = true)
		{
			if (delegateType == null) throw new ArgumentNullException(nameof(delegateType));
			if (method == null) throw new ArgumentNullException(nameof(method));

#if NETSTANDARD
			try
			{
				return method.CreateDelegate(delegateType);
			}
			catch
			{
				if (throwOnBindingFailure)
					throw;

				return null;
			}
#else
			return Delegate.CreateDelegate(delegateType, method, throwOnBindingFailure);
#endif
		}
#if NETSTANDARD
		public static MethodInfo GetMethodInfo(this Delegate delegateInstance)
		{
			if (delegateInstance == null) throw new ArgumentNullException(nameof(delegateInstance));

			return RuntimeReflectionExtensions.GetMethodInfo(delegateInstance);
		}
#else
		public static MethodInfo GetMethodInfo(this Delegate delegateInstance)
		{
			if (delegateInstance == null) throw new ArgumentNullException("delegateInstance");

			return delegateInstance.Method;
		}
#endif
	}
}
