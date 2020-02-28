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

### Element app

- `@from` and `@to` define a point A (when their value is equal) or a continuous range from A to B.
- `@loc` contains multiple word IDs separated by space.
- `@type`
- `lem` optionally with `@source`, `@type`, `@wit`:
  - `add`
  - `ident`
  - `note`
- `note`
  - `add`
  - `ident`
  - `note`
- `rdg` optionally with `@source`, `@type` (e.g. `ancient-note`), `@wit`, contains text (the variant reading) mixed with:
  - `add`
  - `ident`
  - `note`

In any context:

- `add` always with `@type`, contains text mixed with `emph` and `lb`. When `@type` is `abstract` it refers to note section 1; when it is `intertext` it refers to note section 4.
- `note` always has `@target`, may have `@type`, and contains text mixed with `emph` and `lb`. When `@type` is `operation` it refers to note section 2; when it is `details`, it refers to note section 3. The `@target` attribute contains a witness/author ID when the note refers to that witness/author.
- `emph` always has `@style`, and may contain text or other `emph` (recursively) or `lb`.
- `ident` always with `@n`, represents a normalized form and contains only text. The `@n` attribute contains the ID of the word the normalized form is derived from, and is not predictable (see below).
- `lb` is empty.
- `@source` contains authors ID(s), separated by space.
- `@wit` contains witness(es) ID(s), separated by space.

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

As for modeling, a general point should be stressed here: from an abstract point of view, one could argue that the models represented by XML and JSON are logically similar; maybe JSON is less verbose, but both represent the same data.

Yet, there are at least a couple of substantial differences:

- a first difference is made by their **context**: the Cadmus model is stored and edited independently, in its own *part*. Its model does not affect that of the text it refers to; nor is affected by it. In XML instead, this fragment must become part of a much larger, yet unique DOM-shaped structure, where each element must get entangled with all the others, whatever their conceptual domain or practical purpose.
- the Cadmus model is totally **predictable**. It may well be highly nested, and include optional and/or required properties of any specific type; but its model is well defined, just as an object class in a programming language. The XML tree instead is highly variable, right because of the very lax model designed to represent any possible detail of a historical document, merging a lot of different structures all laid on the same text. In fact, unless we have constrained our document model into a highly disciplined subset of TEI, we cannot be sure about the content of each XML element: for instance, here a `note` could include just text, or a text mixed with elements like `emph` or `lb`; in turn, `emph` might include another `emph`; it even happens that a `note` includes another `note`. In theory, this opens to infinite recursion, and the only way of knowing which structures are effectively found in our documents is scanning all of them. All these documents are TEI-compliant; but this is not enough to allow us to know in advance which structures might happen to be found. We can never be sure, unless we scan all the documents; and this is fragile, as any newly added 
