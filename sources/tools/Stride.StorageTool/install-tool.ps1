dotnet pack;
dotnet tool install -g --add-source ./nupkg stride.storagetool;

# Associate file with our program for Windows

if( $ENV:OS -eq 'Windows_NT')
{
    cmd /c assoc .bundle=bundlefile;
    $path = """"+ (get-command stride-bundle).Path+"""";
    cmd /c ftype bundlefile=$path """%1""";
}