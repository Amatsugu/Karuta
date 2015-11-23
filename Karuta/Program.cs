using System;
using System.Windows.Forms;

namespace com.LuminousVector.Karuta
{
	static class Program
	{
		//[STAThread]
		static void Main()
		{
			Karuta karuta = new Karuta();
			karuta.Run();
		}
	}
}
