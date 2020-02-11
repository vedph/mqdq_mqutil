# Import

**NOTE**: this is just a plan; development will follow once it is fully defined.

Before importing, ensure that all the documents requiring [partitioning](partition.md) have been partitioned. The other documents instead can be manually copied in the import directory as they are.

The import process implies two essential operations: parsing XML to extract data from it, and remodel extracted data by following the Cadmus architecture.

This remodeling is heavily conditioned by two capital factors:

- the requirement to carry on a lot of metadata which, though redundant for the editor, are necessary to inject edited data back into their legacy source XML documents.

- the requirement to provide a simple yet strongly checked editing experience, allowing non technical users to edit data without the risk of breaking any of the links to the legacy data.

Now, as far as we can tell from the legacy XML, it provides a lot of metadata up to the word domain (`w` elements inside `l`), and we have been told that most of these metadata, though apparently regular, cannot be algorithmically regenerated (as there are cases of manual interventions).

Also, a text partition (essentially a `div`) has no granted content: usually its content is just lines (`l` elements), but there are a number of other children, e.g. `p` for an unmetrical text or speaker (I suppose here `speaker` was not used because it required a `sp`eech parent), `head` for headings, etc. Further, virtually each element has an explicit ID, which cannot be algorithmically generated.

We thus must provide a model which preserves all these legacy data, yet allowing users to edit them.

## Workflow Plan

### Text

The general procedure for importing could be implemented as follows:

1. open the XML text document. If it contains any `pb` element, it's a partitioned document; else, it's an unpartitioned document (=a document which did not require partitioning).

2. determine the partitions *boundaries*:

- for *unpartitioned* documents, each partition is either `div2` (when any `div2` is present), or `div1` (when no `div2` is present), as a whole.
- for *partitioned* documents, each partition is all the `l`/`p` children elements of each `div1` (with all of their descendants), up to the first `pb` child, or up to the `div1` end. We speak of `div1` only here, as [partitioning](partition.md) never applies to `div2`.

3. determine the partitions *citations*:

- for partitions closed by `pb`, each `pb@n` attribute contains the citation of the partition ending with it.
- else, each partition must build its citation from the `div2`/`div1` parent element, just like the citation built by the [partitioner](partition.md).

4. model as follows:

- **item**: the partition corresponds to a new item, whose title is the citation.
- **tiled text part**: the partition's text are all the `div`'s `l` or `p` children. Each of these elements becomes a tiles row. All its attributes and the element's name itself (under key `_name`, which is either `l` or `p`) become row's metadata. The `xml:id` attribute gets translated into `id` (semicolons cannot be used as metadata keys). Inside each `l` or `p` element, we do as follows:
  - if there are `w` children elements, import each into a tile. The attributes of each `w` elements become the tile's metadata (transforming `xml:id` as above). The text content of the `w` element becomes the tile's `text` metadatum. If this text includes a legacy escape (type `(==...)`), the escape is removed and its value added as a `patch` metadatum.
  - if there is just a child text node, split it into graphical words, treating them as above for "true" `w` elements. The only difference is that their IDs get generated, and their row gets an additional `_split` metadatum which preserves the information about this splitting happened at the import level.

Full lines or paragraphs are split into words because users might want to add an apparatus or other metadata entry to that text. We thus split every text when importing it; then, users will be able to edit metadata at will. Once exporting, we will just reassemble the full, unsplit text when we find that no such editing occurred. We can easily spot when this happened, by looking at those rows with `split` attribute having no tile connected to any of the item's layers.

5. store the item and its text part in the target database.

Once this phase is completed, we have remodeled and imported all the works text into a Cadmus database. We now have to turn to apparatus.

### Apparatus

1. check if an apparatus exists for the text file opened. By convention, apparatus documents have the same file location and name of the text document, with the addition of an `-app` suffix in their name.

TODO: process apparatus...

## Commands

### Parse Text

The parse text command is used to parse text documents, dumping the output into a set of JSON files including the Cadmus items and text parts.

Syntax:

```ps1
.\Mqutil.exe partition <InputFilesMask> <OutputDir> [-m MaxItemsPerFile]
```

where:

- `InputFilesMask` is the input file(s) mask.
- `OutputDir` is the output directory (will be created if not exists).
- `-m` is the maximum count of desired items per output file. The default value is 100. Set to 0 to output a single file (not recommended unless your input files are small).

Output files will be created in the output directory, and named after the corresponding input files, plus a numeric suffix.
