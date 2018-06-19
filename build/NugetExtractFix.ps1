Get-ChildItem * -recurse | Where-Object { $_.Name -match "%24*" } | Rename-Item -NewName { $_.Name -replace "%24","$" }
Get-ChildItem * -recurse | Where-Object { $_.Name -match "%40*" } | Rename-Item -NewName { $_.Name -replace "%40","@" }
Get-ChildItem * -recurse | Where-Object { $_.Name -match "%20*" } | Rename-Item -NewName { $_.Name -replace "%20"," " }