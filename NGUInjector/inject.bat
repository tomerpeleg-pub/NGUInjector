@setlocal enableextensions
pushd "%~dp0"

.\injector\smi.exe inject -p NGUIdle -a .\injector\NGUInjector.dll -n NGUInjector -c Loader -m Init

popd