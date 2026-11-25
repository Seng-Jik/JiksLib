```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
12th Gen Intel Core i5-12600KF 3.70GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=.NET 10.0  Runtime=.NET 10.0  InvocationCount=1  
UnrollFactor=1  

```
| Method                            | Mean     | Error     | StdDev    | StdErr    | Min      | Q1       | Median   | Q3       | Max      | Op/s      | Allocated |
|---------------------------------- |---------:|----------:|----------:|----------:|---------:|---------:|---------:|---------:|---------:|----------:|----------:|
| PublishEvent_InterfaceBase        | 2.353 μs | 0.1756 μs | 0.5066 μs | 0.0517 μs | 1.500 μs | 1.900 μs | 2.350 μs | 2.625 μs | 3.600 μs | 424,966.8 |      24 B |
| PublishEvent_ObjectBase           | 2.168 μs | 0.1614 μs | 0.4578 μs | 0.0475 μs | 1.500 μs | 1.900 μs | 2.100 μs | 2.400 μs | 3.500 μs | 461,309.5 |      24 B |
| PublishEvent_SharedTypeChainCache | 2.727 μs | 0.1804 μs | 0.5233 μs | 0.0531 μs | 1.500 μs | 2.300 μs | 2.700 μs | 3.100 μs | 4.300 μs | 366,729.7 |      24 B |
