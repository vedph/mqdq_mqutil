# Quick Start

This is a sample showing the full import procedure.

To start with, copy `VERG-eclo-app.xml` and `VERG-eclo.xml` into an `mqdq` folder. Mine is in my desktop (`E:\Work\mqdq\`), and this is the location I'll use in this sample; the root for the output files will be `E:\Work\mqdqc`.

You can then follow these steps which summarize the import flow:

(1) report and remove overlaps from apparatus files:

```ps1
.\Mqutil.exe report-overlaps E:\Work\mqdq\ *-app.xml E:\Work\mqdqc\~overlaps.md -s

.\Mqutil.exe remove-overlaps E:\Work\mqdq\ *-app.xml E:\Work\mqdqc\app\ -s
```

The new apparatus files are now in `...\mqdqc\app`.

(2) **partition**: partition the text documents.

```ps1
.\Mqutil.exe partition E:\Work\mqdq\ "^[^-]+-[^-]+\.xml" E:\Work\mqdqc\txt\ -r -s
```

The partitioned documents (under `...\mqdqc\txt`) are identical to the original ones, except for the insertion of `pb` elements at the end of each partition. These elements never appear in the original documents.

(3) **parse text**: parse the partitioned text documents, dumping the output into a set of 1 or more JSON files:

```ps1
.\Mqutil.exe parse-text E:\Work\mqdqc\txt\ *.xml E:\Work\mqdqc\jtxt\
```

The output JSON file(s) will be found under `...\mqdqc\jtxt`, named after the original documents (e.g. `VERG-eclo_00001.json` etc.).

(4) **parse apparatus**: parse the apparatus documents, combining them with the dumps produced by the text parser to compute the fragments locations:

```ps1
.\Mqutil.exe parse-app E:\Work\mqdqc\app\ *-app.xml E:\Work\mqdqc\jtxt\ E:\Work\mqdqc\japp
```

The output JSON file(s) will be found under `...\mqdqc\japp`, named after the original documents (e.g. `VERG-eclo-app_00001.json` etc.).

The log file is named `mqutil-log...txt` (where `...` is a set of digits representing year, month and day) under the same folder of the program. To look for error inside it, search for the `[ERR]` string. The file is written as a cyclic buffer, so if you want to get only the log of the last run just delete it before launching the `parse-app` command.

(5) **import thesauri**: parse the headers of the apparatus documents, extracting thesauri data from `listBib` and `listWit` into JSON-serialized thesauri. These are ready to be pasted into a Cadmus profile.

```ps1
.\Mqutil.exe import-thes E:\Work\mqdqc\app\*-app.xml E:\Work\mqdqc\thesauri.json
```

## Sample Batch

You can place this batch in the same folder of the downloaded MQDQ files (e.g. `E:\Work\mqdq\`):

```bat
@echo off
set srcdir=E:\Work\mqdq\
set dstdir=E:\Work\mqdqc\
set mqu=D:\Projects\Core20\Vedph\Mqutil\Mqutil\bin\Debug\netcoreapp3.1\Mqutil.exe

echo REPORT OVERLAPS
%mqu% report-overlaps %srcdir% *-app.xml %dstdir%overlaps.md -s
echo (please keep the next log for editors reference;
echo  you can just delete the current log before advancing)
pause

echo REMOVE OVERLAPS
%mqu% remove-overlaps %srcdir% *-app.xml %dstdir%app\ -s -d
pause

echo PARTITION
%mqu% partition %srcdir% "^[^-]+-[^-]+\.xml" %dstdir%txt -r -s
pause

echo PARSE TEXT
%mqu% parse-text %dstdir%txt *.xml %dstdir%jtxt\ -d %dstdir%app\~overlap-err-divs.txt
pause

echo PARSE APPARATUS
%mqu% parse-app %dstdir%app *-app.xml %dstdir%jtxt\ %dstdir%japp\
pause

echo IMPORT THESAURI
%mqu% import-thes %dstdir%app\*.xml %dstdir%thesauri.json
echo Break here if you don't want to import the database
pause

echo DRY IMPORT DATABASE
%mqu% import-json %dstdir%jtxt\ *.json %dstdir%japp\ %dstdir%mqdq-profile.json mqdq -d
pause

echo IMPORT DATABASE
%mqu% import-json %dstdir%jtxt\ *.json %dstdir%japp\ %dstdir%mqdq-profile.json mqdq
pause
```
