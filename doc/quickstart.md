# Quick Start

This is a sample showing the full import procedure.

To start with, copy `VERG-eclo-app.xml` and `VERG-eclo.xml` into an `mqdq` folder. Mine is in my desktop (`C:\Users\dfusi\Desktop\`), and this is the location I'll use in this sample.

You can then follow these steps which summarize the import flow:

(1) **partition**: partition the text documents. This will create a new VERG-eclo.xml document under `...\mqdq\part`.

```ps1
.\Mqutil.exe partition C:\Users\dfusi\Desktop\mqdq\ VERG-eclo.xml C:\Users\dfusi\Desktop\mqdq\part\
```

This document is identical to the original one, except for the insertion of `pb` elements at the end of each partition. These elements never appear in the original documents.

(2) **parse text**: parse the partitioned text documents, dumping the output into a set of 1 or more JSON files:

```ps1
.\Mqutil.exe parse-text C:\Users\dfusi\Desktop\mqdq\part\ VERG-eclo.xml c:\users\dfusi\desktop\mqdq\part\txt
```

The output JSON file(s) will be found under `...\mqdq\part\txt`, named after the original documents (e.g. `VERG-eclo_00001.json` etc.).

(3) **parse apparatus**: parse the apparatus documents, combining them with the dumps produced by the text parser to compute the fragments locations:

```ps1
.\Mqutil.exe parse-app C:\Users\dfusi\Desktop\mqdq\ VERG-eclo-app.xml c:\users\dfusi\desktop\mqdq\part\txt\ c:\users\dfusi\desktop\mqdq\part\app
```

The output JSON file(s) will be found under `...\mqdq\part\app`, named after the original documents (e.g. `VERG-eclo-app_00001.json` etc.).

The log file is named `mqutil-log...txt` (where `...` is a set of digits representing year, month and day) under the same folder of the program. To look for error inside it, search for the `[ERR]` string. The file is written as a cyclic buffer, so if you want to get only the log of the last run just delete it before launching the `parse-app` command.

(4) **import thesauri**: parse the apparatus documents, extracting thesauri data from `listBib` and `listWit` into JSON-serialized thesauri. These are ready to be pasted into a Cadmus profile.

```ps1
.\Mqutil.exe import-thes C:\Users\dfusi\Desktop\mqdq\ VERG-eclo-app.xml c:\users\dfusi\desktop\mqdq\thesauri.json
```
