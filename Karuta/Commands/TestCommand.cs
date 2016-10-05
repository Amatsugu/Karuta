using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LuminousVector.Karuta.Commands
{
	//[KarutaCommand(Name = "test")]
	class TestCommand : Command
	{
		public TestCommand() : base("test", "test")
		{

			_default = () =>
			{
				Karuta.Write((DateTime.Now - DateTime.Now.AddSeconds(25)).TotalSeconds);
				Karuta.CreateThread("testThread", () => Karuta.Write("Test")).Abort();
			};

			RegisterOption('t', t =>
			{
				int time;
				if(int.TryParse(t, out time))
				{
					Timer timer = new Timer(info =>
					{
						Karuta.Write(DateTime.Now.Second);
					}, null, 0, time);
				}
			});
		}
	}
}
