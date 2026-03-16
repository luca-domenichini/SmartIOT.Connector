| Method               | N    | Mean         | Error       | StdDev      | Gen0    | Allocated |
|--------------------- |----- |-------------:|------------:|------------:|--------:|----------:|
| ReadCycles           | 1    |     423.1 ns |     3.27 ns |     1.71 ns |  0.0563 |     736 B |
| WriteCycles          | 1    |     378.0 ns |     7.77 ns |     5.14 ns |  0.0424 |     560 B |
| ReadWriteInterleaved | 1    |     803.7 ns |    10.07 ns |     5.99 ns |  0.0877 |    1152 B |
| ScheduleNextTag      | 1    |     501.9 ns |     9.24 ns |     6.11 ns |  0.0591 |     776 B |
| ReadCycles           | 10   |   4,326.7 ns |   123.14 ns |    81.45 ns |  0.5569 |    7360 B |
| WriteCycles          | 10   |   3,805.5 ns |    47.99 ns |    31.74 ns |  0.4272 |    5600 B |
| ReadWriteInterleaved | 10   |   7,775.9 ns |    61.89 ns |    40.94 ns |  0.8698 |   11520 B |
| ScheduleNextTag      | 10   |   5,073.1 ns |   155.37 ns |   102.77 ns |  0.4654 |    6160 B |
| ReadCycles           | 100  |  41,968.1 ns |   677.13 ns |   447.88 ns |  5.6152 |   73600 B |
| WriteCycles          | 100  |  38,621.8 ns |   721.49 ns |   477.22 ns |  4.2725 |   56000 B |
| ReadWriteInterleaved | 100  |  81,314.3 ns | 1,885.98 ns | 1,247.46 ns |  8.7891 |  115200 B |
| ScheduleNextTag      | 100  |  49,409.3 ns |   546.10 ns |   361.21 ns |  4.6997 |   61600 B |
| ReadCycles           | 1000 | 427,544.9 ns | 3,393.43 ns | 2,019.37 ns | 56.1523 |  736000 B |
| WriteCycles          | 1000 | 369,598.1 ns | 2,601.45 ns | 1,548.08 ns | 42.4805 |  560000 B |
| ReadWriteInterleaved | 1000 | 793,921.1 ns | 9,016.93 ns | 5,964.14 ns | 87.8906 | 1152000 B |
| ScheduleNextTag      | 1000 | 490,627.5 ns | 6,621.14 ns | 3,940.13 ns | 46.8750 |  616000 B |

