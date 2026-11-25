```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
12th Gen Intel Core i5-12600KF 3.70GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=.NET 10.0  Runtime=.NET 10.0  InvocationCount=1  
UnrollFactor=1  

```
| Method                          | Mean     | Error     | StdDev    | StdErr    | Min      | Q1       | Median   | Q3       | Max      | Op/s      | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |---------:|----------:|----------:|----------:|---------:|---------:|---------:|---------:|---------:|----------:|------:|--------:|----------:|------------:|
| PublishWithPrewarmedCache       | 1.850 μs | 0.1559 μs | 0.4447 μs | 0.0459 μs | 1.200 μs | 1.500 μs | 1.800 μs | 2.075 μs | 3.000 μs | 540,540.5 |  1.06 |    0.35 |      32 B |        1.00 |
| PublishAfterClearTypeChainCache | 3.870 μs | 0.3408 μs | 0.9888 μs | 0.1004 μs | 1.800 μs | 3.100 μs | 3.700 μs | 4.500 μs | 6.600 μs | 258,391.0 |  2.21 |    0.76 |      32 B |        1.00 |
