using System;
using System.Threading;
using System.Windows.Forms;

namespace com.LuminousVector.Karuta
{
	static class Program
	{
		//[STAThread]
		static void Main()
		{
			Thread.CurrentThread.Name = "Karuta.Main";
			Karuta.Run();
		}

		static void OnProcessExit()
		{
			Karuta.Close();
		}
	}
}
