# Apparatus

Data from scan:

- [XML tree](mqdq-app-report.html)
- [characters counts](mqdq-app-chars.tsv)

## Apparatus Fragment Model

The apparatus model is designed to meet the requirements of MQDQ and at the same time be general enough to apply to other projects as well.

The apparatus fragment, i.e. an entry in the apparatus metatextual layer, is modeled as follows:

- `location`: the fragment location.
- `tag`: an optional arbitrary string representing a categorization of some sort for that fragment, e.g. "margin", "interlinear", etc. This can be overridden by variants `tag`.

- `entries`: 1 or more variants/notes, each with these properties:

  - `type`: an enumerated constant to be chosen among replacement (0), addition before (1), addition after (2), note (3).
  - `value`: the variant's value. May be zero (empty or null) for deletions. Is optional (because not used) when `type` is note.
  - `tag`: an optional arbitrary string representing a categorization of some sort for that fragment, e.g. "margin", "interlinear", etc. It overrides the fragment's `tag`.
  - `normValue`: an optional normalized form derived from `value`. Normalization details are up to each single project, and the model makes no assumptions about it.
  - `note`: an optional annotation. When `type` is _note_, `value` has no meaning, and this property contains the note's text. Otherwise, this can be an additional note, side to side with the variant's value.
  - `authors`: optional array of annotated authors. Each has a `value` and an optional `note`.
  - `witnesses`: optional array of annotated witnesses, as above.
  - `isAccepted`: boolean, true if the variant represents the accepted text (=lemma).
  - `groupId`: an optional arbitrary ID to be used for grouping fragments in the layer together.

If required, notes can be Markdown to include some minimal formatting. This should anyway be limited to very basic formatting, e.g. italic and bold.

## XML

### Overview

The XML documents tree is as follows:

- `TEI/text/body/div1` is the root for the apparatus content.
- `div1` contains:
  - `@xml:id` always
  - `@type` always
  - 1 `head` child with header data.
  - 1-N `app` children.

The `app` elements are rebuilt by the export process and reinjected past `div1/head`, in place of all the old `app` elements.

### Header

The header must be processed to import thesauri for witnesses and authors. This is an import-only process, as no changes will be made to the header data; thus, nothing will be exported. It is just a way of providing human-readable lookup data (e.g. witnesses and authors names) to end users while editing text and apparatus.

From the root `TEI/teiHeader/fileDesc/sourceDesc`, we parse these elements (TODO: check if double children are ok):

- `listBibl/listBibl`: authors.
- `listWit/listWit`: witnesses.

The deepest of the two `listBibl` contains `bibl` children, each with its ID in `@xml:id` and _text mixed_ with `emph` (eventually nesting another `emph`, like in body). We are interested only in importing the ID and a descriptive text for it, so this is all what we need.

The deepest of the two `listWit` contains `witness` children, modeled just like `bibl`.

### Body

#### Element app

Attributes: any app element always has either `@from` and `@to` (always in pair) or `@loc`; `@type` instead is optional, and used in less than 10% of cases:

- `@from` and `@to` define a point A (when their value is equal) or a continuous range from A to B.
- `@loc` contains multiple word IDs separated by space.
- `@type`: when present, its only value is `margin-note`.

Children elements:

- `lem` and `rdg`
- `note`: this element as a direct child of `app` has a different schema from other, deeper `note` elements.

#### Elements lem/rdg

These elements are variant readings; they are formally equal, the only difference being that `lem` is the chosen reading.

Optionally with `@type` (in `lem` the only value seems `ancient-note` TODO: confirm), `@source` (authors; multiple tokens separated by space), `@wit` (witnesses; multiple tokens separated by space), they contain _text mixed_ with `ident` (normalized form), `add` (note section 1 or 4), `note` (note section 2 or 3).

#### Element add

Element `add` always with `@type`, contains _text mixed_ with `emph` and `lb`.

When `@type` is `abstract` it refers to note section 1; when it is `intertext` it refers to note section 4.

#### Element note - 1 (Child of app)

Element `note` has 2 different schemas according to its location, i.e. whether it is a direct child of `app` or not.

When direct child of `app`, the element does _not_ contain text nodes; it may have `@type` (14 cases, always with value `ancient-note`); and it can contain these children:

- `add`
- `ident`: only 1 case, representing a range two words which are absent from most witnesses:

