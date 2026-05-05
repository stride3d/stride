# tests/

Gold/baseline images for image-comparison regression tests. Test code lives next to whatever runs the comparison; this tree only holds the reference PNGs (and, where convenient, the small fixture script that drives capture).

## Subtrees

- [GPU-TESTING.md](GPU-TESTING.md) — engine-level GPU regression tests (rendering primitives, shaders, particles, etc.). Per-API gold images under `Stride.<X>.Tests/<TestName>.<API>.png`.
- [Stride.Samples.Tests/](Stride.Samples.Tests/README.md) — end-to-end sample screenshot tests. Per-sample fixture + `<Sample>/<frame>.png` golds.
