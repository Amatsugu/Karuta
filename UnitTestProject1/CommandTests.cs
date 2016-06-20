using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using com.LuminousVector.Karuta.Commands;
using System.Diagnostics;

namespace com.LuminousVector.Karuta.Tests
{
	[TestClass]
	public class CommandTests
	{
		private bool _defaultCalled = false;
		private static bool _rToggle = false;
		private static string _inputA = null;

		[TestMethod]
		public void TestCommand()
		{
			ICommand testCommand = new TestCommand("test", Deafult);
			string input = "-ra test";
			List<string> args = new List<string>();
			args.AddRange(input.Split(' '));
			testCommand.Pharse(args);

			Assert.IsTrue(_rToggle, "Option r triggered");
			Assert.IsNotNull(_inputA, "Option a not set");
			Assert.IsTrue(_defaultCalled, "Default action not executed");
		}

		public void Deafult()
		{
			_defaultCalled = true;
		}

		public static void OptionR()
		{
			_rToggle = true;
		}

		public static void OptionA(string input)
		{
			_inputA = input;
		}
	}

	class TestCommand : Command
	{
		public TestCommand(string name, Action defaultAction) : base(name, defaultAction, "test command") { }

		protected override void Init()
		{
			base.Init();
			RegisterOption('r', CommandTests.OptionR);
			RegisterOption('a', CommandTests.OptionA);
		}
	}
}
