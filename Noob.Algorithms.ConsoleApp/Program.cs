// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Noob.Algorithms.ConsoleApp;

Console.WriteLine("Hello, World!");
var summary = BenchmarkRunner.Run<FactorialBenchmarks>();