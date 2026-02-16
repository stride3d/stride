MixinName "MixinA"
%1 = OpTypeFloat 32
%2 = OpTypeVector %1 2
MixinEnd
MixinName "MixinB"
MixinInherit "MixinA"
%1 MixinImport "MixinA" 1
%2 = OpTypeVector %1 3
%3 = OpTypeMatrix %2 3
MixinEnd
MixinName "MixinC"
MixinInherit "MixinA"
%1 MixinImport "MixinA" 1
%2 = OpTypeVector %1 4
%3 = OpTypeMatrix %2 4
MixinEnd



MixinName "MixinA"          --> OpNop
%1 = OpTypeFloat 32         --> Keep
%2 = OpTypeVector %1 2      --> Keep
MixinEnd                    --> OpNop
MixinName "MixinB"          --> OpNop
MixinInherit "MixinA"       --> OpNop
%1 MixinImport "MixinA" 1   --> Keep + offset id
%2 = OpTypeVector %1 3      --> Keep + offset id
%3 = OpTypeMatrix %2 3
MixinEnd
MixinName "MixinC"
MixinInherit "MixinA"
%1 MixinImport "MixinA" 1
%2 = OpTypeVector %1 4
%3 = OpTypeMatrix %2 4
MixinEnd