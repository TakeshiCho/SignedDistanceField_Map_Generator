# Signed Distance Field Map Generator

### Examine Environment : 
CPU: Intel i7-9700 / AMD R9-6900HX \
GPU: RTX 2060 / RTX 3070ti Laptop

### Raster Scanning (8ssedt CPU)
https://zhuanlan.zhihu.com/p/518292475

| size |   i7-9700   | R9-6900HX |
|:---: |:-----------:|:--------------:|
| 2048 * 2048 |   5227ms    |     2513ms     |
| 1024 * 2048 |   1261ms    |     574ms      |
| 512 * 512  |    266ms    |     151ms      |

### Independent Scanning (Saito GPU)
https://zhuanlan.zhihu.com/p/556295864

| size | RTX 2060 | RTX 3070ti Laptop |
|:---: |:--------:|:-----------------:|
| 2048 * 2048 |   61ms   |       52ms        |
| 1024 * 2048 |   8ms    |        8ms        |
| 512 * 512  |   1ms    |       1ms        |
