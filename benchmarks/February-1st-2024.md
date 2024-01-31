```
// * Summary *
AggregatedRows
BenchmarkDotNet v0.13.11, Arch Linux
11th Gen Intel Core i7-11800H 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
  [Host]   : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  .NET 8.0 : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=.NET 8.0  Runtime=.NET 8.0  InvocationCount=1  
UnrollFactor=1 
```

- LocalBulkInsertionBenchmark:

| Method                 | BulkInsertAmount |         Mean |      Error |       StdDev |       Median |
|:-----------------------|-----------------:|-------------:|-----------:|-------------:|-------------:|
| BulkInsertionBenchmark |               10 |     55.00 us |   1.947 us |     5.524 us |     54.40 us |
| BulkInsertionBenchmark |              100 |    449.43 us |  16.686 us |    47.064 us |    446.46 us |
| BulkInsertionBenchmark |             1000 |  3,504.20 us | 665.039 us | 1,960.882 us |  2,896.52 us |
| BulkInsertionBenchmark |            10000 | 16,451.46 us | 348.388 us |   976.919 us | 16,447.48 us |

  
- NetworkBulkInsertionBenchmark:

| Method                 | BulkInsertAmount |      Mean |     Error |     StdDev |    Median |
|:-----------------------|-----------------:|----------:|----------:|-----------:|----------:|
| BulkInsertionBenchmark |               10 | 87.070 ms | 1.6669 ms |  1.7118 ms | 88.304 ms |
| BulkInsertionBenchmark |              100 | 85.432 ms | 1.6938 ms |  1.7394 ms | 84.454 ms |
| BulkInsertionBenchmark |             1000 | 20.028 ms | 7.1632 ms | 21.1209 ms |  3.641 ms |
| BulkInsertionBenchmark |             2000 |  6.699 ms | 0.2537 ms |  0.6593 ms |  6.556 ms |


- LocalSimpleAggregationBenchmark

| Method                                       | AggregatedRows |        Mean |     Error |    StdDev |
|:---------------------------------------------|---------------:|------------:|----------:|----------:|
| SimpleAggregationBenchmark                   |            100 |    136.3 us |   4.28 us |  12.13 us |
| SimpleAggregationAndDeserializationBenchmark |            100 |    203.4 us |   4.03 us |  10.55 us |
| SimpleAggregationBenchmark                   |           1000 |  1,615.9 us |  53.46 us | 152.52 us |
| SimpleAggregationAndDeserializationBenchmark |           1000 |  2,606.9 us | 227.29 us | 663.02 us |
| SimpleAggregationBenchmark                   |          10000 | 17,081.6 us | 338.35 us | 937.55 us |
| SimpleAggregationAndDeserializationBenchmark |          10000 | 18,289.4 us | 365.47 us | 770.91 us |

- IntegerBTreeInsertionBenchmark

| Method    | Degree | InsertionAmount |        Mean |     Error |      StdDev |      Median |
|:----------|-------:|----------------:|------------:|----------:|------------:|------------:|
| BTree     |     10 |            1000 |    609.7 us |  11.73 us |    11.52 us |    609.9 us |
| Reference |     10 |            1000 |    731.8 us |  14.43 us |    15.44 us |    731.9 us |
| BTree     |     10 |           10000 |  6,954.4 us |  90.37 us |    84.53 us |  6,954.3 us |
| Reference |     10 |           10000 |  2,376.7 us |  34.98 us |    67.40 us |  2,370.2 us |
| BTree     |     10 |          100000 | 21,218.2 us | 417.41 us |   463.95 us | 21,108.4 us |
| Reference |     10 |          100000 | 32,807.2 us | 253.85 us |   211.97 us | 32,848.2 us |
| BTree     |    100 |            1000 |    606.8 us |  11.82 us |    15.37 us |    607.6 us |
| Reference |    100 |            1000 |    735.6 us |  13.99 us |    12.40 us |    740.1 us |
| BTree     |    100 |           10000 |  7,129.6 us | 101.04 us |    94.52 us |  7,142.9 us |
| Reference |    100 |           10000 |  3,825.1 us | 682.50 us | 2,012.37 us |  2,309.6 us |
| BTree     |    100 |          100000 | 20,298.8 us | 306.42 us |   255.87 us | 20,243.6 us |
| Reference |    100 |          100000 | 33,223.0 us | 359.07 us |   318.30 us | 33,238.1 us |
| BTree     |   1000 |            1000 |  2,119.1 us |  41.85 us |    78.60 us |  2,128.8 us |
| Reference |   1000 |            1000 |    662.0 us |  13.24 us |    28.22 us |    661.0 us |
| BTree     |   1000 |           10000 |  4,191.9 us |  40.72 us |    34.01 us |  4,191.8 us |
| Reference |   1000 |           10000 |  2,240.5 us |  60.09 us |   158.30 us |  2,231.1 us |
| BTree     |   1000 |          100000 | 46,173.7 us | 558.70 us |   466.54 us | 46,073.5 us |
| Reference |   1000 |          100000 | 30,244.3 us | 626.17 us | 1,836.46 us | 30,255.2 us |


