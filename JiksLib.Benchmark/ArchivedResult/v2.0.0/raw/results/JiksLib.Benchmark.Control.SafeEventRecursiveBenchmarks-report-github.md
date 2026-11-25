```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
12th Gen Intel Core i5-12600KF 3.70GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=.NET 10.0  Runtime=.NET 10.0  InvocationCount=1  
UnrollFactor=1  

```
| Method                               | Mean       | Error    | StdDev   | StdErr   | Median     | Min        | Q1         | Q3         | Max        | Op/s        | Allocated |
|------------------------------------- |-----------:|---------:|---------:|---------:|-----------:|-----------:|-----------:|-----------:|-----------:|------------:|----------:|
| PublishEvent_RecursiveQueue          |   770.4 ns | 97.36 ns | 284.0 ns | 28.69 ns |   700.0 ns |   400.0 ns |   500.0 ns |   900.0 ns | 1,500.0 ns | 1,298,013.2 |         - |
| PublishEvent_DeferredListenerChanges | 1,412.0 ns | 38.10 ns | 101.7 ns | 11.16 ns | 1,400.0 ns | 1,100.0 ns | 1,400.0 ns | 1,500.0 ns | 1,600.0 ns |   708,191.1 |         - |
| PublishEvent_RecursiveChain          | 1,153.8 ns | 49.67 ns | 139.3 ns | 14.60 ns | 1,100.0 ns |   900.0 ns | 1,050.0 ns | 1,200.0 ns | 1,600.0 ns |   866,666.7 |         - |
