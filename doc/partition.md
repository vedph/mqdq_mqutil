# Partitioning

## Rationale

As we are dealing with text, we must define a partitioning criterion. We should examine the documents typical structure(s) and metrics to define the best strategy.

Of course, in the context of such editing system, we could not think of storing the whole text in a single unit, like in a big text file; this would not be scalable, hamper the highly networked nature of the parts, and make impossible to concurrently edit different parts of it. We must then partition our documents.

This partitioning must be driven by the intrinsic structure of the text, so that each resulting text part represents a meaningful, self-contained unit. For instance, in a corpus of inscriptions the item would be an inscription; in a corpus of epigrams, it would be an epigram; in a prose corpus of Platonic dialogs, it would be a paragraph. As for their typographical nature, paragraphs are modern units determined by the traditional text layout; but of course they obey the text meaning, by grouping one or more complete sentences into a relatively self-contained unit.

Of course this is an arbitrary choice; but, in a sense, this type of text partitioning is no different from the divisions applied to the structure of a TEI document. For instance, imagine a Plato's dialog where each part is a paragraph, and a corresponding TEI text where each paragraph is marked by p: as for archiving, the substantial difference would be that each paragraph gets separately stored in the database, while it is usually contained in a single file in XML. Joining text parts to rebuild the unique flow of base text would of course be trivial, whether we are exporting data to generate TEI, or just displaying a continuous text.

Naturally, such decomposition, functional to the software requirements and to the high density of metatextual data with their multiple connections, would be an overkill if applied outside the scenarios which practically defined the birth of the system itself.

The partition function targets MQDQ XML document comments. Its primary output will be a copy of the original document, with some `pb` elements inserted. This element is chosen because `pb` is never used in the original documents, and we need an empty element to avoid breaking the existing XML structure.

An importer will then just have to look at these `pb` elements, and store an item for each partition, with a text part and its citation.

## Procedure

The general procedure for partitiong is:

- if there is `div2`, partition = `div2`.
- else: look at `div1@type` value:
  - `section`, `fragment`, `fragments`: e.g. a book in the Aeneid, or a poem in Catullus, or a fragment: partition = `div1`. For value section, the `div1` might be too long (e.g. a book in the Aeneid). In this case, refer to the algorithm below ('too long' here means >M).
  - work: a full work without further divisions: partition = `div1`. If >M, apply partitioning.

For partitioning case `div1@type=section` when this is too long (e.g. >50 lines), we must use an algorithmic approach, following these principles:

- min lines count treshold = N;
- max lines count treshold = M;
- break after the first `l` whose content matches the regex `[\u037e.?!][^\p{L}]*$` (=stop/exclamation/question mark at line end);
- if no match, prefer the largest partition (if any) below N, or just force a break at M;
- in the corner case where the starting `l` of the next partition is the last child of `div1`, this will be joined to the previous partition.