- IntegerBTreePointQueryBenchmark

| Method         | Degree | InsertionAmount | RepeatCount |        Mean |     Error |    StdDev |      Median |
|:---------------|-------:|----------------:|------------:|------------:|----------:|----------:|------------:|
| BTreePoint     |     10 |           10000 |       10000 |  1,461.5 us |  28.44 us |  40.79 us |  1,452.1 us |
| ReferencePoint |     10 |           10000 |       10000 |    968.4 us |  37.17 us | 105.44 us |    981.5 us |
| BTreePoint     |     10 |           10000 |      100000 | 13,751.8 us |  70.22 us |  62.25 us | 13,737.9 us |
| ReferencePoint |     10 |           10000 |      100000 |  9,541.7 us |  84.74 us |  79.26 us |  9,555.4 us |
| BTreePoint     |     10 |          100000 |       10000 |  1,988.7 us |  39.59 us |  47.13 us |  1,991.6 us |
| ReferencePoint |     10 |          100000 |       10000 |  1,494.3 us |  24.47 us |  22.89 us |  1,490.0 us |
| BTreePoint     |     10 |          100000 |      100000 | 19,138.2 us | 248.55 us | 232.49 us | 19,125.2 us |
| ReferencePoint |     10 |          100000 |      100000 | 15,930.2 us | 183.75 us | 171.88 us | 15,902.1 us |
| BTreePoint     |    100 |           10000 |       10000 |  1,473.9 us | 161.81 us | 448.37 us |  1,154.0 us |
| ReferencePoint |    100 |           10000 |       10000 |    984.8 us |  18.30 us |  40.92 us |    990.8 us |
| BTreePoint     |    100 |           10000 |      100000 | 10,619.1 us | 209.23 us | 325.75 us | 10,679.4 us |
| ReferencePoint |    100 |           10000 |      100000 |  9,686.0 us | 105.39 us |  98.58 us |  9,636.0 us |
| BTreePoint     |    100 |          100000 |       10000 |  1,517.4 us |  29.44 us |  52.33 us |  1,521.4 us |
| ReferencePoint |    100 |          100000 |       10000 |  1,554.9 us |  52.18 us | 153.04 us |  1,496.6 us |
| BTreePoint     |    100 |          100000 |      100000 | 14,335.0 us | 235.40 us | 208.68 us | 14,322.7 us |
| ReferencePoint |    100 |          100000 |      100000 | 13,807.3 us | 198.33 us | 185.51 us | 13,846.8 us |
| BTreePoint     |   1000 |           10000 |       10000 |    968.5 us |  10.06 us |  25.06 us |    967.8 us |
| ReferencePoint |   1000 |           10000 |       10000 |    982.5 us |  13.99 us |  12.40 us |    978.8 us |
| BTreePoint     |   1000 |           10000 |      100000 |  9,288.1 us | 107.92 us |  90.12 us |  9,287.2 us |
| ReferencePoint |   1000 |           10000 |      100000 |  9,552.3 us |  82.90 us |  69.23 us |  9,551.8 us |
| BTreePoint     |   1000 |          100000 |       10000 |  1,367.5 us |  27.00 us |  58.13 us |  1,361.4 us |
| ReferencePoint |   1000 |          100000 |       10000 |  1,505.0 us |  29.78 us |  69.62 us |  1,495.0 us |
| BTreePoint     |   1000 |          100000 |      100000 | 12,629.6 us | 248.26 us | 254.94 us | 12,661.1 us |
| ReferencePoint |   1000 |          100000 |      100000 | 13,769.3 us | 199.88 us | 177.19 us | 13,778.1 us |

- IntegerBTreeRangeQueryBenchmark

