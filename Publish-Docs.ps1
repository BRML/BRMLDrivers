$doctemp = "$env:TEMP\Docs"
if (Test-Path $doctemp) { rm -Recurse -Force $doctemp }
mkdir $doctemp | Out-Null
pushd $doctemp
git clone -b gh-pages https://github.com/BRML/BRMLDrivers.git . 2>$null
rm -Recurse *
popd

$ErrorActionPreference = "Stop"
.\Generate-Docs.ps1
$ErrorActionPreference = "Continue"
cp -Recurse docs\output\* $doctemp\

pushd $doctemp
git add --all . 2>$null
git commit -m "automatic documentation generation" 2>$null
git push 2>$null
popd

