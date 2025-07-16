## About

Sign CLI is a .NET tool that provides digital signing for .NET assemblies, packages, and other files.

The tool signs files inside-out, starting with the most nested files and then the outer files, ensuring everything is signed in the correct order.

## Prerequisites

- An up-to-date x64-based version of Windows currently in [mainstream support](https://learn.microsoft.com/lifecycle/products/)
- [.NET 8 SDK or later](https://dotnet.microsoft.com/download)
- [Microsoft Visual C++ 14 runtime](https://aka.ms/vs/17/release/vc_redist.x64.exe)
- For ClickOnce and VSTO signing:  A recent --- ideally, the latest --- version of [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework)

## Usage

- For help with...
    - Azure Key Vault:  `sign code azure-key-vault --help`
    - Trusted Signing:  `sign code trusted-signing --help`
    - local signing:  `sign code certificate-store --help`
- Version information:  `sign --version`

See the [GitHub repository](https://github.com/dotnet/sign) for additional information and samples.

## License

This package is released as open source under the [MIT license](https://licenses.nuget.org/MIT).

## Feedback

Bug reports, feedback, and contributions are welcome at the [GitHub repository](https://github.com/dotnet/sign).

Happy signing! 🚀
