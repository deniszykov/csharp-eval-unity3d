using System;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	///     Utility methods for working with delegates and reflection.
	/// </summary>
	public static class DelegateUtils
	{
		/// <summary>
		///     Creates a delegate of the specified type from the specified <see cref="MethodInfo" />.
		/// </summary>
		/// <param name="delegateType">The type of the delegate to create. Cannot be null.</param>
		/// <param name="method">The method to create the delegate for. Cannot be null.</param>
		/// <param name="throwOnBindingFailure">True to throw an exception if the delegate cannot be bound; otherwise, false.</param>
		/// <returns>The created delegate, or null if binding fails and <paramref name="throwOnBindingFailure" /> is false.</returns>
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
		/// <summary>
		///     Gets the <see cref="MethodInfo" /> associated with the specified delegate instance.
		/// </summary>
		/// <param name="delegateInstance">The delegate instance. Cannot be null.</param>
		/// <returns>The <see cref="MethodInfo" /> of the delegate.</returns>
		public static MethodInfo GetMethodInfo(this Delegate delegateInstance)
		{
			if (delegateInstance == null) throw new ArgumentNullException(nameof(delegateInstance));

			return RuntimeReflectionExtensions.GetMethodInfo(delegateInstance);
		}
#else
		/// <summary>
		///     Gets the <see cref="MethodInfo" /> associated with the specified delegate instance.
		/// </summary>
		/// <param name="delegateInstance">The delegate instance. Cannot be null.</param>
		/// <returns>The <see cref="MethodInfo" /> of the delegate.</returns>
		public static MethodInfo GetMethodInfo(this Delegate delegateInstance)
		{
			if (delegateInstance == null) throw new ArgumentNullException("delegateInstance");

			return delegateInstance.Method;
		}
#endif
	}
}
