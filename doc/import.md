# Import

**NOTE**: this is just a plan; development will follow once it is fully defined.

Before importing, ensure that all the documents requiring [partitioning](partition.md) have been partitioned. The other documents instead can be manually copied in the import directory as they are.

## Workflow Plan

The general procedure for importing could be implemented as follows:

1. open the XML text document. If it contains any `pb` element, it's a partitioned document; else, it's an unpartitioned document (=a document which did not require partitioning).

2. determine the partitions boundaries:

- for unpartitioned documents, each partition is either `div2` (when any `div2` is present), or `div1` (when no `div2` is present), as a whole.
- for partitioned documents, each partition is all the children elements of each `div1` (with all of their descendants), up to the first `pb` child, or up to the `div1` end.

3. determine the partitions citations:

- for partitions closed by `pb`, each `pb@n` attribute contains the citation.
- else, each partition must build its citation from the `div2`/`div1` parent element, just like the citation built by the [partitioner](partition.md).

## Modeling

Modeling here is heavily conditioned by two capital factors:

- the requirement to carry on a lot of metadata which, though redundant for the editor, are necessary to inject edited data back into their legacy source XML documents.

- the requirement to provide a simple yet strongly checked editing experience, allowing non technical users to edit data without the risk of breaking any of the links to the legacy data.

Now, as far as we can tell from the legacy XML, it provides a lot of metadata up to the word domain (`w` elements inside `l`), and we have been told that most of these metadata, though apparently regular, cannot be algorithmically regenerated (as there are cases of manual interventions).

Also, a text partition (essentially a `div`) has no granted content: usually its content is just lines (`l` elements), but there are a number of other children, e.g. `p` for an unmetrical text or speaker (I suppose here `speaker` was not used because it required a `sp`eech parent), `head` for headings, etc. Further, virtually each element has an explicit ID, which cannot be algorithmically generated.

TODO: from here

The outcome of these operations is:

- 1 **item** per partition; its title will be equal to the concatenation of the following portions of the partition citation: file name, space, and `l`'s `id`. For instance, `LVCR-rena 00122` from `LVCR-rena xml:id=d001|type=section|decls=#md|met=H 12#00122`. This should allow sorting the items in their natural order, by just sorting them by title (which is what is done by the standard item sort key generator in Cadmus, apart from normalizations).

- 1 **tiled text part** per item. This contains the partition's text, where each line is a verse. When importing this text (=the content of a partition in the sense defined above) we take into account only `l` and `p` children; they can just bear a single text node, or a number of `w` elements, one for each word. The escape `(==...)` (orthographic patches) may appear in any text content, and is processed so that it becomes metadata of the imported tile.

TODO: apparatus. This must be retrieved from files having it, and mapped to parts.
