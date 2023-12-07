using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management;
using System.Text;

namespace SingletonComponentsBenchmark;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class BenchmarkRunner
{
	public uint Iterations = 10000;
	public uint Repeat = 5;
	public bool CollectBetweenCases = true;
	public bool CollectBetweenRepeat = true;
	public bool Warmup = true;
	public uint WarmupCycles = 3;
	public SortMode Sort = SortMode.Memory;
	public char GroupSeparator = '/';
	readonly List<Case> _casesList = new();
	Case[]? _cases;
	public BenchmarkRunner AddCase(Action action, string name = "", Action? prepare = null)
	{
		_casesList.Add(new(action, prepare, string.IsNullOrWhiteSpace(name) ? action.Method.Name : name, new long[Repeat],
			new TimeSpan[Repeat]));
		return this;
	}

	public BenchmarkRunner DoTest()
	{
		_cases = _casesList.ToArray();
		var casesCount = _cases.Length;
		if (Warmup)
		{
			for (var i = 0; i < WarmupCycles; i++)
			{
				for (var c = 0; c < casesCount; c++)
				{
					_cases[c].Prepare?.Invoke();
					_cases[c].Action();
				}
			}
		}

		GC.Collect();
		var stopwatch = new Stopwatch();
		for (var c = 0; c < casesCount; c++)
		{
			var currentCase = _cases[c];
			if (CollectBetweenCases)
			{
				GC.Collect();
			}

			for (var r = 0; r < Repeat; r++)
			{
				stopwatch.Stop();
				_cases[c].Prepare?.Invoke();
				var startMemory = GC.GetTotalMemory(CollectBetweenRepeat);
				stopwatch.Reset();
				stopwatch.Start();
				for (var i = 0; i < Iterations; i++)
				{
					currentCase.Action();
				}

				stopwatch.Stop();
				var endMemory = GC.GetTotalMemory(false);
				currentCase.Memory[r] = endMemory-startMemory;
				currentCase.Time[r]   = stopwatch.Elapsed;
			}
		}

		return this;
	}

	public void DumpResults(TextWriter writer)
	{
		var outputBuilder = new StringBuilder();
		DumpMachineInfo(outputBuilder);

		DumpGroup(_casesList, outputBuilder);

		writer.Write(outputBuilder.ToString());
	}

	void DumpGroup(IEnumerable<Case> cases, StringBuilder outputBuilder, int depth = 0)
	{
		var groups = cases.GroupBy(c =>
		{
			var splitGroups = c.Name.Split(GroupSeparator);
			if (splitGroups.Length > depth+1)
			{
				return splitGroups[depth];
			}
			return string.Empty;
		});

		IEnumerable<Case> defaultGroup = groups.Where(g => string.IsNullOrEmpty(g.Key)).SelectMany(g => g).ToArray();
		if (defaultGroup.Any())
		{
			var groupCases = defaultGroup;
			if (Sort == SortMode.Memory)
			{
				groupCases = defaultGroup.OrderBy(c => c.Memory.Max());
			}
			else if (Sort == SortMode.Time)
			{
				groupCases = defaultGroup.OrderBy(c => c.Time.Max(t => t.Ticks));
			}

			var groupMemoryAvg    = long.MaxValue;
			var groupTimeAvgTicks = long.MaxValue;
			foreach (var currentCase in groupCases)
			{
				currentCase.CalculateStats();
				if (groupMemoryAvg > currentCase.MemoryAvg)
				{
					groupMemoryAvg = currentCase.MemoryAvg;
				}
				if (groupTimeAvgTicks > currentCase.TimeAvg.Ticks)
				{
					groupTimeAvgTicks = currentCase.TimeAvg.Ticks;
				}
			}

			var splittedName = groupCases.First().Name.Split(GroupSeparator).ToArray();
			var groupName    = string.Join(GroupSeparator.ToString(), splittedName, 0, splittedName.Length-1);
			if (string.IsNullOrWhiteSpace(groupName))
			{
				groupName = "Default group";
			}
			outputBuilder.AppendLine();
			outputBuilder.Append("==== ");
			outputBuilder.Append(groupName);
			outputBuilder.Append(" ====");
			outputBuilder.Append(HumanReadableTimeSpan(TimeSpan.FromTicks(groupTimeAvgTicks)));
			outputBuilder.Append(" - ");
			outputBuilder.Append(HumanReadableBytes(groupMemoryAvg));
			outputBuilder.AppendLine();
			foreach (var currentCase in groupCases)
			{
				outputBuilder.Append(" -- ");
				outputBuilder.Append(currentCase.Name.Split(GroupSeparator).Last());
				outputBuilder.Append(" - ");
				outputBuilder.Append("Time: ");
				outputBuilder.Append(HumanReadableTimeSpan(currentCase.TimeAvg));
				outputBuilder.Append(" (+/- ");
				outputBuilder.Append(HumanReadableTimeSpan(currentCase.TimeStdDev));
				outputBuilder.Append(") -- ");
				outputBuilder.Append(" Memory: ");
				outputBuilder.Append(HumanReadableBytes(currentCase.MemoryAvg));
				outputBuilder.Append(" (+/- ");
				outputBuilder.Append(HumanReadableBytes(currentCase.MemoryStdDev));
				outputBuilder.Append(")");
				outputBuilder.AppendLine();
				for (var index = 0; index < currentCase.Memory.Length; index++)
				{
					var currentCaseMemory = currentCase.Memory[index];
					var currentCaseTime   = currentCase.Time[index];

					var timeAvgDiff   = (double)(currentCaseTime.Ticks-groupTimeAvgTicks)/groupTimeAvgTicks*100;
					var memoryAvgDiff = (double)(currentCaseMemory-groupMemoryAvg)/groupMemoryAvg*100;
					if (double.IsNaN(memoryAvgDiff))
					{
						memoryAvgDiff = 0;
					}

					outputBuilder.Append(index);
					outputBuilder.Append(". ");
					outputBuilder.Append(HumanReadableTimeSpan(currentCaseTime));
					outputBuilder.Append(" (");
					outputBuilder.Append(timeAvgDiff.ToString("+0.00;-0.00;0.00"));
					outputBuilder.Append("%)");
					outputBuilder.Append("  --  ");
					outputBuilder.Append(HumanReadableBytes(currentCaseMemory));
					outputBuilder.Append(" (");
					outputBuilder.Append(memoryAvgDiff.ToString("+0.00;-0.00;0.00"));
					outputBuilder.Append("%)");
					outputBuilder.AppendLine();
				}
			}
		}

		foreach (var group in groups.Where(g => !string.IsNullOrEmpty(g.Key)))
		{
			DumpGroup(group, outputBuilder, depth+1);
		}
	}

