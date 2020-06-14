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

- `TEI/text/body/div1` is the multiple root for the apparatus content. The ID of each `div1` is stored as the tag of each fragment. If the fragment has a `@type`, it is appended to this tag separated by a space.
- each `div1` contains:
  - `@xml:id` always
  - `@type` always
  - 1 `head` child with header data.
  - 1-N `app` children.

The `app` elements are rebuilt by the export process and reinjected under each `div1`, past their initial `head` child (which is kept unchanged), in place of all the old `app` elements.

### Note on Overlapping app Elements

A general point should be made about some cases of overlapping `app` elements. This requires me to spend some words about the Cadmus architecture, before facing MQDQ-specific issues and their resolution.

#### No Overlapping Fragments

In the Cadmus architecture, *items* are empty boxes including an open set of models (*parts*). In MQDQ we want to represent literary texts; so each item corresponds to a text piece (partitioned in a way which takes into adequate account semantic boundaries), as of course we could never think of editing a full, lengthy document at once in a web-based, concurrent editing environment.

The parts are the models assigned to each item. The text itself is just a part inside an item. We could have as many part types as we can imagine, to represent e.g. datation, categories, keywords, comments, paleographic descriptions, prosopographic data, etc. There is no limit, and each part is designed and implemented independently from all the others.

Textual metadata like apparatus entries are just parts, too. They belong to a family of parts named *layer parts*, which represent layers overlaid on a base text. Each layer contains one and one only model type: thus, we have the apparatus layer, the comments layer, the standard orthography layer, the abbreviations layer, etc.

Every layer part is a collection of specialized models (*fragments*), each linked to a specific portion of the base text through a coordinates system. The link is thus external to the text, which means that we can add as many layers and fragments inside them, without ever having to touch the base text.

Each part (and each fragment inside layer parts) has its own model; whatever type of data we want to add, we just add new parts to items. Thus, we have a dynamic data model, where the "type" of an item is just the sum of all its parts.

Ultimately, the database is a set of independent, linked models; in the editor UI, each of these models has its own UI. *As the database aggregates different models, so the UI aggregates different interfaces*. Thus, the composable UI goes hand in hand with the composable data model (and their implementation as composable software components). This is why the architecture has been carefully designed to balance the requirements of both backend and frontend layers.

Now, both usability reasons connected to the UI and theorical principles underlying the Cadmus architecture concur to define a specific and intentional limitation for fragments: *in the same layer, there can be no fragments linked to overlapping text portions*.

For instance, in the *same* comment layer I could not add a comment fragment for the words "Sicelides Musae", and another, distinct yet overlapping fragment for the "Sicelides" word only, because:

- the *user experience* in editing layers is as much friendly as possible: the user freely selects the portion of text he wants to connect to a layer fragment, and then edits it. All the portions of text already connected to a fragment are highlighted with some color. Thus, we have all the layer's metadata displayed at a glance; and editing them is as easy as selecting and clicking a button. Of course, this implies that there is no overlap between fragments in the same layer; otherwise, no such visualization and editing experience could be possible.

- *conceptually*, we might want to comment "Sicelides Musae" as a whole, e.g. to explain the literary tradition (Theocritus etc.) the poet is referring to with this phrase; or we might just want to comment "Sicelides" because of its Greek-like form. Now, this would create an overlap, between a fragment spanning from "Sicelides" to "Musae", and another one referred to "Sicelides" only. Granted, we could just add a single comment fragment, annotating both these data in it. But the very fact that we find ourselves in an overlap scenario is a strong clue that we are really *confusing two conceptually distinct domains*. In fact, here we are dealing with two different types of comments: one is a *literary* annotation, another is a *linguistic* one. We could thus better represent these different domains using *two different layers*, with the same model (the comment model): a literary comments layer and a linguistic comments layer. Or we could go even further, and provide additional, more specialized data in the linguistic annotation, thus requiring a different model at all. In this case, we would still have two different layers, but also belonging to two different models (a comments layer and a linguistic layer). Whatever our solution, this is a better representation of the different conceptual domains involved, and also removes the potential fragments overlap.

This is one of the reasons for Cadmus having the possibility of stacking as many layers as we want on a text, even with the same type (model). In this case, each layer of the same type differs from the others for its specific *role*: we might have a literary comment and a linguistic comment; an original-text datation and a text-copy datation in an inscription; etc. There is no limit to the layers, even of the same type. As we can see from these examples, this is a purely conceptual requirement; but it also nicely fits with the user-experience requirements of keeping UI simple by avoiding fragments overlaps.

#### Overlapping app's

