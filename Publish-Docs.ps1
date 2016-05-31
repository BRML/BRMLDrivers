$doctemp = "$env:TEMP\Docs"
if (Test-Path $doctemp) { rm -Recurse -Force $doctemp }
mkdir $doctemp
pushd $doctemp
git clone -b gh-pages https://github.com/BRML/BRMLDrivers.git . 2> $nul
rm -Recurse *
popd

.\Generate-Docs.ps1
cp -Recurse docs\output\* $doctemp\

pushd $doctemp
git add --all .
git commit -m "automatic documentation generation"
git push
popd

