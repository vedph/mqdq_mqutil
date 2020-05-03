# Introduction to MQDQ Texts

I provide this introduction as a quickstart, by summarizing the most relevant information I acquired in the course of developing a software solution for migrating and editing them.

## Production Flow

The MQDQ files for both text and apparatus (where existing) have been reencoded into TEI documents. This set of files is our original corpus.

In the planned production flow, a team of editors will work to correct and enrich the existing documents, add new apparatuses, etc. They should work at the same time on a centralized, web-accessible platform. This is provided by the [Cadmus system](https://github.com/vedph/cadmus_core).

This type of editing of course implies a database, rather than a set of text files. Yet, the original TEI documents must be preserved as they feed the legacy software currently running the MQDQ online site.

Thus, the solution for this editing operation is as follows:

1. import and remodel text and apparatus from TEI documents into a Cadmus database.
2. edit in Cadmus.
3. export text and apparatus from the Cadmus database back into the original TEI documents. This allows preserving all their general infrastructure and header, while replacing their text and apparatus content.

Thus, I designed and implemented these tools:

- a set of tools for partitioning texts (for obvious reasons, we cannot think of editing a full-length word in a single web page), parsing texts, parsing apparatuses, remodel them into the Cadmus architecture, and importing all them.
- a set of tools for exporting data from the database into the original TEI documents, replacing their content with newly generated XML code.

Apart from building a fully structured database as a byproduct, this solution also has the advantage of making the editing process much easier, as standoff editing for a critical apparatus is prone to issues and errors, and usually much longer and complex for untrained operators.

## Files Overview

The MQDQ textual corpus consists of a number of UTF-8 TEI text documents eventually accompanied by a corresponding TEI apparatus document with standoff notation.

The text documents are named after this convention: `AUTHOR-title.xml`. When it exists, the corresponding apparatus has the same name suffixed with `-app`; thus, for Catullus we have `CATVLL-carm.xml` and `CATVLL-carm-app.xml`.

The texts portions with an apparatus are marked so that each single word is inside its own `w` element, having an `@xml:id` attribute. These IDs are the attachment points for the apparatus. A *word* here is just any sequence of characters delimited by whitespace.

For instance:

```xml
<l xml:id="d001l1" n="1">
  <w xml:id="d001w1">Cui</w>
  <w xml:id="d001w2">dono</w>
  <w xml:id="d001w3">lepidum</w>
  <w xml:id="d001w4">nouum</w>
  <w xml:id="d001w5">libellum</w>
</l>
```

The IDs were alogorithmically generated, but we can make no assumptions about them, as they might have been manually edited. So the "ordered" appearance like the one sampled above is the most frequent, but we cannot assume it. All what we can say is that every `w` has its own ID, unique inside the document.

Here is an apparatus entry sample:

```xml
<app from="#d001w6" to="#d001w6">
  <lem>Arida
      <add type="abstract"><i>Seru., plurimi edd.</i></add>
  </lem>
  <rdg wit="#lw1-22">arido
      <ident n="d001w6">ARIDO</ident>
  </rdg>
</app>
```

As you can see, here the ID `d001w6` is used to refer this `app` to the corresponding word in the text file.

Notice that Catullus, like other files, is only partially provided with an apparatus; thus, in the original documents a portion of the text is marked up to the word in the way sampled above; another portion lacks the `w` elements, as there is no apparatus attached to it.

This situation is anyway emended by my migration software (see below), so we can provide a Catullus text which is uniformly marked up to the word level for the whole text.

### Text Files

Explorative resources provided by using software tools of mine:

- [XML structure of text files](https://github.com/vedph/mqdq_mqutil/blob/master/doc/mqdq-txt-report.html): download this and open locally to see it, as GitHub just shows the source HTML code.
- [count of all the character codes used in the text](https://github.com/vedph/mqdq_mqutil/blob/master/doc/mqdq-txt-chars.tsv)

The XML structure document is an interactive HTML report produced by a software tool of mine, used to inspect the tree of an XML document. It provides users with a quick and interactive overview of the elements and attributes effectively used in a set of documents, together with their location and frequency.

Note that this report has been produced by scanning *all* the MQDQ text documents, and not a single one; it is thus the representation of a virtual tree, deduced from scanning all its instances. Some documents may well represent only a subset of that tree; but it is granted that all the documents either have that tree or a subset of it.

This tree is thus a sort of "schema" which tells us about which elements and attributes are effectively found in the documents. Of course, given the huge complexity of TEI, the fact that they are TEI-compliant is far from telling us the details about their structure.

The report represents the whole tree from its root. You can expand/collapse each node by clicking the +/- button at its side. Each node is followed by the total number of its occurrences, the count of its attributes types (prefixed by `@`), and its full XPath-like path. It also ends with a leftwards arrow when it's a leaf node; this arrow is empty when the element has no content.

Readers are thus encouraged to walk through this tree to grasp the general structure of text documents.

In general, the structure is as follows:

- text is inside `div1` or `div1/div2`. The details of this distribution are somewhat complex, and depend on the nature of the text being encoded. You can find more in the original documentation. For instance, Catullus just has `div1`.
- each div essentially has three types of contents:
  - an optional initial `head` child element;
  - any number of `p`/`l` children elements, eventually followed by sibling `lb` elements.

Each of the `p` or `l` elements has either a text content (when it has no apparatus) or `w` children elements, one for each word. In normalized text documents, we should find `w` children everywhere.

All the major document divisions, from `div` to `l`/`p` and `w`, have their own `@xml:id`.

#### Escapes

A special type of metadatum in the original files is conventionally encoded with an escape rather than using XML. I call these *legacy escapes*. These are introduced by `(==` and closed by `)`, e.g. `uolucres(==volucres)`.

Legacy escapes are used to include metrically-oriented spellings for some words, e.g. to disambiguate letters like `u`.

As for any other legacy encoding feature, these escapes are kept as such (which is a requirement for the legacy software), so processors and editors must be aware of them.

When editing inside Cadmus, the escapes are removed from the text and modeled as true, separate metadata. When exporting into TEI, the escapes are regenerated inside the text.

### Apparatus Files

Explorative resources provided by using software tools of mine:

- [XML structure of text files](https://github.com/vedph/mqdq_mqutil/blob/master/doc/mqdq-app-report.html): download this and open locally to see it, as GitHub just shows the source HTML code.
- [count of all the character codes used in the text](https://github.com/vedph/mqdq_mqutil/blob/master/doc/mqdq-app-chars.tsv)

The XML documents tree is as follows:

- `TEI/text/body/div1` is the multiple root for the apparatus content. The ID of each `div1` is stored as the tag of each fragment. If the fragment has a `@type`, it is appended to this tag separated by a space.
- each `div1` contains:
  - `@xml:id` always
  - `@type` always
  - 1 `head` child with header data.
  - 1-N `app` children.

The `app` elements are rebuilt by the export process and reinjected under each `div1`, past their initial `head` child (which is kept unchanged), in place of all the old `app` elements.

You can find more about apparatus modeling and discussion about the issues listed below [here](https://github.com/vedph/mqdq_mqutil/blob/master/doc/apparatus.md).

The most relevant peculiarities for `app` entries are:

- `lem`/`rdg` have a mixed content model.
- `ident` is used to contain normalized word forms with `@n` having their text ID.
- notes are part of a bigger, virtual note entity which is conventionally split into 4 combinations of elements and attributes (for the details about the content of each "section" see the original documentation):
  1. `add` with `@type`=`abstract`.
  2. `note` with `@type`=`operation`.
  3. `note` with `@type`=`details`.
  4. `add` with `@type`=`intertext`.

When importing, the virtual note is reassembled into a single text, using backticks as the section separator.

The import/export process may change two aspects of the original apparatus:

- the order of `app` elements. This is often reordered to reflect the distinction of conceptual domains made in the database for 3 "types" of apparatus: the apparatus proper, a specialized apparatus for ancient notes, and a specialized apparatus for margin notes. All of these 3 layers share the same model, but have different roles. In each layer there are entries, which are then output as `app` elements in the XML files.

- the linking to the text files:
  - `@loc` (sparse entries) is avoided and replaced by multiple `@from`/`@to` pairs grouped under the same ID.
  - in some cases, for a number of reasons the original files may present overlapping `app` elements, i.e. different elements referring to two regions of text which partially or totally overlap. This is not allowed for a number of theorical and practical reasons, so the importer merges these entries. When data are exported back into TEI, the original entries will appear merged.