| Method          | Degree | InsertionAmount | FetchAmount | RepeatCount |         Mean |       Error |      StdDev |
|:----------------|-------:|----------------:|------------:|------------:|-------------:|------------:|------------:|
| BTreeRangeQuery |     10 |           10000 |        1000 |         100 |   1,496.6 us |    20.99 us |    32.06 us |
| BTreeRangeQuery |     10 |           10000 |        1000 |        1000 |  15,075.5 us |   296.38 us |   277.23 us |
| BTreeRangeQuery |     10 |           10000 |        5000 |         100 |   7,371.9 us |    78.57 us |    73.49 us |
| BTreeRangeQuery |     10 |           10000 |        5000 |        1000 |  73,060.5 us |   347.57 us |   308.11 us |
| BTreeRangeQuery |     10 |           10000 |        8000 |         100 |  12,044.1 us |   233.69 us |   492.94 us |
| BTreeRangeQuery |     10 |           10000 |        8000 |        1000 | 118,430.6 us | 1,373.20 us | 1,146.68 us |
| BTreeRangeQuery |     10 |          100000 |        1000 |         100 |   1,566.0 us |    30.04 us |    35.76 us |
| BTreeRangeQuery |     10 |          100000 |        1000 |        1000 |  15,001.0 us |   298.34 us |   331.60 us |
| BTreeRangeQuery |     10 |          100000 |        5000 |         100 |   8,774.3 us |   109.01 us |   101.97 us |
| BTreeRangeQuery |     10 |          100000 |        5000 |        1000 |  80,453.8 us | 1,215.55 us | 1,137.03 us |
| BTreeRangeQuery |     10 |          100000 |        8000 |         100 |  12,299.7 us |   239.40 us |   302.76 us |
| BTreeRangeQuery |     10 |          100000 |        8000 |        1000 | 127,006.5 us | 1,926.91 us | 1,708.16 us |
| BTreeRangeQuery |    100 |           10000 |        1000 |         100 |     883.1 us |    14.86 us |    28.63 us |
| BTreeRangeQuery |    100 |           10000 |        1000 |        1000 |   8,094.8 us |   103.19 us |    96.52 us |
| BTreeRangeQuery |    100 |           10000 |        5000 |         100 |   4,103.4 us |    66.53 us |    62.23 us |
| BTreeRangeQuery |    100 |           10000 |        5000 |        1000 |  39,362.4 us |   333.14 us |   311.62 us |
| BTreeRangeQuery |    100 |           10000 |        8000 |         100 |   6,540.6 us |   112.46 us |   146.23 us |
| BTreeRangeQuery |    100 |           10000 |        8000 |        1000 |  66,867.8 us |   312.47 us |   277.00 us |
| BTreeRangeQuery |    100 |          100000 |        1000 |         100 |     805.8 us |    14.73 us |    13.78 us |
| BTreeRangeQuery |    100 |          100000 |        1000 |        1000 |   8,010.9 us |   147.14 us |   130.44 us |
| BTreeRangeQuery |    100 |          100000 |        5000 |         100 |   3,856.2 us |    55.16 us |    51.60 us |
| BTreeRangeQuery |    100 |          100000 |        5000 |        1000 |  40,488.7 us |   560.10 us |   523.92 us |
| BTreeRangeQuery |    100 |          100000 |        8000 |         100 |   6,126.0 us |    93.99 us |    87.92 us |
| BTreeRangeQuery |    100 |          100000 |        8000 |        1000 |  68,057.0 us |   392.61 us |   367.25 us |
| BTreeRangeQuery |   1000 |           10000 |        1000 |         100 |     741.1 us |    14.59 us |    18.97 us |
| BTreeRangeQuery |   1000 |           10000 |        1000 |        1000 |   7,534.9 us |    71.74 us |    63.60 us |
| BTreeRangeQuery |   1000 |           10000 |        5000 |         100 |   3,693.1 us |    71.80 us |   107.47 us |
| BTreeRangeQuery |   1000 |           10000 |        5000 |        1000 |  37,891.5 us |   291.96 us |   273.10 us |
| BTreeRangeQuery |   1000 |           10000 |        8000 |         100 |   5,611.9 us |   111.61 us |   237.85 us |
| BTreeRangeQuery |   1000 |           10000 |        8000 |        1000 |  60,224.6 us |   329.50 us |   292.09 us |
| BTreeRangeQuery |   1000 |          100000 |        1000 |         100 |     727.3 us |    14.03 us |    20.12 us |
| BTreeRangeQuery |   1000 |          100000 |        1000 |        1000 |   7,221.2 us |   125.55 us |   111.29 us |
| BTreeRangeQuery |   1000 |          100000 |        5000 |         100 |   3,985.5 us |   119.37 us |   351.96 us |
| BTreeRangeQuery |   1000 |          100000 |        5000 |        1000 |  37,215.3 us |   739.99 us |   880.90 us |
| BTreeRangeQuery |   1000 |          100000 |        8000 |         100 |   5,745.6 us |    33.94 us |    30.09 us |
| BTreeRangeQuery |   1000 |          100000 |        8000 |        1000 |  62,104.7 us |   445.09 us |   347.49 us |