Now, in XML documents we have a number of cases where `app` elements, which in theory should convey all the data connected to their text portion, are really overlapping with other `app` elements. Thus, we have several `app` elements referring to the same portion of text, or a subset of it.

Fortunately, most of these cases are the natural effect of the above mentioned confusion of conceptual domains, which cannot be avoided in XML (where all data are laid on the unique, DOM-related structure). In fact, they belong to two cases:

- whole apparatus entries marked as `margin-note`.
- lemma/variants of an apparatus entry marked as `ancient-note`. Note that there can be cases of `ancient-note` entries inside `margin-note` `app`'s.

The comment-like nature of such cases often let their `app` element overlap with other `app` elements, representing only apparatus variants in a stricter sense.

The solution here, both for separating their different conceptual domains and avoiding overlaps, is *moving margin notes into separate layers*, with the same model of the apparatus layer. Thus, we will have up to 2 layers for the text (all with the same model):

- apparatus layer with variants.
- apparatus layer for margin notes.

This removes most of the overlap cases.

For instance, these two `app` elements overlap by the word at `d001w9`:

```xml
<app from="#d001w9" to="#d001w13" type="margin-note">
    <lem source="#lb1-56" type="ancient-note">
        <add type="abstract"><emph style="font-style:italic">Meditaris</emph> cantas, uel <emph style="font-style:italic">melitaris</emph>, -<emph style="font-style:italic">l</emph>- pro -<emph style="font-style:italic">d</emph>-, ut idem sit tropus.</add>
    </lem>
</app>
<app from="#d001w9" to="#d001w9">
    <lem wit="#lw1-16 #lw1-21">siluestrem</lem>
    <rdg source="#lb1-50 #lb1-25">agrestem
        <note type="details" target="#lb1-50"> 9, 4, 85,</note>
        <note type="details" target="#lb1-25"> SI 244</note>
        <ident n="d001w9">AGRESTEM</ident>
    </rdg>
    <rdg source="#lb1-56" type="ancient-note">
        <add type="abstract"><emph style="font-style:italic">silvestrem</emph>, agrestem<emph style="font-style:italic">.</emph></add>
    </rdg>
</app>
```

Anyway, here the first `app` element belongs to margin notes; thus it will be moved to a different layer, removing the overlap.

Apart from these cases, there remains a number of overlapping `app` elements with different reasons, like:

```xml
<app from="#d005w257" to="#d005w258">
    <lem wit="#lw1-16">fontibus umbras</lem>
    <rdg wit="#lw1-20">frondibus aras
        <note type="details" target="#lw1-20"> <emph style="font-style:italic">ap</emph>. <emph style="font-style:italic">Pierium</emph></note>
        <ident n="d005w257">FRONDIBVS</ident>
        <ident n="d005w258">ARAS</ident>
    </rdg>
</app>
<app from="#d005w258" to="#d005w258">
    <lem wit="#lw1-16">umbras</lem>
    <rdg wit="#lw1-21 #lw1-1">aras
        <ident n="d005w258">ARAS</ident>
    </rdg>
</app>
```

Here we have two `app` elements referred to the same word "umbras" `d005w258`; in the first element, "fontibus umbras" has the variant "frondibus aras"; in the second element, "umbras" has the variant "aras". These could easily be merged into a single `app` element with the proper entries, thus removing the overlap.

In these cases the fix is applied by a specially designed procedure, which is executed at the beginning of the whole flow to adjust the XML apparatus documents. There are 2 commands in the CLI tool for this purpose:

- `report-overlaps` builds a full Markdown report of all the overlap cases.
- `remove-overlaps` removes the overlaps from the apparatus XML documents, saving the results into another folder. This removal happens by "cutting" all the children elements of the overlapped entry (except its `lem`), adding to each of them an `@n` attribute with their parent `app` original `@from`/`@to` values, and pasting them at the end of the content of the target `app` element. If the cut `lem` element had any `@wit` or `@source` attribute whose content is not a subset of the target `app`'s `lem`, an error is logged because we are going to lose some information which users should restore manually later in the editor.

For instance, given this overlap:

```xml
<app from="#d003w158" to="#d003w158">
    <lem wit="#lw2-8 #lw2-20 #lw2-33">et</lem>
    <rdg source="#lb2-51">
        <note type="operation" target="#lb2-51">om.</note>
        <note type="details" target="#lb2-51"> 319, 7</note>
    </rdg>
</app>
<app from="#d003w158" to="#d003w160">
    <lem wit="#lw2-8 #lw2-20 #lw2-33">et dictu uideo
        <note type="details" target="#lw2-8"> <emph style="font-style:italic">p.c.</emph></note>
    </lem>
    <rdg source="#lb2-47">dictu et visu
        <note type="details" target="#lb2-47"> 3, 10, 6</note>
        <ident n="d003w158">DICTV</ident>
        <ident n="d003w159">ET</ident>
        <ident n="d003w160">VISV</ident>
    </rdg>
</app>
```

