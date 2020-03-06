@echo off
echo UPDATE PLUGINS

set target=Mqutil\bin\Release\netcoreapp3.1\Plugins\

md %target%
del %target%*.* /q

xcopy ..\..\Cadmus\Cadmus.Parts\bin\Release\netstandard2.0\*.dll %target% /y
xcopy ..\..\Cadmus\Cadmus.Lexicon.Parts\bin\Release\netstandard2.0\*.dll %target% /y
xcopy ..\..\Cadmus\Cadmus.Philology.Parts\bin\Release\netstandard2.0\*.dll %target% /y
pause
