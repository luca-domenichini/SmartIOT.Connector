# Before optimization + flip bytes
| Method               | N    | Mean         | Error       | StdDev      | Gen0    | Allocated |
|--------------------- |----- |-------------:|------------:|------------:|--------:|----------:|
| ReadCycles           | 1    |     428.4 ns |    13.31 ns |     8.81 ns |  0.0563 |     736 B |
| WriteCycles          | 1    |     383.9 ns |    17.54 ns |    11.60 ns |  0.0424 |     560 B |
| ReadWriteInterleaved | 1    |     806.3 ns |    16.27 ns |    10.76 ns |  0.0877 |    1152 B |
| ScheduleNextTag      | 1    |     513.5 ns |     7.69 ns |     4.57 ns |  0.0591 |     776 B |
| ReadCycles           | 10   |   4,247.2 ns |    52.89 ns |    31.48 ns |  0.5569 |    7360 B |
| WriteCycles          | 10   |   3,653.9 ns |    31.14 ns |    20.60 ns |  0.4272 |    5600 B |
| ReadWriteInterleaved | 10   |   7,830.9 ns |   119.03 ns |    70.83 ns |  0.8698 |   11520 B |
| ScheduleNextTag      | 10   |   4,933.1 ns |    62.07 ns |    41.05 ns |  0.4654 |    6160 B |
| ReadCycles           | 100  |  42,565.6 ns |   760.03 ns |   452.28 ns |  5.6152 |   73600 B |
| WriteCycles          | 100  |  37,141.8 ns |   690.99 ns |   361.40 ns |  4.2725 |   56000 B |
| ReadWriteInterleaved | 100  |  77,968.4 ns | 1,120.42 ns |   741.09 ns |  8.7891 |  115200 B |
| ScheduleNextTag      | 100  |  48,551.0 ns |   415.31 ns |   274.70 ns |  4.6997 |   61600 B |
| ReadCycles           | 1000 | 424,167.6 ns | 3,619.79 ns | 2,394.27 ns | 56.1523 |  736000 B |
| WriteCycles          | 1000 | 379,599.0 ns | 5,342.94 ns | 3,534.03 ns | 42.4805 |  560000 B |
| ReadWriteInterleaved | 1000 | 779,851.2 ns | 9,516.95 ns | 4,977.55 ns | 87.8906 | 1152000 B |
| ScheduleNextTag      | 1000 | 490,078.1 ns | 4,551.73 ns | 2,708.66 ns | 46.8750 |  616000 B |

# After optimization + flip bytes
| Method               | N    | Mean         | Error       | StdDev      | Gen0    | Allocated |
|--------------------- |----- |-------------:|------------:|------------:|--------:|----------:|
| ReadCycles           | 1    |     406.0 ns |     4.69 ns |     3.10 ns |  0.0486 |     640 B |
| WriteCycles          | 1    |     341.3 ns |     5.46 ns |     3.61 ns |  0.0243 |     320 B |
| ReadWriteInterleaved | 1    |     732.2 ns |    13.51 ns |     8.94 ns |  0.0620 |     816 B |
| ScheduleNextTag      | 1    |     468.1 ns |     3.70 ns |     2.20 ns |  0.0520 |     680 B |
| ReadCycles           | 10   |   4,025.3 ns |   103.16 ns |    68.23 ns |  0.4883 |    6400 B |
| WriteCycles          | 10   |   3,342.0 ns |    25.07 ns |    16.58 ns |  0.2441 |    3200 B |
| ReadWriteInterleaved | 10   |   7,313.1 ns |    64.14 ns |    42.43 ns |  0.6180 |    8160 B |
| ScheduleNextTag      | 10   |   4,724.7 ns |    37.53 ns |    24.82 ns |  0.3357 |    4480 B |
| ReadCycles           | 100  |  40,003.2 ns |   420.02 ns |   277.82 ns |  4.8828 |   64000 B |
| WriteCycles          | 100  |  34,092.6 ns |   841.25 ns |   556.43 ns |  2.4414 |   32000 B |
| ReadWriteInterleaved | 100  |  75,030.9 ns |   997.59 ns |   659.85 ns |  6.2256 |   81600 B |
| ScheduleNextTag      | 100  |  47,111.4 ns |   484.88 ns |   320.72 ns |  3.4180 |   44800 B |
| ReadCycles           | 1000 | 410,498.2 ns | 6,101.36 ns | 3,630.82 ns | 48.8281 |  640000 B |
| WriteCycles          | 1000 | 335,121.5 ns | 3,827.80 ns | 2,531.85 ns | 24.4141 |  320000 B |
| ReadWriteInterleaved | 1000 | 737,767.5 ns | 9,610.79 ns | 6,356.94 ns | 61.5234 |  816000 B |
| ScheduleNextTag      | 1000 | 471,945.7 ns | 9,376.69 ns | 6,202.10 ns | 34.1797 |  448000 B |

