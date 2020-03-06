# MQDQ Migration Tool

This is a dummy tool to be used for importing MQDQ documents into Cadmus for editing, and to export them back once finished.

This tool has a CLI interface. Currently the following commands are planned:

- **partition**: partition text documents.
- **import**: import partitioned text documents, plus apparatus where available, into a Cadmus database.
- **export**: export data from the Cadmus database into the source XML documents.

See the `doc` folder in this repository for the [documentation](doc/index.md).

Note for users: if not using a development machine (which has the SDK installed), you must install the [.NET Core runtime](https://dotnet.microsoft.com/download/dotnet-core) to run this program.

## Syntax

Note: for Linux users, you should run the program like this:

```bash
dotnet ./Mqutil.dll ...arguments...
```

### Partition

The `partition` command partitions all the files matching the specified mask and requiring partitioning, saving a copy of each partitioned file in the specified output directory.

The output files will be equal to the input files, except for the addition of `pb` elements at the end of each partition.

Syntax:

```ps1
.\Mqutil.exe partition <InputFilesMask> <OutputDir> [-n Min] [-m Max]
```

where:

- `InputFilesMask` is the input file(s) mask.
- `OutputDir` is the output directory (will be created if not exists).
- `-n` is the optional minimum treshold (default 20).
- `-m` is the optional maximum treshold (default 50).

Just launch the program without arguments to get help directions. This gets a generic help, which also tells you how to get help about any specific command.

Sample:

```ps1
.\Mqutil.exe partition C:\Users\dfusi\Desktop\mqdq\VERG-eclo.xml C:\Users\dfusi\Desktop\mqdq\part\
```

### Parse Text

The `parse-text` command is used to parse text documents, dumping the output into a set of JSON files including the Cadmus items and text parts.

Syntax:

```ps1
.\Mqutil.exe parse-text <InputFilesMask> <OutputDir> [-m MaxItemsPerFile]
```

where:

- `InputFilesMask` is the input file(s) mask.
- `OutputDir` is the output directory (will be created if not exists).
- `-m` is the maximum count of desired items per output file. The default value is 100. Set to 0 to output a single file (not recommended unless your input files are small).

Sample:

```ps1
.\Mqutil.exe parse-text C:\Users\dfusi\Desktop\mqdq\part\VERG-eclo.xml c:\users\dfusi\desktop\mqdq\part\txt
```

Output files will be created in the output directory, and named after the corresponding input files, plus a numeric suffix.

### Parse Apparatus

The `parse-app` command parses the apparatus XML documents, dumping the results into a set of JSON files.

TODO: add parse-app doc

```ps1
.\Mqutil.exe parse-app C:\Users\dfusi\Desktop\mqdq\part\VERG-eclo.xml c:\users\dfusi\desktop\mqdq\part\app
```

### Import Thesauri

The `import-thes` thesauri command is used to parse apparatus documents to extract witnesses and authors into a set of JSON thesauri.

Syntax:

```ps1
.\Mqutil.exe import-thes <InputFilesMask> <OutputFilePath>
```

where:

- `InputFilesMask` is the input file(s) mask.
- `OutputFilePath` is the output file path.

Sample:

```ps1
.\Mqutil.exe import-thes C:\Users\dfusi\Desktop\mqdq\VERG-eclo-app.xml c:\users\dfusi\desktop\mqdq\thesauri.json
```

### Import JSON

The `import-json` command imports a set of JSON dumps representing parsed text and apparatus into a Cadmus database.

TODO: add import-json doc