```xml
<app from="#d049w62" to="#d049w63" xmlns="http://www.tei-c.org/ns/1.0">
  <lem wit="#lw2-8" source="#lb2-11">immobilis haeret</lem>
  <note>
      <add type="intertext"><emph style="font-style:italic">haec verba non habent plurimi codices</emph></add>
      <ident n="d049w62 d049w63"></ident>
  </note>
</app>
```

#### Element note - 2 (Not Child of app)

Element `note` always has `@target`, may have `@type`, and contains _text mixed_ with `emph` and `lb`.

When `@type` is `operation` it refers to note section 2; when it is `details`, it refers to note section 3.

The `@target` attribute contains a witness/author ID when the note refers to that witness/author.

#### Element emph

Element `emph` always has `@style`, and may contain _text_, eventually _mixed_ with other `emph` (recursively; less than 400 cases) and `lb`. The styles are:

- `font-style:italic`
- `font-weight:bold`
- `vertical-align:super;font-size:smaller`: this is used for various superscript characters: digits, space, lowercase letters, dashes, etc.
- `vertical-align:sub;font-size:smaller`: this is used for various subscript characters.

The super/sub `emph` can nest italic/bold inside them.

#### Element ident

Element `ident` always with `@n`, represents a normalized form and contains _only text_.

The `@n` attribute contains the ID of the word the normalized form is derived from, and is not predictable (see below).

#### Element lb

Element `lb` is empty.

## Storing Word ID with the Normalized Form

As for the `ident` element, consider a case like `VERG-eclo-app.xml` line 432:

```xml
<app from="#d001w379" to="#d001w381">
    <lem wit="#lw1-16 #lw1-21">pueri; summittite</lem>
    <rdg wit="#lw1-29" source="#lb1-36">pueri et summittite
        <note type="details" target="#lb1-36"> 390,7 (= IVM II.1, p. 34)</note>
        <ident n="d001w379">PVERI</ident>
        <ident n="d001w379">ET</ident>
        <ident n="d001w381">SVMMITTITE</ident>
    </rdg>
</app>
```

The corresponding text line is:

```xml
<l xml:id="d001l53" n="45">
    <w xml:id="d001w374">"Pascite</w>
    <w xml:id="d001w375">ut</w>
    <w xml:id="d001w376">ante</w>
    <w xml:id="d001w377">boues,</w>
    <w xml:id="d001w379">pueri;</w>
    <w xml:id="d001w381">summittite</w>
    <w xml:id="d001w382">tauros."</w>
</l>
```

Here, the text reads `pueri; summittite` (as in `lem`), but other witnesses insert `et` between these words (as in `rdg`). In XML we need to wrap some text to mark it; but here there is no text before the insertion: we replace a zero-text with `et`. Thus, we must wrap the words surrouding the insertion point, and provide a multi-word variant which includes both them (`pueri` and `summittite`) and the word inserted in between (`et`).

As this variant contains two words, plus a third one inserted between them, there are 3 normalized forms, each with its own ID. The inserted word has no ID to refer to, as it is replacing a zero text; so, here it refers to the word preceding it (`pueri`). This is why both `PVERI` et `SVMMITTITE` have the same word ID.

Similar scenarios force us to keep the word ID, as it can be assigned in an unpredictable way. Yet, the ID is required only for legacy compatibility, so we would not want to "pollute" the general apparatus model with specific requirements.

A possible hack could be storing the word ID with the normalized form; for instance, the normalized form `PVERI` and word ID `d001w379` might be stored as `PVERI#d001w379`. Given that the normalized form value is up to the specs of each project, this can be an acceptable hack, and saves the general model.

## Mapping to Model

### Header Mapping

Each XML app document provides its own list of authors and witnesses, which must be appended to a single JSON file containing all of them as thesauri. This will then be copied into the Cadmus profile file for the MQDQ database.

Thus, for each `bibl` we output a thesaurus with `id`=`apparatus-authors` + `.` + filename (without `-app` and extension) + `@en`, all lowercased.

The thesaurus contains any number of object properties with `id`=element's `@xml:id`, and `value`=text value of the element, eventually reduced.

For `witness`es it is the same, except that the ID prefix is `apparatus-witnesses`.

For instance, this entry from `VERG-eclo-app.xml`:

