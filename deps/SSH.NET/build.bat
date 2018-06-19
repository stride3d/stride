set EXTERNALS_SSH_NET=..\..\externals\SSH.NET\src
pushd %EXTERNALS_SSH_NET%
msbuild /p:Configuration=Release Renci.SshNet.VS2015.sln
popd

copy %EXTERNALS_SSH_NET%\Renci.SshNet\bin\Release\Renci.SshNet.* .
