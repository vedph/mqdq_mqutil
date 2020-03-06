@echo off
echo UPDATE PLUGINS

set target=Mqutil\bin\Debug\netcoreapp3.1\Plugins\

md %target%
del %target%*.* /q

xcopy ..\..\Cadmus\Cadmus.Parts\bin\Debug\netstandard2.0\*.dll %target% /y
xcopy ..\..\Cadmus\Cadmus.Lexicon.Parts\bin\Debug\netstandard2.0\*.dll %target% /y
xcopy ..\..\Cadmus\Cadmus.Philology.Parts\bin\Debug\netstandard2.0\*.dll %target% /y
pause
