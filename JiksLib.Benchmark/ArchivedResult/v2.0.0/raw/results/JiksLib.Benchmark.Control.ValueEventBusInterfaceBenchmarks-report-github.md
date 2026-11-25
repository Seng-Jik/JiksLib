```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
12th Gen Intel Core i5-12600KF 3.70GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=.NET 10.0  Runtime=.NET 10.0  InvocationCount=1  
UnrollFactor=1  

```
| Method                                 | ListenerCount | Mean       | Error     | StdDev   | StdErr   | Median     | Min           | Q1         | Q3         | Max        | Op/s        | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------------- |-------------- |-----------:|----------:|---------:|---------:|-----------:|--------------:|-----------:|-----------:|-----------:|------------:|------:|--------:|----------:|------------:|
| **PublishViaInterface_NoListeners**        | **1**             | **1,138.0 ns** |  **79.34 ns** | **223.8 ns** | **23.33 ns** | **1,100.0 ns** |   **600.0000 ns** | **1,000.0 ns** | **1,200.0 ns** | **1,700.0 ns** |   **878,701.1** |     **?** |       **?** |      **24 B** |           **?** |
| PublishViaInterface_WithListeners      | 1             | 1,973.4 ns | 123.83 ns | 353.3 ns | 36.44 ns | 1,950.0 ns | 1,050.0000 ns | 1,750.0 ns | 2,225.0 ns | 2,850.0 ns |   506,738.5 |     ? |       ? |      24 B |           ? |
| PublishDirect_NoListeners              | 1             |   217.2 ns |  36.79 ns | 107.9 ns | 10.84 ns |   200.0 ns |     0.0000 ns |   100.0 ns |   300.0 ns |   600.0 ns | 4,604,651.2 |     ? |       ? |         - |           ? |
| PublishDirect_WithListeners            | 1             | 1,138.3 ns | 114.34 ns | 326.2 ns | 33.65 ns | 1,150.0 ns |   550.0000 ns |   850.0 ns | 1,350.0 ns | 1,950.0 ns |   878,504.7 |     ? |       ? |         - |           ? |
| PublishMultipleEventTypes_ViaInterface | 1             | 1,505.3 ns | 137.77 ns | 395.3 ns | 40.56 ns | 1,500.0 ns |   700.0000 ns | 1,200.0 ns | 1,700.0 ns | 2,600.0 ns |   664,335.7 |     ? |       ? |      72 B |           ? |
| TupleAccessPerformance                 | 1             | 1,428.7 ns | 116.49 ns | 332.3 ns | 34.28 ns | 1,400.0 ns |   800.0000 ns | 1,200.0 ns | 1,600.0 ns | 2,200.0 ns |   699,925.5 |     ? |       ? |      24 B |           ? |
| InterfaceConversionOverhead            | 1             | 1,690.9 ns | 131.38 ns | 361.9 ns | 38.58 ns | 1,650.0 ns | 1,000.0000 ns | 1,500.0 ns | 1,900.0 ns | 2,700.0 ns |   591,397.8 |     ? |       ? |     480 B |           ? |
| DictionaryLookupWithTuple              | 1             | 1,215.3 ns | 119.40 ns | 348.3 ns | 35.18 ns | 1,200.0 ns |   600.0000 ns |   900.0 ns | 1,400.0 ns | 2,100.0 ns |   822,838.0 |     ? |       ? |     120 B |           ? |
|                                        |               |            |           |          |          |            |               |            |            |            |             |       |         |           |             |
| **PublishViaInterface_NoListeners**        | **10**            | **1,236.3 ns** | **103.02 ns** | **288.9 ns** | **30.28 ns** | **1,200.0 ns** |   **700.0000 ns** | **1,000.0 ns** | **1,300.0 ns** | **2,100.0 ns** |   **808,888.9** |     **?** |       **?** |      **24 B** |           **?** |
| PublishViaInterface_WithListeners      | 10            | 2,187.4 ns | 144.98 ns | 416.0 ns | 42.68 ns | 2,100.0 ns | 1,400.0000 ns | 1,900.0 ns | 2,350.0 ns | 3,300.0 ns |   457,170.4 |     ? |       ? |      24 B |           ? |
| PublishDirect_NoListeners              | 10            |   153.8 ns |  38.74 ns | 109.9 ns | 11.40 ns |   100.0 ns |     0.0000 ns |   100.0 ns |   200.0 ns |   400.0 ns | 6,503,496.5 |     ? |       ? |         - |           ? |
| PublishDirect_WithListeners            | 10            | 1,401.0 ns | 102.50 ns | 297.4 ns | 30.19 ns | 1,300.0 ns |   900.0000 ns | 1,200.0 ns | 1,600.0 ns | 2,200.0 ns |   713,760.1 |     ? |       ? |         - |           ? |
| PublishMultipleEventTypes_ViaInterface | 10            | 1,357.0 ns | 113.32 ns | 321.5 ns | 33.34 ns | 1,300.0 ns |   700.0000 ns | 1,100.0 ns | 1,500.0 ns | 2,300.0 ns |   736,925.5 |     ? |       ? |      72 B |           ? |
| TupleAccessPerformance                 | 10            | 1,261.5 ns |  93.46 ns | 269.6 ns | 27.52 ns | 1,200.0 ns |   900.0000 ns | 1,000.0 ns | 1,400.0 ns | 1,900.0 ns |   792,733.3 |     ? |       ? |      24 B |           ? |
| InterfaceConversionOverhead            | 10            | 1,789.4 ns | 102.99 ns | 293.8 ns | 30.31 ns | 1,700.0 ns | 1,400.0000 ns | 1,600.0 ns | 1,900.0 ns | 2,600.0 ns |   558,858.5 |     ? |       ? |     480 B |           ? |
| DictionaryLookupWithTuple              | 10            | 1,629.5 ns | 105.89 ns | 303.8 ns | 31.17 ns | 1,600.0 ns | 1,000.0000 ns | 1,400.0 ns | 1,800.0 ns | 2,400.0 ns |   613,695.1 |     ? |       ? |     120 B |           ? |
|                                        |               |            |           |          |          |            |               |            |            |            |             |       |         |           |             |
| **PublishViaInterface_NoListeners**        | **100**           | **1,217.2 ns** | **101.53 ns** | **288.0 ns** | **29.87 ns** | **1,200.0 ns** |   **700.0000 ns** | **1,000.0 ns** | **1,400.0 ns** | **1,900.0 ns** |   **821,554.8** |  **5.72** |    **3.22** |      **24 B** |          **NA** |
| PublishViaInterface_WithListeners      | 100           | 3,908.2 ns | 213.30 ns | 622.2 ns | 62.85 ns | 3,850.0 ns | 2,800.0000 ns | 3,400.0 ns | 4,300.0 ns | 5,400.0 ns |   255,874.7 | 18.35 |    9.72 |      24 B |          NA |
| PublishDirect_NoListeners              | 100           |   261.6 ns |  43.99 ns | 126.2 ns | 12.95 ns |   250.0 ns |    50.0000 ns |   150.0 ns |   350.0 ns |   650.0 ns | 3,822,937.6 |  1.23 |    0.90 |         - |          NA |
| PublishDirect_WithListeners            | 100           | 2,247.4 ns | 133.23 ns | 386.5 ns | 39.24 ns | 2,000.0 ns | 1,700.0000 ns | 2,000.0 ns | 2,600.0 ns | 3,500.0 ns |   444,954.1 | 10.55 |    5.64 |         - |          NA |
| PublishMultipleEventTypes_ViaInterface | 100           | 1,162.1 ns |  73.00 ns | 209.4 ns | 21.49 ns | 1,100.0 ns |   700.0000 ns | 1,000.0 ns | 1,300.0 ns | 1,700.0 ns |   860,507.2 |  5.46 |    2.93 |      72 B |          NA |
| TupleAccessPerformance                 | 100           | 1,255.4 ns |  72.71 ns | 205.1 ns | 21.38 ns | 1,150.0 ns |   950.0000 ns | 1,150.0 ns | 1,350.0 ns | 1,850.0 ns |   796,536.8 |  5.89 |    3.13 |      24 B |          NA |
| InterfaceConversionOverhead            | 100           | 1,595.9 ns |  99.99 ns | 290.1 ns | 29.45 ns | 1,500.0 ns | 1,000.0000 ns | 1,400.0 ns | 1,700.0 ns | 2,300.0 ns |   626,615.0 |  7.49 |    4.03 |     480 B |          NA |
| DictionaryLookupWithTuple              | 100           | 1,366.0 ns |  80.67 ns | 234.0 ns | 23.76 ns | 1,300.0 ns |   900.0000 ns | 1,200.0 ns | 1,500.0 ns | 2,000.0 ns |   732,075.5 |  6.41 |    3.43 |     120 B |          NA |
