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
.\Mqutil.exe import-thes E:\Work\mqdq\ *-app.xml E:\Work\mqdqc\thesauri.json
```

Note that as we are just parsing headers we do not need to refer to the apparatus files produced by merging overlaps; we can directly use the original files.