```xml
<witness xml:id="lw1-3" n="b">
    Bernensis 165, olim Turonensis [<emph style="font-style:italic">MO</emph> B. 10, saec. IX 1/4]
</witness>
```

becomes this entry in its thesaurus with ID `apparatus-witnesses.verg-eclo@en`:

```json
{
  "id": "lw1-3",
  "value": "Bernensis 165, olim Turonensis [MO B. 10, saec. IX 1/4]"
}
```

The thesaurus values tend to be somewhat long, which in a UI might hinder usability: e.g. `Excerpta ex Grilli commento in primum Ciceronis librum de inventione (saec.IV-V) = RLM (Rhetores Latini Minores), pp. 596-606 (ed. C. Halm, Lipsiae 1863)`. In a dropdown list, we could probably just read the first half of this text, otherwise the control width would grow too large. We might want to apply some summarizing algorithm to it.

The best reduction strategy depends on the typical patterns used in the citations, and should be targeted to focus on what is most important for the users to be recognized at a glance. For instance, currently we apply this strategy:

- cut at the best location at about 30 characters from the start;
- if the value ends with `)` or `]`, append the parenthesized text, cutting it too if it exceeds 30 characters.

For instance, this results in `Excerpta ex Grilli commento... (ed. C. Halm, Lipsiae 1863)` from the above sample text.

### Body Mapping

TODO: refactor once analysis is complete

This section discusses the mapping between the above apparatus model and its MQDQ representation in terms of the XML DOM.

Each `app` element corresponds to a **fragment**. Its mapping is as follows:

- `app/@from @to`: `location`. The location is recalculated according to the Cadmus coordinates system and the corresponding base text. The original location for export will be retrieved from the metadata of the base text tiles.
- `app/@loc`: `location`. In this case the value of the `loc` attribute contains 2 or more IDs representing a non-continuous range. This is modeled into Cadmus as distinct entries, all belonging to the same group. Thus, in this case we will map a fragment for each single location in loc.
- `app/@type`: copied in `tag` as it is. This way we will be able to export it back.

As for **variants**:

- `app/lem` is the accepted variant, i.e. it is modeled like any other variants, with the only difference that its accepted property is true.
- `app/rdg*`: 0 or more readings, each mapped to a variant.

In both cases, their subtree is mapped as follows (in what follows I represent `lem` or `rdg` with the generic `P`=parent name):

- `app/P/@type`: copy into `tag` as it is. TODO: type=ancient-note
- `app/P/rdg/` text value: the unique child text node of `rdg` is its `value`. I assume there is only 1 such child text node.

- `app/P/rdg/ident/`: its text value is the `normValue`. Usually there is just a 1:1 relationship with the lemma. Yet, in some cases it may happen that a different number of words are involved; for instance, when 2 words correspond to a single word. In this case, these 2 words will have the same ID in their `n` attribute. TODO: details and example

- `app/P/@wit`: witnesses. Split at space and store in `witnesses`, removing the `#` prefix.
- `app/P/@source`: sources. Split at space and store in `authors`, removing the `#` prefix.

- `app/P/add @type=abstract`: `note` section 1.
- `app/P/note @type=operation`: `note` section 2.
- `app/P/note @type=details`: `note` section 3.
- `app/P/add @type=intertext`: `note` section 4.

The target of a note is specified by its `target` attribute, always present.

Here `note` is a unique string where a divider character (can we use `|`??) is used to end each section. Thus, `one || two | three` means that section 1 = `one`, section 2 is not present, section 3 = `two`, section 4 = `three`.

Note values are trimmed, assuming that (??) we must add a whitespace before any after-value, or after any before-value.

Any of the note elements (`add`/`note`) can have mixed content where the only allowed child element is `emph` representing formatting. Its formatting is mapped from `@style` as follows:

- `font-style:italic`: italic. This text is wrapped in `_` (Markdown; I suggest `_` rather than `*` as I suppose the asterisk might happen to occur in its proper value).
- `font-weight:bold`: bold. This text is wrapped in `__` (Markdown, as above).

I assume that (??):

- no other styles are present.
- no other child element is present in `note`.

As for these properties, they are not directly mapped but calculated:

- `type`: TODO: type

## Samples

The following samples are extracted from real documents, and eventually reduced for clarity.

### Vergilius ecl. 1,2

`VERG-eclo` (1,2):

- apparatus with lemma and 2 variants.
- notes to authors.

