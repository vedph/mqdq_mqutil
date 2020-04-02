# Import

## Overview

Before importing, ensure that all the documents requiring [partitioning](partition.md) have been partitioned. The other documents instead can be manually copied in the import directory as they are.

The import process implies two essential operations: *parsing* XML to extract data from it, and *remodeling* extracted data by following the Cadmus architecture.

This remodeling is heavily conditioned by two capital factors:

- the requirement to preserve a lot of metadata which, though redundant for the editor, are necessary to inject edited data back into their legacy source XML documents.

- the requirement to provide a simple, yet strongly checked editing experience, allowing non technical users to edit data without the risk of breaking any of the links to the legacy data.

Our legacy XML provides a lot of metadata, up to the word domain (`w` elements inside `l`), and we have been told that most of these metadata, though apparently regular, cannot be algorithmically regenerated (as there are cases of manual interventions).

Also, a text partition (essentially a `div`) has no granted content: usually its content is just lines (`l` elements), but there are a number of other children, e.g. `p` for an unmetrical text or speaker (I suppose here `speaker` was not used because it required a `sp`eech parent), `head` for headings, etc. Further, virtually each element has an explicit ID, which cannot be algorithmically generated.

We thus must provide a model which preserves all these legacy data, yet allowing users to edit them.

To this end, we are not going to fully regenerate the whole XML document; rather, we pick some parts from it, edit them, and reinject data serialized as XML branches into the original documents.

## Workflow Plan

### 1. Text

Data from scan:

- [XML tree](mqdq-txt-report.html)
- [characters counts](mqdq-txt-chars.tsv)

The general procedure for importing is implemented as follows:

1. open the XML text document. If it contains any `pb` element, it's a partitioned document; else, it's an unpartitioned document (=a document which did not require partitioning).

2. determine the partitions *boundaries*:

- for *unpartitioned* documents, each partition is either `div2` (when any `div2` is present), or `div1` (when no `div2` is present), as a whole.
- for *partitioned* documents, each partition is all the `l`/`p` children elements of each `div1` (with all of their descendants), up to the first `pb` child, or up to the `div1` end. We speak of `div1` only here, as [partitioning](partition.md) never applies to `div2`.

3. model as follows:

- **item**: the partition corresponds to a new item.
- **tiled text part**: the partition's text are all the `div`'s `l` or `p` children. Each of these elements becomes a tiles row. All its attributes and the element's name itself (under key `_name`, which is either `l` or `p`) become row's metadata. The `xml:id` attribute gets translated into `id` (semicolons cannot be used as metadata keys). Inside each `l` or `p` element, we do as follows:
  - if there are `w` children elements, import each into a tile. The attributes of each `w` elements become the tile's metadata (transforming `xml:id` as above). The text content of the `w` element becomes the tile's `text` metadatum. If this text includes a *legacy escape* (type `(==...)`), the escape is removed and its value added as a `patch` metadatum.
  - if there is just a child text node, split it into graphical words, treating them as above for "true" `w` elements. The only difference is that their IDs get generated, and their row gets an additional `_split` metadatum which preserves the information about this splitting happened at the import level.

Full lines or paragraphs are split into words because users might want to add an apparatus or other metadata entry to that text. We thus split every text when importing it; then, users will be able to edit metadata at will. Once exporting, we will just reassemble the full, unsplit text when we find that no such editing occurred. We can easily spot when this happened, by looking at those rows with `split` attribute having no tile connected to any of the item's layers.

Once this phase is completed, we have remodeled and imported all the works text into a set of JSON files, representing the dumps of the objects which will be stored in the Cadmus database. This is more useful than directly storing them in the database, as it allows for checking by simply looking at text files.

### 2. Apparatus

Importing [apparatus](apparatus.md) requires that text have been parsed first, so that we have their JSON dumps available.

### 3. Thesauri

Finally, we import thesauri by scanning all the apparatus documents. This produces a single JSON dump with all the thesauri, 2 for each document (for witnesses and authors).

This document will be then pasted into the Cadmus profile configuration file.
