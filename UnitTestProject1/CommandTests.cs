using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LuminousVector.Karuta.Commands;
using System.Diagnostics;

namespace LuminousVector.Karuta.Tests
{
	[TestClass]
	public class CommandTests
	{
		private bool _defaultCalled = false;
		private static bool _goCalled = false;
		private static bool _rToggle = false;
		private static string _inputA = null;
		private static string _inputD = null;
		private static string _inputB = null;

		private ICommand testCommand;
		private List<string> args;

		[TestMethod]
		public void TestOptions()
		{
			//Prepare Test
			reset();
			string input = "-ra test";
			args.AddRange(input.SplitPreserveGrouping());
			//Run Test
			testCommand.Parse(args);
			//Assert Results
			Assert.IsTrue(_rToggle, "Option r triggered");
			Assert.AreEqual(_inputA, "test", false, "Option a set");
			Assert.IsTrue(_defaultCalled, "Default action not executed");
		}

		[TestMethod]
		public void TestKeyword()
		{
			//Prepare Test
			reset();
			string input = "go";
			args.AddRange(input.SplitPreserveGrouping());
			//Run Test
			testCommand.Parse(args);
			//Asert Results
			Assert.IsTrue(_goCalled, "Go Called");
		}

		[TestMethod]
		public void TestBoth()
		{
			//Prepare Test
			reset();
			string input = "go -ra test";
			args.AddRange(input.SplitPreserveGrouping());
			//Run Test
			testCommand.Parse(args);
			//Assert Results
			Assert.IsTrue(_rToggle, "Option r triggered");
			Assert.AreEqual(_inputA, "test", false, "Option a set");
			Assert.IsTrue(_goCalled, "Go Called");
		}

		[TestMethod]
		public void TestQuotes()
		{
			//Prepare Test
			reset();
			string input = "-a \"This is a quote\"";
			args.AddRange(input.SplitPreserveGrouping());
			//Run Test
			testCommand.Parse(args);
			//Assert Results
			Assert.AreEqual(_inputA, "This is a quote", false, $"Quote parsed {_inputA}");
			Assert.IsTrue(_defaultCalled, "Default called");
		}

		[TestMethod]
		public void TestMultiQuotes()
		{
			//Prepare Test
			reset();
			string input = "-ad \"This is a quote\" \"This one too\"";
			args.AddRange(input.SplitPreserveGrouping());
			//Run Test
			testCommand.Parse(args);
			//Assert Results
			Assert.AreEqual(_inputA, "This is a quote", false, "Quote one parsed");
			Assert.AreEqual(_inputD, "This one too", false, "Quote two parsed");
			Assert.IsTrue(_defaultCalled, "Default called");
		}

		[TestMethod]
		public void TestAll()
		{
			//Prepare Test
			reset();
			string input = "go -abrd \"quote\" hello \"This one too\"";
			args.AddRange(input.SplitPreserveGrouping());
			//Run Test
			testCommand.Parse(args);
			//Assert Results
			Assert.IsTrue(_rToggle, "Option r triggered");
			Assert.AreEqual(_inputA, "quote", false, "Quote one parsed");
			Assert.AreEqual(_inputB, "hello", false, "text parsed");
			Assert.AreEqual(_inputD, "This one too", false, "Quote two parsed");
			Assert.IsTrue(_goCalled, "Go Called");
		}

		void reset()
		{
			_defaultCalled = _goCalled = _rToggle = false;
			_inputA = _inputD = _inputB = null;
			testCommand = new TestCommand("test", Deafult);
			args = new List<string>();
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

		public static void OptionB(string input)
		{
			_inputB = input;
		}

		public static void OptionD(string input)
		{
			_inputD = input;
		}

		public static void Go()
		{
			_goCalled = true;
		}
	}

	class TestCommand : Command
	{
		public TestCommand(string name, Action defaultAction) : base(name, defaultAction, "test command")
		{ 
			RegisterOption('r', CommandTests.OptionR);
			RegisterOption('a', CommandTests.OptionA);
			RegisterOption('d', CommandTests.OptionD);
			RegisterOption('b', CommandTests.OptionB);
			RegisterKeyword("go", CommandTests.Go);
		}
	}
}