```xml
<app from="#d001w9" to="#d001w9">
    <!-- lemma -->
    <lem wit="#lw1-16 #lw1-21">siluestrem</lem>
    <!-- variant -->
    <rdg source="#lb1-50 #lb1-25">agrestem
        <note type="details" target="#lb1-50"> 9, 4, 85,</note>
        <note type="details" target="#lb1-25"> SI 244</note>
        <ident n="d001w9">AGRESTEM</ident>
    </rdg>
    <!-- ancient note -->
    <rdg source="#lb1-56" type="ancient-note">
        <add type="abstract"><emph style="font-style:italic">silvestrem</emph>, agrestem<emph style="font-style:italic">.</emph></add>
    </rdg>
</app>
```

Model:

```json
{
  "location": "2.1",
  "entries": [
    {
      "type": 0,
      "isAccepted": true,
      "value": "siluestrem",
      "witnesses": [{ "value": "lw1-16" }, { "value": "lw1-21" }]
    },
    {
      "type": 0,
      "value": "agrestem",
      "normValue": "AGRESTEM#d001w9",
      "authors": [
        { "value": "lb1-50", "note": "``9, 4, 85," },
        { "value": "lb1-50", "note": "``SI 244" }
      ]
    },
    {
      "type": 0,
      "tag": "ancient-note",
      "note": "_silvestrem_, agrestem_._",
      "authors": [{ "value": "lb1-56" }]
    }
  ]
}
```

Note values are trimmed and prepended by the required number of separator characters to represent their section. If there is only a single section, the first one, there is no seperator character, like in the ancient note above. Notice that `emph` is converted to Markdown, but in some cases it is redundant (e.g. when marking the final dot as italic).

### A Remark on Sample Models

As for modeling, a general point should be stressed here: from an abstract point of view, one could argue that the models represented by XML and JSON are logically similar; maybe JSON is less verbose, but both represent the same data.

Yet, there are a number of substantial differences:

- **context**: the Cadmus model is designed, stored and edited independently, in its own _part_. Its model does not affect that of the text it refers to; nor is affected by it. In XML instead, this fragment must become part of a much larger, yet unique DOM-shaped structure, where each element gets entangled with all the others, whatever their conceptual domain or practical purpose.

- **content creation**: as a practical consequence, Cadmus models can be stored in a database, remotely and concurrently edited in a distributed architecture, validated and indexed in real-time, etc. XML documents instead are usually stored in a file system, and individually and locally edited.

- **lax semantics**: in the XML documents, a number of elements and attributes are used with a specialized meaning, which is specific to that project. Think for instance of the different types of notes (`add` and `note`) and their attributes, which compose a multiple-section annotation with a very specific meaning. The TEI model allows such lax semantics, as it must fit any document type: TEI users do interpret the standard and adapt it to their own purposes. Yet, in this way a full understanding of the markup semantics falls outside the capabilities of the model itself, which is no more self-documented. Effectively, we need to ask the documents creators about how they used certain markup, and which meaning it conveys. In Cadmus parts, the model is much more formalized and constrained, as far as each part is targeted to one and only one conceptual domain, and is designed in a totally independent way.

- **highly undetermined XML structures**: the Cadmus model is totally _predictable_. It may well be highly nested, and include optional and/or required properties of any specific type; but its model is well defined, just as an object class in a programming language. The XML tree instead is highly variable, right because of the very lax model designed to represent any possible detail of a historical document, merging a lot of different structures, all overlaid on the same text. The above XML sample turned into a Cadmus model is only one of the potentially innumerable shapes the XML tree might take. In fact, unless we have constrained our document model into a highly disciplined subset of TEI, we cannot be sure about the content of each XML element, i.e. which attributes and elements can exactly be found in each element; and whether this is true in any context where they might occur. For instance, in MQDQ apparatuses a `note` could include just text, or a text mixed with elements like `emph` or `lb`; in turn, `emph` might include another `emph`. Even in MQDQ, the schema allows for cases of potentially infinite recursion; and the only way of knowing which structures are effectively found in our documents is scanning all of them. Yet, all these documents are TEI-compliant, and conform to that model; but this is not enough to allow us to know in advance which structures might happen to be found. We can never be sure, unless we scan all the documents; and this is fragile, as any newly added document might change our empirically deduced model. This seriously hinders the development of software solutions, which for XML documents must often be tailored and thus reinvented for each specific project.
