using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace LuminousVector.Karuta
{
	static class Program
	{
		//#region Trap application termination
		//[DllImport("Kernel32")]
		//private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

		//private delegate bool EventHandler(CtrlType sig);
		//static EventHandler _handler;

		//enum CtrlType
		//{
		//	CTRL_C_EVENT = 0,
		//	CTRL_BREAK_EVENT = 1,
		//	CTRL_CLOSE_EVENT = 2,
		//	CTRL_LOGOFF_EVENT = 5,
		//	CTRL_SHUTDOWN_EVENT = 6
		//}

		//private static bool Handler(CtrlType sig)
		//{
		//	//Save data and clean up
		//	Karuta.Close();
		//	//Close 
		//	Environment.Exit(-1);
		//	return true;
		//}
		//#endregion

		static void Main()
		{
			//_handler += new EventHandler(Handler);
			//SetConsoleCtrlHandler(_handler, true);
			Thread.CurrentThread.Name = "Karuta.Main";
			Karuta.Run();
		}
	}
}
