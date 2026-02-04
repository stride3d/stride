# StreamWrapperGenerator Extraction - Phase 5

## Status
Phase 5 is partially complete. The BuiltinProcessor has been successfully extracted.

## Remaining Work
The `GenerateStreamWrapper` method (688 lines, lines 207-894 in InterfaceProcessor.cs) needs to be extracted into this class.

## Approach
Due to the size and complexity of this method, with 10 local helper functions tightly coupled to local state, the extraction requires:

1. Creating a static `GenerateStreamWrapper` method in StreamWrapperGenerator class
2. Adding parameters for dependencies:
   - `Action<int, int>? codeInserted` - for the CodeInserted delegate
   - `Func<SpirvContext, Symbol, int> findOutputPatchSize` - for FindOutputPatchSize method
   - `Func<SymbolTable, SpirvContext, Symbol, Symbol?> resolveHullPatchConstantEntryPoint` - for ResolveHullPatchConstantEntryPoint method
3. Updating InterfaceProcessor to call the extracted method
4. Keeping the local helper functions within the method (they're tightly coupled)

## Alternative Approach  
Instead of a massive static method, consider creating StreamWrapperGenerator as an instance class that holds state (buffer, context, etc.) and breaks down the local functions into private methods. This would be a cleaner architecture but requires more extensive refactoring.
