#region

using System;
using System.Collections.Generic;
using HearthstoneReplays;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace HearthstoneReplayTests
{
	[TestClass]
	public class GameEventsTest
	{
		[TestMethod]
		public void Test()
		{
			List<string> logFile = TestDataReader.GetInputFile("Power_2.log.txt");
			ReplayParser parser = new ReplayParser();
			parser = new ReplayParser();
			parser.Init((call) => Console.WriteLine(call));

			foreach (string logLine in logFile)
			{
				parser.ReadLine(logLine);
			}
		}
	}
}
