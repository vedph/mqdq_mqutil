# MQDQ Migration Tool

This is a dummy tool to be used for importing MQDQ documents into Cadmus for editing, and to export them back once finished.

This tool has a CLI interface. Its available commands are listed below. You can get more information about the syntax of each command by typing it followed by `--help`.

See the `doc` folder in this repository for the [documentation](doc/index.md).

## Requirements

If not using a development machine (which has the SDK installed), you must install the [.NET Core runtime](https://dotnet.microsoft.com/download/dotnet-core) to run this program.

## Syntax

Note: for Linux users, you should run the program like this:

```bash
dotnet ./Mqutil.dll ...arguments...
```

### Report Overlaps Command

Parse the MQDQ apparatus and text documents creating a Markdown overlaps report into the specified file.

Syntax:

```ps1
.\Mqutil.exe <ApparatusFilesDir> <ApparatusFilesMask> <OutputFilePath> [-r] [-s]
```

where:

- `ApparatusFilesDir` is the apparatus file(s) directory.
- `ApparatusFilesMask` is the apparatus file(s) mask.
- `OutputFilePath` is the output file path.
- `-r` means that the files mask is a regular expression.
- `-s` recurses subdirectories when matching input files.

Sample:

```ps1
.\Mqutil.exe report-overlaps E:\Work\mqdq\ *-app.xml E:\Work\mqdqc\overlaps.md -s
```

### Remove Overlaps Command

Remove app overlaps from text documents saving the updated documents into the specified directory.

Syntax:

```ps1
.\Mqutil.exe <ApparatusFilesDir> <ApparatusFilesMask> <OutputDir> [-r] [-s] [-d]
```

where:

- `ApparatusFilesDir` is the apparatus file(s) directory.
- `ApparatusFilesMask` is the apparatus file(s) mask.
- `OutputDir` is the output directory. If it does not exist, it will be created.
- `-r` means that the files mask is a regular expression.
- `-s` recurses subdirectories when matching input files.
- `-d` additionally writes a file named `overlap-err-divs.txt` in the output directory, having a line for each distinct ID of `div`'s containing overlap removal errors. This can be used later in import, to flag the corresponding items.

Sample:

```ps1
.\Mqutil.exe remove-overlaps E:\Work\mqdq\ *-app.xml E:\Work\mqdqc\app\ -s -d
```

### Partition Command

The `partition` command partitions all the files matching the specified mask and requiring partitioning, saving a copy of each file (either partitioned or not) in the specified output directory.

The output files will be equal to the input files, except for the addition of `pb` elements at the end of each partition.

Syntax:

```ps1
.\Mqutil.exe partition <InputFilesDir> <InputFilesMask> <OutputDir> [-n Min] [-m Max] [-r]
```

where:

- `InputFilesDir` is the input file(s) directory.
- `InputFilesMask` is the input file(s) mask.
- `OutputDir` is the output directory (will be created if not exists).
- `-n` is the optional minimum treshold (default 20).
- `-m` is the optional maximum treshold (default 50).
- `-r` means that the files mask is a regular expression.
- `-s` recurses subdirectories when matching input files.

Sample:

```ps1
.\Mqutil.exe partition E:\Work\mqdq\ "^[^-]+-[^-]+\.xml" E:\Work\mqdqc\txt\ -r -s
```

### Parse Text Command

The `parse-text` command is used to parse text documents, dumping the output into a set of JSON files including the Cadmus items and text parts.

Syntax:

```ps1
.\Mqutil.exe parse-text <InputFilesDir> <InputFilesMask> <OutputDir> [-m MaxItemsPerFile] [-r] [-d div-list-path]
```

where:

- `InputFilesDir` is the input file(s) directory.
- `InputFilesMask` is the input file(s) mask.
- `OutputDir` is the output directory (will be created if not exists).
- `-m` is the maximum count of desired items per output file. The default value is 100. Set to 0 to output a single file (not recommended unless your input files are small).
- `-r` means that the files mask is a regular expression.
- `[-d div-list-path]` specifies the path to a text file containing a list of div IDs to be flagged with 1. This list is optionally produced by the `remove-overlaps` command (see above). The typical usage is flagging for revision items with overlap errors.

Sample:

```ps1
.\Mqutil.exe parse-text E:\Work\mqdqc\txt\ *.xml E:\Work\mqdqc\jtxt\ -d E:\Work\mqdqc\app\~overlap-err-divs.txt
```

Output files will be created in the output directory, and named after the corresponding input files, plus a numeric suffix.

### Parse Apparatus Command

The `parse-app` command parses the apparatus XML documents, dumping the results into a set of JSON files.

Syntax:

```ps1
.\Mqutil.exe parse-app <InputFilesDir> <InputFilesMask> <TextDumpDir> <OutputDir> [-m MaxItemsPerFile] [-r] [-s]
```

where:

- `InputFilesDir` is the input file(s) directory.
- `InputFilesMask` is the input file(s) mask.
- `TextDumpDir` is the directory containing the JSON text dumps. These are the output of the `parse-text` command.
- `OutputDir` is the output directory, where JSON apparatus dumps will be saved.
- `-m` is the maximum count of desired items per output file. The default value is 100. Set to 0 to output a single file (not recommended unless your input files are small).
- `-r` means that the files mask is a regular expression.
- `-s` recurses subdirectories when matching input files.

Sample:

```ps1
.\Mqutil.exe parse-app C:\Users\dfusi\Desktop\mqdq\VERG-eclo-app.xml c:\users\dfusi\desktop\mqdq\part\txt\ c:\users\dfusi\desktop\mqdq\part\app
```

### Import Thesauri Command

The `import-thes` thesauri command is used to parse apparatus documents to extract witnesses and authors into a set of JSON thesauri.

Syntax:

```ps1
.\Mqutil.exe import-thes <InputFilesMask> <OutputFilePath> [-r]
```

where:

- `InputFilesMask` is the input file(s) mask.
- `OutputFilePath` is the output file path.
- `-r` means that the files mask is a regular expression.

Sample:

```ps1
.\Mqutil.exe import-thes C:\Users\dfusi\Desktop\mqdq\VERG-eclo-app.xml c:\users\dfusi\desktop\mqdq\thesauri.json
```

### Import JSON Command

The `import-json` command imports a set of JSON dumps representing parsed text and apparatus into a Cadmus database.

Syntax:

```ps1
.\Mqutil.exe import-json <JsonTextFilesDir> <JsonTextFilesMask> <JsonApparatusFilesDir> <JsonProfileFile> <DatabaseName> [-d] [-r]
```

where:

- `JsonTextFilesDir` is the input JSON text files directory.
- `JsonTextFilesMask` is the input JSON text files mask.
- `JsonApparatusFilesDir` is the JSON apparatus files directory.
- `JsonProfileFile` is the JSON profile file path.
- `DatabaseName` is the target database name.
- `-d` triggers a dry run, where nothing is written to the database.
- `-r` means that the files mask is a regular expression.

Sample:

```ps1
.\Mqutil.exe import-json E:\Work\mqdqc\jtxt\ *.json E:\Work\mqdqc\japp\ E:\Work\mqdqc\mqdq-profile.json mqdq -d
```

## Prepare Export Command

The `prepare-export` command prepares the output directory for exporting. This implies copying all the text files in their corresponding subdirectories from the original files directory, and adding to each the corresponding apparatus file (when present) from the directory with the apparatus files processed to remove overlaps.

Syntax:

```ps1
.\Mqutil.exe prepare-export <OriginalFilesDir> <ProcessedApparatusFilesDir> <OutputDir>
```

where:

- `OriginalFilesDir` is the root directory of the original files. This contains a subdirectory for each author.
- `ProcessedApparatusFilesDir` is the directory containing the apparatus files where overlaps have been removed.
- `OutputDir` is the directory to be prepared for export. If existing, it will removed before being recreated.

Sample:

```ps1
.\Mqutil.exe prepare-export E:\Work\mqdq\ E:\Work\mqdqc\app\ E:\Work\mqdqe\
```

## Export Text Command

The `export-text` command exports the text from database into existing TEI documents, replacing their content where needed.

Syntax:

```ps1
.\Mqutil.exe export-text <DatabaseName> <OutputDir> [-c] [-d]
```

where:

- `DatabaseName` is the source database name.
- `OutputDir` is the directory where TEI text documents to be updated are located.
- `-c` adds comments in the output files.
- `-d` triggers a dry run, where nothing is written to the output files.

Sample:

```ps1
.\Mqutil.exe export-text mqdq E:\Work\mqdqe\ -c -d
```

## Export Apparatus Command

The `export-app` command exports the apparatus into existing TEI files.

Syntax:

```ps1
.\Mqutil.exe export-app <DatabaseName> <OutputDir> [-c] [-d]
```

where:

- `DatabaseName` is the source database name.
- `OutputDir` is the directory where TEI text documents to be updated are located.
- `-c` adds comments in the output files.
- `-d` triggers a dry run, where nothing is written to the output files.

Sample:

```ps1
.\Mqutil.exe export-app mqdq E:\Work\mqdqe\ -c -d
```

## Add Credit Command

The `add-credit` command adds a `respStmt` element to the header under `TEI/teiHeader/fileDesc/seriesStmt/`, with this structure:

```xml
<respStmt>
  <resp key="MQDQ">...</resp>
    <persName>...</persName>
</respStmt>
```

Syntax:

```ps1
.\Mqutil.exe add-credit <TextFilesRootDir> <RespValue> <PersValue> [-d]
```

where:

- `TextFilesRootDir` is the root directory of the TEI text files. Each text file is found under a subdirectory in this root.
- `RespValue` is the value for the `resp` element.
- `PersValue` is the value for the `persName` element under `resp`.
- `-d` triggers a dry run, where nothing is written to the output files.

## Processing Corpus

To process the whole corpus from the original TEI files to the database and back to the updated TEI files, follow the procedure described in this section.

### 1. Creating Files

We can process the whole corpus using a batch like this; just replace the values for `srcdir` (=the source MQDQ directory, as downloaded), `dstdir` (=the root target directory, a new folder in your target drive), and `mqu` (the path to the `Mqutil` program):

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
echo (you should update the MQDQ profile with thesauri)
pause

echo DRY IMPORT DATABASE
%mqu% import-json %dstdir%jtxt\ *.json %dstdir%japp\ %dstdir%mqdq-profile.json mqdq -d
pause

echo IMPORT DATABASE
%mqu% import-json %dstdir%jtxt\ *.json %dstdir%japp\ %dstdir%mqdq-profile.json mqdq
pause
```

Note that before importing the database you should update the profile thesauri, as generated by `import-json`.

The partition command targets only the texts; this is why we're using a regular expression to match them only, excluding apparatuses. The parse apparatus command instead does the inverse, but here a simple file mask is enough. In both cases, given that each document is in its own subfolder, we add the `-s` option to recurse subdirectories.

Note that once files have been partitioned, all the following text-related processing happens on the partitioned files, rather than on the original ones.

### 2. Importing Database

To create a Cadmus database and import JSON files (only if you stopped the above batch before importing):

```ps1
.\Mqutil.exe import-json E:\Work\mqdqc\jtxt\ *.json E:\Work\mqdqc\japp\ E:\Work\mqdqc\mqdq-profile.json mqdq -d
```

Remove the `-d` option to disable dry run and truly import data. You will now find a MongoDB database named `mqdq`. If the database already exists, remove it before importing.

### 3. Exporting Database

```ps1
.\Mqutil.exe prepare-export E:\Work\mqdq\ E:\Work\mqdqc\app\ E:\Work\mqdqe\
.\Mqutil.exe export-text mqdq E:\Work\mqdqe\ -c -d
.\Mqutil.exe export-app mqdq E:\Work\mqdqe\ -c -d
```

Remove `-d` to effectively write to the files (`-c` adds comments, which are usually kept for diagnostic purposes):

```ps1
.\Mqutil.exe export-text mqdq E:\Work\mqdqe\ -c
.\Mqutil.exe export-app mqdq E:\Work\mqdqe\ -c
```

### 4. Adding Credits

To add credits in the header, we can do this either in the original files or in the exported files. It must be done only once, as the next editing cycle will just pick up the existing header with the additional credits.

```ps1
.\Mqutil.exe add-credit E:\Work\mqdqe\ "Software for data migration, remodeling and editing" "Daniele Fusi" -d
```

Remove `-d` to effectively write to the files.

### 5. Adding Pins Index

The pins index is managed by the Cadmus editor; whenever a part is saved, it gets updated. When importing a database, it must be created before using Cadmus, as in this case all the parts get imported without their pins index. The tool is the Cadmus API tool named `CadmusTool`, which can be used with this syntax:

```ps1
.\CadmusTool.exe index mqdq E:\Work\mqdqc\mqdq-profile.json
```

This creates a new MySql database named `index` using the specified Cadmus profile for MQDQ.

### Files Overview

If using the above batch, or a corresponding folder structure, you will have the following folders under some root folder including all the MQDQ materials:

- `mqdqc`: the Cadmus-converted artifacts. This folder contains all the files used by the import process.
  - `app`: the XML files for apparatuses, with overlaps removed.
  - `txt`: the XML files for texts, partitioned where required.
  - `japp`: the result of parsing XML apparatus files into a set of JSON objects, ready for import in Cadmus.
  - `jtxt`: the result of parsing XML text files into a set of JSON objects, ready for import in Cadmus.
  - `overlaps.md`: the overlaps report generated by the report-overlaps command.
  - `thesauri.json`: the thesauri extracted from XML text files headers, ready to be included in a Cadmus profile.
- `mqdqe`: the Cadmus-exported files. This folder contains the result of exporting a Cadmus MQDQ database into XML files. It contains the same folder structure of the original files, where every work is under its own folder, named after its author (uppercase) and title (lowercase, prefixed by a dash). This structure is derived by just copying all the original files in this folder, and then using them as the export target for both text and apparatus.
