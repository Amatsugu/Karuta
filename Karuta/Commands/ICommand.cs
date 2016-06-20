using System.Collections.Generic;

namespace com.LuminousVector.Karuta.Commands
{
	public interface ICommand
	{
		ICommand Pharse(List<string> args);

		ICommand Execute();

		void Stop();

	}
}
