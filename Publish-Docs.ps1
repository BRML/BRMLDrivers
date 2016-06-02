$ErrorActionPreference = "Stop"

$doctemp = "$env:TEMP\Docs"
if (Test-Path $doctemp) { rm -Recurse -Force $doctemp }
mkdir $doctemp
pushd $doctemp
git clone -b gh-pages https://github.com/BRML/BRMLDrivers.git . 2>&1
rm -Recurse *
popd

.\Generate-Docs.ps1
cp -Recurse docs\output\* $doctemp\

pushd $doctemp
git add --all . 2>&1
git commit -m "automatic documentation generation" 2>&1
git push 2>&1
popd

