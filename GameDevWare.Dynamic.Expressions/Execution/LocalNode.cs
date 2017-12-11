using System;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class LocalNode : ExecutionNode
	{
		public static readonly LocalNode Operand1 = new LocalNode(LOCAL_OPERAND1);
		public static readonly LocalNode Operand2 = new LocalNode(LOCAL_OPERAND2);

		private readonly int localIndex;

		public LocalNode(int localIndex)
		{
			if (localIndex < 0) throw new ArgumentOutOfRangeException("localIndex");

			this.localIndex = localIndex;
		}

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			var constant = closure.Locals[this.localIndex];
			return constant;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.localIndex.ToString();
		}
	}
}
