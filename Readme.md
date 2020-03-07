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
.\Mqutil.exe partition C:\Users\dfusi\Desktop\mqdq\VERG-eclo.xml C:\Users\dfusi\Desktop\mqdq\part\
```

### Parse Text Command

The `parse-text` command is used to parse text documents, dumping the output into a set of JSON files including the Cadmus items and text parts.

Syntax:

```ps1
.\Mqutil.exe parse-text <InputFilesDir> <InputFilesMask> <OutputDir> [-m MaxItemsPerFile] [-r]
```

where:

- `InputFilesDir` is the input file(s) directory.
- `InputFilesMask` is the input file(s) mask.
- `OutputDir` is the output directory (will be created if not exists).
- `-m` is the maximum count of desired items per output file. The default value is 100. Set to 0 to output a single file (not recommended unless your input files are small).
- `-r` means that the files mask is a regular expression.

Sample:

```ps1
.\Mqutil.exe parse-text C:\Users\dfusi\Desktop\mqdq\part\VERG-eclo.xml c:\users\dfusi\desktop\mqdq\part\txt
```

Output files will be created in the output directory, and named after the corresponding input files, plus a numeric suffix.

### Parse Apparatus Command

The `parse-app` command parses the apparatus XML documents, dumping the results into a set of JSON files.

Syntax:

```ps1
.\Mqutil.exe parse-app <InputFilesDir> <InputFilesMask> <TextDumpDir> <OutputDir> [-m MaxItemsPerFile] [-r]
```

where:

- `InputFilesDir` is the input file(s) directory.
- `InputFilesMask` is the input file(s) mask.
- `TextDumpDir` is the directory containing the JSON text dumps. These are the output of the `parse-text` command.
- `OutputDir` is the output directory, where JSON apparatus dumps will be saved.
- `-m` is the maximum count of desired items per output file. The default value is 100. Set to 0 to output a single file (not recommended unless your input files are small).
- `-r` means that the files mask is a regular expression.

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
