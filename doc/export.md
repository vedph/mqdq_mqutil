# Export

## Overview

Given the nature of the documents and the requirements of legacy software, the export procedure in this case is meant to "edit" the existing TEI documents, rather than recreating them from scratch. Recreating the whole documents from scratch would be the way to go for a true export, where the data source is directly the Cadmus database. Here instead we are dealing with a set of legacy TEI documents where every detail about their structure, IDs, and metadata must be preserved, and where we are interested in editing only a subset of the data (those which get [imported](import.md)). This subset is first imported into the database; gets edited in it; and then it exported, i.e. injected back into the original TEI documents.

This makes both the import and export processes a corner case, which coupled with the short deadlines for the editing system to be operational suggests an ad-hoc solution for MQDQ. Yet, this will provide an initial experience in dealing with the remodeling of TEI data into the Cadmus architecture, and vice-versa, which will be useful beyond its specific purpose.

## Exporting Text

The plan for exporting text is described here.

Each set of items belonging to the same work must be injected back into the corresponding TEI text file. This means that we must first collect all the distinct items group IDs from the database, which gives us the list of the works; and then process items grouped by each of these group IDs, in the order defined by their sort key (which reflects their original order).

So, our procedure would essentially be:

for each group ID in the list {

- open the *targetDocument*;
- assume that we must output `w` elements in the text when either *targetDocument* has at least 1 `w`, or any of the corresponding items has a non-empty layer part; this should output `w` only for those documents which originally had it, and additionally for those documents which did not have them, but are now required to have it because layers were added. Note that when importing, all the texts were treated as if they had `w` elements, because this allows adding layers when needed;
- for each `div1`/`div2` in *targetDocument*, remove all its children except for the eventual `head` child at its head; this is because we're going to replace the content with `p` and `l` elements from our items;
- for each item having the current group ID, sorted by sortKey {
  - locate the corresponding `div` element from its `@xml:id` (assuming that every `div`, either `div1` or `div2`, had a document-wide unique ID; this id has been imported in the item's title), and log an error if not found (this should not happen);
  - get the item's text part; if not found, log an error and continue with the next item (this should not happen);
  - build and append to the target `div` element the `l`/`p` elements from its rows and tiles, with their attributes which have been preserved in each tile metadata; the content of these elements is just the joined text of all the row's tiles when we must not output `w` elements; else it's a set of `w` elements, one for each tile in the row;

  } end item for

} end group for