we merge the first overlap (whose extent is less than the other one) into the second:

```xml
<app from="#d003w158" to="#d003w160">
    <lem wit="#lw2-8 #lw2-20 #lw2-33">et dictu uideo
        <note type="details" target="#lw2-8"> <emph style="font-style:italic">p.c.</emph></note>
    </lem>
    <rdg source="#lb2-47">dictu et visu
        <note type="details" target="#lb2-47"> 3, 10, 6</note>
        <ident n="d003w158">DICTV</ident>
        <ident n="d003w159">ET</ident>
        <ident n="d003w160">VISV</ident>
    </rdg>
    <!-- pasted from overlapping entry -->
    <rdg source="#lb2-51" n="d003w158 d003w158">
        <note type="operation" target="#lb2-51">om.</note>
        <note type="details" target="#lb2-51"> 319, 7</note>
    </rdg>
</app>
```

### Header

The header must be processed to import thesauri for witnesses and authors. This is an import-only process, as no changes will be made to the header data; thus, nothing will be exported. It is just a way of providing end users with human-readable lookup data (e.g. witnesses and authors names) while editing text and apparatus.

From the root `TEI/teiHeader/fileDesc/sourceDesc`, we parse these elements:

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

Ideally, the note text including the variant is split in MQDQ into these parts:

1. `add @type=abstract`: section 1 (before variant).
2. `note @type=operation`: section 2 (before variant).
3. `note @type=details`: section 3 (after variant).
4. `add @type=intertext`: section 4 (after variant).

The target model `note` is a unique string where a divider character (backtick) is used to end each section. For the sake of readability, here I use `|` to represent this divider character.

Thus, `one || two | three` means that section 1 = `one`, section 2 is not present, section 3 = `two`, section 4 = `three`. When there is no such separator, this means that only section 1 is present.

#### Elements lem or rdg

These elements are variant readings; they are formally equal, the only difference being that `lem` is the chosen reading.

Optionally with `@type` (in `lem` the only value seems `ancient-note`), `@source` (authors; multiple tokens separated by space), `@wit` (witnesses; multiple tokens separated by space), they contain _text mixed_ with `ident` (normalized form), `add` (note section 1 or 4), `note` (note section 2 or 3).

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

The thesaurus contains any number of object properties with `id`=element's `@xml:id`, and `value`=text value of the element, eventually reduced. If the element has `@ref`, prepend `@n` to its value, which might even be empty, like:

```xml
<bibl xml:id="lb1-38" ref="bibl:b1043" n="Petron."></bibl>
<bibl xml:id="lb1-43" ref="bibl:b1092" n="Prob. cath. gramm.">
    , pp. 3-43.
</bibl>
```

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

This section discusses the mapping between the above apparatus model and its MQDQ representation in terms of the XML DOM.

#### Mapping app

Each `app` element corresponds to a **fragment**; `app` with `@loc` will be duplicated for each additional location.

Attributes:

- `app/@from @to`: `location`. The location is recalculated according to the Cadmus coordinates system and the corresponding base text. The original location for export will be retrieved from the metadata of the base text tiles.
- `app/@loc`: `location`. In this case the value of the `loc` attribute contains 2 or more IDs representing a non-continuous range. This is modeled into Cadmus as distinct entries, all belonging to the same group. Thus, in this case we will map a fragment for each single location in loc.
- `app/@type`: copied in `tag` as it is. This way we will be able to export it back.

Content:

- `lem`, `rdg`
- `note`

#### Mapping lem or rdg

Elements `lem` or `rdg` are each mapped to an **apparatus entry**, accepted for `lem`.

TODO: determine entry type: hypothesis: it's a note if has no (non-ws) text child node; else replacement.

Attributes:

- `@type`: copy into `tag` as it is.
- `@source`: split value at space and store in `authors`, removing the `#` prefix.
- `@wit`: split at space and store in `witnesses`, removing the `#` prefix.

Content:

- the first _child text node_ is the entry's `value`. If no such node, it's a note entry. There should be no more than 0 or 1 such node text children, excluding whitespace-only or empty nodes. TODO: confirm.
- `ident`, `add`, `note`.

#### Mapping ident

Append to `normValue` (using space as values delimiter) with its `@n` prefixed with `#`. This accounts for several `ident` elements under the same parent element.

