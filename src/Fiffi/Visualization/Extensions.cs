﻿using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiffi.Visualization
{
	public static class Extensions
	{
		public static void Then(this TestContext context, Action<IEvent[], string> f)
			=> context.Then(events =>
		{
			var t = events.Draw();
			f(events, t);
		});

		public static string Draw(this IEvent[] events)
		{
			var table = new AsciiTable();
			table.Columns.Add(new AsciiColumn("Flow", 15));
			table.Columns.Add(new AsciiColumn("Waterfall", 65));
			table.Columns.Add(new AsciiColumn("Time", 4));
			table.Columns.Add(new AsciiColumn("Aggregate", 16));

			var blocks = events.BuildBlocks();

			var total = events.GroupBy(x => x.OccuredAt().ToString()).Count() * 2;

			blocks.ForEach((x, i) =>
				table.Rows.Add(new List<string> { x.Name, DrawBar(0, total, x.Time, x.Time + 1, '\u2593', '\u2591', 60), x.Time.ToString(), x.AggregateId })
			);

			return table.ToString();
		}

		static IEnumerable<(string Name, int Time, string AggregateId)> BuildBlocks(this IEvent[] events)
		{
			var g = events.GroupBy(x => $"{x.GetTrigger()} : {x.GetTriggerId()}");
			var blocks = new List<(string, int, string)>();

			var commandPosition = 0;
			var lastOccured = string.Empty;

			g.ForEach((x, i) =>
			{
				var occured = x.First().OccuredAt().ToString();
				commandPosition = lastOccured == occured ? commandPosition : i * 2;
				lastOccured = occured;
				blocks.Add((x.Key.Split(':')[0].Trim(), commandPosition, x.First().SourceId.ToString()));
				blocks.AddRange(x.Select(e => (e.GetType().Name, commandPosition + 1, e.SourceId.ToString())));
			});

			return blocks;
		}


		static string DrawBar(
			int totalStart, int totalEnd,
			int barStart, int barEnd,
			char barFilling, char backgroundFilling,
			int length)
		{
			var total = totalEnd - totalStart;
			var startIndex = (int)Math.Round((barStart - totalStart) / (decimal)total * length, 0);
			var endIndex = (int)Math.Round((barEnd - totalStart) / (decimal)total * length, 0);

			var before = new string(backgroundFilling, Math.Max(startIndex, 0));
			var bar = new string(barFilling, Math.Max(endIndex - startIndex, 0));
			var returnValue = (before + bar).PadRight(length, backgroundFilling);
			return returnValue;
		}

		static string DrawTimeBar(
			DateTime totalStart, DateTime totalEnd,
			DateTime barStart, DateTime barEnd,
			char barFilling, char backgroundFilling,
			int length)
		{
			var total = totalEnd.Ticks - totalStart.Ticks;
			var startIndex = (int)Math.Round((barStart.Ticks - totalStart.Ticks) / (decimal)total * length, 0);
			var endIndex = (int)Math.Round((barEnd.Ticks - totalStart.Ticks) / (decimal)total * length, 0);

			var before = new string(backgroundFilling, Math.Max(startIndex, 0));
			var bar = new string(barFilling, Math.Max(endIndex - startIndex, 0));
			return (before + bar).PadRight(length, backgroundFilling);
		}
	}
}