# Before optimization no flip bytes
| Method               | N    | Mean         | Error        | StdDev      | Gen0    | Allocated |
|--------------------- |----- |-------------:|-------------:|------------:|--------:|----------:|
| ReadCycles           | 1    |     314.1 ns |      5.80 ns |     3.84 ns |  0.0391 |     512 B |
| WriteCycles          | 1    |     335.2 ns |      7.85 ns |     5.19 ns |  0.0424 |     560 B |
| ReadWriteInterleaved | 1    |     634.4 ns |     25.75 ns |    17.03 ns |  0.0706 |     928 B |
| ScheduleNextTag      | 1    |     426.3 ns |     10.22 ns |     5.34 ns |  0.0420 |     552 B |
| ReadCycles           | 10   |   3,129.7 ns |     52.18 ns |    34.51 ns |  0.3891 |    5120 B |
| WriteCycles          | 10   |   3,408.6 ns |    218.57 ns |   144.57 ns |  0.4272 |    5600 B |
| ReadWriteInterleaved | 10   |   6,477.6 ns |    204.51 ns |   135.27 ns |  0.7095 |    9280 B |
| ScheduleNextTag      | 10   |   4,055.9 ns |     69.51 ns |    45.97 ns |  0.4196 |    5520 B |
| ReadCycles           | 100  |  32,218.7 ns |    803.48 ns |   478.14 ns |  3.9063 |   51200 B |
| WriteCycles          | 100  |  33,349.7 ns |  1,182.22 ns |   781.96 ns |  4.2725 |   56000 B |
| ReadWriteInterleaved | 100  |  61,597.1 ns |    799.93 ns |   476.03 ns |  7.0801 |   92800 B |
| ScheduleNextTag      | 100  |  39,221.8 ns |    990.70 ns |   589.55 ns |  4.2114 |   55200 B |
| ReadCycles           | 1000 | 304,520.6 ns |  6,567.53 ns | 4,344.02 ns | 39.0625 |  512000 B |
| WriteCycles          | 1000 | 316,510.2 ns |  6,567.14 ns | 4,343.75 ns | 42.4805 |  560000 B |
| ReadWriteInterleaved | 1000 | 611,730.1 ns | 11,418.86 ns | 7,552.87 ns | 70.3125 |  928000 B |
| ScheduleNextTag      | 1000 | 374,429.1 ns |  5,426.23 ns | 3,589.12 ns | 41.9922 |  552000 B |

# After optimization no flip bytes
| Method               | N    | Mean         | Error        | StdDev       | Gen0    | Allocated |
|--------------------- |----- |-------------:|-------------:|-------------:|--------:|----------:|
| ReadCycles           | 1    |     320.3 ns |     12.32 ns |      8.15 ns |  0.0315 |     416 B |
| WriteCycles          | 1    |     282.6 ns |      6.33 ns |      4.19 ns |  0.0243 |     320 B |
| ReadWriteInterleaved | 1    |     574.1 ns |     16.94 ns |     11.21 ns |  0.0448 |     592 B |
| ScheduleNextTag      | 1    |     372.7 ns |      8.85 ns |      5.86 ns |  0.0348 |     456 B |
| ReadCycles           | 10   |   3,335.4 ns |     28.32 ns |     18.73 ns |  0.3166 |    4160 B |
| WriteCycles          | 10   |   2,936.4 ns |     54.08 ns |     32.18 ns |  0.2441 |    3200 B |
| ReadWriteInterleaved | 10   |   5,882.0 ns |    145.84 ns |     96.46 ns |  0.4501 |    5920 B |
| ScheduleNextTag      | 10   |   3,840.3 ns |     56.33 ns |     33.52 ns |  0.3471 |    4560 B |
| ReadCycles           | 100  |  30,544.2 ns |    566.52 ns |    337.13 ns |  3.1738 |   41600 B |
| WriteCycles          | 100  |  28,629.8 ns |  1,425.92 ns |    943.16 ns |  2.4414 |   32000 B |
| ReadWriteInterleaved | 100  |  57,186.9 ns |  2,780.21 ns |  1,838.94 ns |  4.5166 |   59200 B |
| ScheduleNextTag      | 100  |  36,529.2 ns |    696.86 ns |    460.93 ns |  3.4790 |   45600 B |
| ReadCycles           | 1000 | 308,622.2 ns |  4,395.26 ns |  2,907.19 ns | 31.7383 |  416000 B |
| WriteCycles          | 1000 | 294,683.6 ns | 17,329.78 ns | 11,462.58 ns | 24.4141 |  320000 B |
| ReadWriteInterleaved | 1000 | 553,531.7 ns |  6,029.61 ns |  3,988.22 ns | 44.9219 |  592000 B |
| ScheduleNextTag      | 1000 | 365,855.1 ns |  1,923.88 ns |  1,272.53 ns | 34.6680 |  456000 B |
