```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
12th Gen Intel Core i5-12600KF 3.70GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=.NET 10.0  Runtime=.NET 10.0  

```
| Method                            | Mean     | Error    | StdDev   | StdErr   | Min      | Q1       | Median   | Q3       | Max       | Op/s         | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|---------:|----------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| FirstPublish_IndependentCache     | 95.63 ns | 1.760 ns | 4.113 ns | 0.510 ns | 88.74 ns | 92.74 ns | 94.85 ns | 97.84 ns | 107.75 ns | 10,457,496.3 |  1.00 |    0.06 | 0.0710 | 0.0001 |     744 B |        1.00 |
| FirstPublish_SharedPrewarmedCache | 63.10 ns | 1.064 ns | 0.943 ns | 0.252 ns | 62.11 ns | 62.48 ns | 62.77 ns | 63.27 ns |  65.06 ns | 15,847,626.8 |  0.66 |    0.03 | 0.0459 |      - |     480 B |        0.65 |
