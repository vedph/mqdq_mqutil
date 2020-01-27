# MQDQ Migration Tool

This is a dummy tool to be used for importing MQDQ documents into Cadmus for editing, and to export them back once finished.

This tool has a CLI interface. Currently the following commands are planned:

- **partition**: partition text documents.
- **import**: import partitioned text documents, plus apparatus where available, into a Cadmus database.
- **export**: export data from the Cadmus database into the source XML documents.

See the `doc` folder in this repository for the documentation.

Note for users: if not using a development machine (which has the SDK installed), you must install the [.NET Core runtime](https://dotnet.microsoft.com/download/dotnet-core) to run this program.
