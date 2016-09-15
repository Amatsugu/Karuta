using System.Collections.Generic;

namespace LuminousVector.Karuta.Commands
{
	public interface ICommand
	{

		ICommand Parse(List<string> args);

		ICommand Execute();

		void Stop();

	}
}
