# MQDQ Utility Tool

This is a dummy tool to be used for importing MQDQ documents into Cadmus for editing, and to export them back once finished.

- [quick start](quickstart.md): quick start sample.
- [text](text.md): analysis of XML text documents.
- [partition](partition.md): partition text documents.
- [apparatus](apparatus.md): apparatus analysis and import plan.
- [apparatus sample](apparatus-sample.md): sample of apparatus parsing.
- [import](import.md): import plan.
- [export](export.md): export plan.

## Directions for Checking

- [check remove overlap](ck-remove-overlaps.md)
- [check partitioning](ck-partition.md)
- [check export text](ck-export-text.md)
- [check export apparatus](ck-export-app.md)

For those brave solus willing to use the MongoDB database in their checking, its full dump is included in its compressed format (`agz`). You can restore it using the `mongorestore` utility, or use a GUI if you prefer.

I can list two GUIs: [Mongo Compass](https://www.mongodb.com/products/compass) and [Studio 3T for MongoDB](https://studio3t.com/), which has a lot more functions but requires you to register; its Academic license is free.

If you explore this database, named mqdq, you should just use two collections for checking:

- `items`
- `parts`

You can pick a specific item or part by just querying for its ID, which in several cases is listed in the log or appears in comments inside the XML output, wherever relevant.
