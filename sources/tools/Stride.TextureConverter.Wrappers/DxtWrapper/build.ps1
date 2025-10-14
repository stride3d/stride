# Make sure to install package required packages : vcpkg install directxtex

param (
    [string]$vcpkg_dir
)

if(!$vcpkg_dir)
{
    Write-Error "Please provide vpckg directory path"
    return;
}

cmake -B "build" -S . -DCMAKE_TOOLCHAIN_FILE="$vcpkg_dir/scripts/buildsystems/vcpkg.cmake";
cd build;
make;
cd ..;