## Sources

Compiled from [custom repository](https://github.com/stride3d/gettextnet) (`master` branch, .NET 10).
Original fork from [Gettext.NET](https://sourceforge.net/projects/gettextnet/) (LGPL 2.1)

`GNU.Gettext.Msgfmt.exe` is the satellite-assembly compiler used at build time (single-file,
framework-dependent net10.0, compiles satellites in-process via Roslyn). `GNU.Gettext.dll` here
is the satellite compile reference and must match the runtime `Stride.GNU.Gettext` package; both
are versioned 3.0.0. Rebuild/publish the runtime packages via `.github/workflows/dep-gettext.yml`.
