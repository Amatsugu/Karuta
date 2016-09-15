using System;

namespace LuminousVector.Karuta.Commands
{
	[AttributeUsage(AttributeTargets.Class)]
	public class KarutaCommand : Attribute
	{
		public string Name { get; set; }
	}
}
