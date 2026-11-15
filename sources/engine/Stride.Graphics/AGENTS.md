# Graphics

- Resource barriers are explicit. When a resource needs a layout transition, add `commandList.ResourceBarrierTransition(resource, BarrierLayout.X)` in the renderer that knows the life of the resource (`ForwardRenderer`, an image effect, the shadow map renderer, in `Stride.Engine` or `Stride.Rendering`), right before the draw or dispatch that needs it. Don't add an automatic transition pass to `CommandList`: it hides the cost and the intent of each transition.
- A depth buffer that stays bound as depth-stencil and is also sampled uses `BarrierLayout.DepthStencilRead`.
- Test a change on each API it touches, with `-p:StrideGraphicsApisTest=<Api>` (see the root [AGENTS.md](../../../AGENTS.md)). Rebuild the test project first: it has its own copy of `Stride.Graphics.dll`.
