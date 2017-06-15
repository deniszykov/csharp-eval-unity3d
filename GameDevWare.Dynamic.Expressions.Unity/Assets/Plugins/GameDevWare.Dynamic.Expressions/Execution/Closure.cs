using System;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class Closure
	{
		public readonly object[] Constants;
		public readonly object[] Locals; // first two locals is reserved, third and others is parameters

		public Closure(object[] constants, object[] locals)
		{
			if (constants == null) throw new ArgumentNullException("constants");
			if (locals == null) throw new ArgumentNullException("locals");
			this.Constants = constants;
			this.Locals = locals;
		}

		public object Box<T>(T value)
		{
			return value;
		}

		public T Unbox<T>(object boxed)
		{
			//if (boxed is StrongBox<T>)
			//	return ((StrongBox<T>)boxed).Value;
			//else if (boxed is IStrongBox)
			//	boxed = ((IStrongBox)boxed).Value;

			if (boxed is T)
				return (T)boxed;
			else
				return (T)Convert.ChangeType(boxed, typeof(T));
		}

		public bool Is<T>(object boxed)
		{
			return boxed is T;
		}

		public Type GetType(object left)
		{
			if (left == null)
				return typeof(object);
			return left.GetType();
		}
	}
}