	static void DumpMachineInfo(StringBuilder outputBuilder)
	{
		outputBuilder.Append(" - Machine Info:\t");

		var systemInfo = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
		foreach (ManagementObject obj in systemInfo.Get())
		{
			outputBuilder.Append(obj["Caption"]);
			outputBuilder.Append(" (");
			outputBuilder.Append(obj["Version"]);
			outputBuilder.Append(")");
		}
		outputBuilder.AppendLine();
		var processorInfo = new ManagementObjectSearcher("select * from Win32_Processor");
		foreach (ManagementObject obj in processorInfo.Get())
		{

			outputBuilder.Append(obj["Name"]);
			outputBuilder.Append(" - Logic cores: ");
			outputBuilder.Append(obj["NumberOfLogicalProcessors"]);
			outputBuilder.Append(" - Phisic cores: ");
			outputBuilder.Append(obj["NumberOfEnabledCore"]);
			outputBuilder.Append(" of ");
			outputBuilder.Append(obj["NumberOfCores"]);
		}
		var ramInfo = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
		foreach (ManagementObject result in ramInfo.Get())
		{
			outputBuilder.Append("\t RAM: ");
			var totalRam = (long)(ulong)result["TotalVisibleMemorySize"]*1000;
			var freeRam  = (long)(ulong)result["FreePhysicalMemory"]*1000;
			outputBuilder.Append(HumanReadableBytes(freeRam));
			outputBuilder.Append("/");
			outputBuilder.Append(HumanReadableBytes(totalRam));
		}

		outputBuilder.AppendLine();
		outputBuilder.AppendLine();
	}

	static string HumanReadableTimeSpan(TimeSpan timeSpan)
	{
		if (timeSpan.Ticks == 0)
		{
			return "0 ns";
		}
		var sb = new StringBuilder();

		AddActionToSb(timeSpan.Seconds, new("s"), 1, true);
		AddActionToSb(timeSpan.Milliseconds, new("ms"), 3, false);
		AddActionToSb((long)Convert.ToUInt64((int)((timeSpan.TotalMilliseconds-(int)timeSpan.TotalMilliseconds)*1000)),
			new("µs"), 3, false);
		AddActionToSb((long)Convert.ToUInt64((decimal)(timeSpan.Ticks*100)%1000), new("ns"), 3,
			false);
		return sb.ToString().TrimStart();

		void AddActionToSb(long val, StringBuilder displayunit, int zeroplaces, bool skipZero)
		{
			if (val > 0 || !skipZero)
			{
				sb.AppendFormat(" {0:DZ}X".Replace("X", displayunit.ToString()).Replace("Z", zeroplaces.ToString()),
					val);
			}
		}
	}

	static string HumanReadableBytes(long byteCount)
	{
		string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB", }; //Longs run out around EB
		if (byteCount == 0)
		{
			return "0"+suf[0];
		}
		var bytes = Math.Abs(byteCount);
		var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
		var num   = Math.Round(bytes/Math.Pow(1024, place), 1);
		return Math.Sign(byteCount)*num+suf[place];
	}

	class Case
	{
		public readonly Action Action;
		public readonly Action? Prepare;
		public readonly string Name;
		public readonly long[] Memory;
		public readonly TimeSpan[] Time;

		public long MemoryAvg;
		public long MemoryMin;
		public long MemoryMax;
		public long MemoryStdDev;

		public TimeSpan TimeAvg;
		public TimeSpan TimeMin;
		public TimeSpan TimeMax;
		public TimeSpan TimeStdDev;

		public Case(Action action, Action? prepare, string name, long[] memory, TimeSpan[] time)
		{
			Action = action;
			Prepare = prepare;
			Name   = name;
			Memory = memory;
			Time   = time;
		}

		public void CalculateStats()
		{
			MemoryMin    = Memory.Min();
			MemoryMax    = Memory.Max();
			MemoryAvg    = (long)Memory.Average();
			MemoryStdDev = (long)Math.Sqrt(Memory.Sum(m => (m-MemoryAvg)*(m-MemoryAvg))/(Memory.Length-1));

			TimeMin = TimeSpan.FromTicks(Time.Min(t => t.Ticks));
			TimeMax = TimeSpan.FromTicks(Time.Max(t => t.Ticks));
			TimeAvg = TimeSpan.FromTicks((long)Time.Average(t => t.Ticks));
			var timeAvgTicks = TimeAvg.Ticks;
			TimeStdDev =
				TimeSpan.FromTicks((long)Math.Sqrt(Time.Sum(t => (t.Ticks-timeAvgTicks)*(t.Ticks-timeAvgTicks))/
				                                   (Time.Length-1)));
		}
	}

	public enum SortMode
	{
		Memory,
		Time,
		Order,
	}
}
