﻿using SingletonComponentsBenchmark;
using SingletonComponentsBenchmark.Components;

SingletonStorageSlow storageSlow = null;

void PrepareSlow()
{
	storageSlow = new SingletonStorageSlow(100);
	storageSlow.Add(new Component1());
	storageSlow.Add(new Component1());
	storageSlow.Add(new Component1());
	storageSlow.Add(new Component2());
	storageSlow.Add(new Component3());
	storageSlow.Add(new Component4());
	storageSlow.Add(new Component5());
	storageSlow.Add(new Component6());
	storageSlow.Add(new Component7());
	storageSlow.Add(new Component8());
	storageSlow.Add(new Component9());
	storageSlow.Add(new Component10());
	storageSlow.Add(new Component11());
	storageSlow.Add(new Component12());
}

new BenchmarkRunner
	{
		Iterations = 100_000,
		Sort       = BenchmarkRunner.SortMode.Time,
		WarmupCycles = 2,
	}
	// Add
	.AddCase(() =>
	{
		storageSlow.Add(new Component1());
		storageSlow.Add(new Component1());
		storageSlow.Add(new Component1());
		storageSlow.Add(new Component2());
		storageSlow.Add(new Component3());
		storageSlow.Add(new Component4());
		storageSlow.Add(new Component5());
		storageSlow.Add(new Component6());
		storageSlow.Add(new Component7());
		storageSlow.Add(new Component8());
		storageSlow.Add(new Component9());
		storageSlow.Add(new Component10());
		storageSlow.Add(new Component11());
		storageSlow.Add(new Component12());
	}, "Add/Slow", () => storageSlow = new SingletonStorageSlow(100))
	// .AddCase(() =>
	// {
	// 	storageFast.Add(new Component1());
	// 	storageFast.Add(new Component1());
	// 	storageFast.Add(new Component1());
	// 	storageFast.Add(new Component2());
	// 	storageFast.Add(new Component3());
	// 	storageFast.Add(new Component4());
	// 	storageFast.Add(new Component5());
	// 	storageFast.Add(new Component6());
	// 	storageFast.Add(new Component7());
	// 	storageFast.Add(new Component8());
	// 	storageFast.Add(new Component9());
	// 	storageFast.Add(new Component10());
	// 	storageFast.Add(new Component11());
	// 	storageFast.Add(new Component12());
	// }, "Add/Fast", () => storageFast = new SingletonStorageFast(100))
	// Get value
	.AddCase(() =>
	{
		var acc = 0;
		acc += storageSlow.Value<Component1>().value;
		acc += storageSlow.Value<Component2>().value;
		acc += storageSlow.Value<Component3>().value;
		acc += storageSlow.Value<Component4>().value;
		acc += storageSlow.Value<Component5>().value;
		acc += storageSlow.Value<Component6>().value;
	}, "Get/Slow", PrepareSlow)
	// Has
	.AddCase(() =>
	{
		storageSlow.Has<Component1>();
		storageSlow.Has<Component2>();
		storageSlow.Has<Component3>();
		storageSlow.Has<Component4>();
		storageSlow.Has<Component5>();
		storageSlow.Has<Component6>();
		storageSlow.Has<Component7>();

		storageSlow.Has<Component21>();
		storageSlow.Has<Component22>();
		storageSlow.Has<Component23>();
		storageSlow.Has<Component24>();
		storageSlow.Has<Component25>();
		storageSlow.Has<Component26>();
		storageSlow.Has<Component27>();
	}, "Has/Slow", PrepareSlow)
	// Update
	.AddCase(() =>
	{
		var cmp1 = storageSlow.Value<Component1>();
		cmp1.value++;
		storageSlow.Add(cmp1);

		var cmp2 = storageSlow.Value<Component2>();
		cmp2.value += 2;
		storageSlow.Add(cmp2);

		var cmp3 = storageSlow.Value<Component3>();
		cmp3.value += 3;
		storageSlow.Add(cmp3);

		var cmp4 = storageSlow.Value<Component4>();
		cmp4.value += 4;
		storageSlow.Add(cmp4);

		var cmp5 = storageSlow.Value<Component5>();
		cmp5.value += 5;
		storageSlow.Add(cmp5);
	}, "Update/Slow", PrepareSlow)
	// Remove
	.AddCase(() =>
	{
		storageSlow.Remove<Component1>();
		storageSlow.Remove<Component2>();
		storageSlow.Remove<Component3>();
		storageSlow.Remove<Component4>();
		storageSlow.Remove<Component5>();
		storageSlow.Remove<Component6>();
		storageSlow.Remove<Component7>();

		storageSlow.Remove<Component21>();
		storageSlow.Remove<Component22>();
		storageSlow.Remove<Component23>();
		storageSlow.Remove<Component24>();
		storageSlow.Remove<Component25>();
		storageSlow.Remove<Component26>();
		storageSlow.Remove<Component27>();
	}, "Remove/Slow", PrepareSlow)
	.DoTest()
	.DumpResults(Console.Out);