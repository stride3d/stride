# Generating CFG in spirv

given this :

```glsl
for(int i = 0; i< 4; i++)
{
   
    fragColor.x = i;
}
```

spirv-cross generates 

```

// Creating the variable i
%i = OpVariable %_ptr_Function_int Function
// initializing it with 0
      OpStore %i %int_0
// We define an unconditional branch that always goes to a specific label. 
      OpBranch %10
// Specific label to go back to
%10 = OpLabel
// Declares a structed loop, %12 is the merge block (exit), %13 is the continue
      OpLoopMerge %12 %13 None
// An OpBranch should come right after an OpLoopMerge to start the branch
      OpBranch %14
%14 = OpLabel
// Load i and register if i is below 4
%15 = OpLoad %int %i
%18 = OpSLessThan %bool %15 %int_4
// This OpBranchConditional goes to either %11(code) or %12 (exit) depending %18
      OpBranchConditional %18 %11 %12
%11 = OpLabel
// This is the code inside the block
%23 = OpLoad %int %i
%24 = OpConvertSToF %float %23
%28 = OpAccessChain %_ptr_Output_float %fragColor %uint_0
      OpStore %28 %24
// End of the code inside the block, now go back to %13 to add 1 to i
      OpBranch %13
%13 = OpLabel
%29 = OpLoad %int %i
%31 = OpIAdd %int %29 %int_1
      OpStore %i %31
// Now go back to %10 to start the loop again
      OpBranch %10
%12 = OpLabel
```

While loops are surprisingly simpler.

```
int i = 0;
while(i < 4)
{
    fragColor.x = i;
    i += 1;
}
```

```
%i = OpVariable %_ptr_Function_int Function
    OpStore %i %int_0
    OpBranch %10
%10 = OpLabel
    OpLoopMerge %12 %13 None
    OpBranch %14
%14 = OpLabel
%15 = OpLoad %int %i
%18 = OpSLessThan %bool %15 %int_4
    OpBranchConditional %18 %11 %12
%11 = OpLabel
%23 = OpLoad %int %i
%24 = OpConvertSToF %float %23
%28 = OpAccessChain %_ptr_Output_float %fragColor %uint_0
    OpStore %28 %24
%30 = OpLoad %int %i
%31 = OpIAdd %int %30 %int_1
    OpStore %i %31
    OpBranch %13
%13 = OpLabel
    OpBranch %10
%12 = OpLabel
```