#### Mapping add

Map to note section 1 (`@type`=`abstract`) or 4 (`@type`=`intertext`). Log error if the target section already exists.

#### Mapping note

Map to note section 2 (`@type`=`operation`) or 3 (`@type`=`intertext`). Log error if the target section already exists.

#### Mapping app/note

A note as the direct child of `app` is an **apparatus note entry**.

Attributes:

- optional `@type` in `tag`.

Content:

- `add`: as above for `app/lem/add` or `app/rdg/add`.
- `ident`: as above for `app/lem/ident` or `app/rdg/ident`.

## Import Specs Summary

This summarizes the above discussion.

- `app` -> *fragment*:
  - attributes: `@from`+`@to` or `@loc`; in the latter case, clone the fragment for each location and log it.
  - content: `lem`, `rdg`, `note`.

- `lem` or `rdg` -> apparatus *entry*:
  - `@type` -> *tag*.
  - `@source` -> *authors* (multiple).
  - `@wit` -> *witnesses* (multiple).
  - text -> *value*.
- `note` -> apparatus *entry* (note):
  - `@type` -> *tag*.

Descendant elements of `lem`, `rdg`, or `note`; I mark with ET the elements whose content is text mixed with `emph` and/or `lb`:

- `ident` -> *normValue* (multiple, each with `@n`).
- `add` (ET) -> *note* section 1 (`@type`=`abstract`) or 4 (`@type`=`intertext`).
- `note` (ET) -> *note* section 2 (`@type`=`operation`) or 3 (`@type`=`details`); if `@target` is present, it refers the note to the corresponding author/witness.

ET-content is processed so that all the children elements `emph` or `lb` become Markdown escapes. This way, after this processing the parent element should contain only text.

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

Note values are trimmed and prepended by the required number of separator characters to represent their section. If there is only a single section, the first one, there is no seperator character, like in the ancient note above.

### A Remark on Sample Models

As for modeling, a general point should be stressed here: from an abstract point of view, one could argue that the models represented by XML and JSON are logically similar; maybe JSON is less verbose, but both represent the same data.

Yet, there are a number of substantial differences:

- **context**: the Cadmus model is designed, stored and edited independently, in its own _part_. Its model does not affect that of the text it refers to; nor is affected by it. In XML instead, this fragment must become part of a much larger, yet unique DOM-shaped structure, where each element gets entangled with all the others, whatever their conceptual domain or practical purpose.

- **content creation**: as a practical consequence, Cadmus models can be stored in a database, remotely and concurrently edited in a distributed architecture, validated and indexed in real-time, etc. XML documents instead are usually stored in a file system, and individually and locally edited.

- **lax semantics**: in the XML documents, a number of elements and attributes are used with a specialized meaning, which is specific to that project. Think for instance of the different types of notes (`add` and `note`) and their attributes, which compose a multiple-section annotation with a very specific meaning. The TEI model allows such lax semantics, as it must fit any document type: TEI users do interpret the standard and adapt it to their own purposes. Yet, in this way a full understanding of the markup semantics falls outside the capabilities of the model itself, which is no more self-documented. Effectively, we need to ask the documents creators about how they used certain markup, and which meaning it conveys. In Cadmus parts, the model is much more formalized and constrained, as far as each part is targeted to one and only one conceptual domain, and is designed in a totally independent way.

- **highly undetermined XML structures**: the Cadmus model is totally _predictable_. It may well be highly nested, and include optional and/or required properties of any specific type; but its model is well defined, just as an object class in a programming language. The XML tree instead is highly variable, right because of the very lax model designed to represent any possible detail of a historical document, merging a lot of different structures, all overlaid on the same text. The above XML sample turned into a Cadmus model is only one of the potentially innumerable shapes the XML tree might take. In fact, unless we have constrained our document model into a highly disciplined subset of TEI, we cannot be sure about the content of each XML element, i.e. which attributes and elements can exactly be found in each element; and whether this is true in any context where they might occur. For instance, in MQDQ apparatuses a `note` could include just text, or a text mixed with elements like `emph` or `lb`; in turn, `emph` might include another `emph`. Even in MQDQ, the schema allows for cases of potentially infinite recursion; and the only way of knowing which structures are effectively found in our documents is scanning all of them. Yet, all these documents are TEI-compliant, and conform to that model; but this is not enough to allow us to know in advance which structures might happen to be found. We can never be sure, unless we scan all the documents; and this is fragile, as any newly added document might change our empirically deduced model. This seriously hinders the development of software solutions, which for XML documents must often be tailored and thus reinvented for each specific project.
