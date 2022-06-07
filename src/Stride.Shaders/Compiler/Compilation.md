# Compilation

SDSL should first be lowered to a three address code that ressembles SPIRV.
The idea is to manage this intermediate code for optimization of code instead of using `spirv-tools` which are c++ libraries.

## Lowering

Lowering will be done by 