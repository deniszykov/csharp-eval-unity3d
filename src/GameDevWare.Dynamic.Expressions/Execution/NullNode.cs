namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal sealed class NullNode : ExecutionNode
	{
		public static readonly NullNode Instance = new NullNode();

		/// <inheritdoc />
		public override object Run(Closure closure)
		{
			return null;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return "null";
		}
	}
}
