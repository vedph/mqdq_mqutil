# Export

## Overview

Given the nature of the documents and the requirements of legacy software, the export procedure in this case is meant to "edit" the existing TEI documents, rather than recreating them from scratch. Recreating the whole documents from scratch would be the way to go for a true export, where the data source is directly the Cadmus database. Here instead we are dealing with a set of legacy TEI documents where every detail about their structure, IDs, and metadata must be preserved, and where we are interested in editing only a subset of the data (those which get [imported](import.md)). This subset is first imported into the database; gets edited in it; and then it exported, i.e. injected back into the original TEI documents.

This makes both the import and export processes a corner case, which coupled with the short deadlines for the editing system to be operational suggests an ad-hoc solution for MQDQ. Yet, this will provide an initial experience in dealing with the remodeling of TEI data into the Cadmus architecture, and vice-versa, which will be useful beyond its specific purpose.

## (A) Exporting Text

The plan for exporting text is described here.

Each set of items belonging to the same work must be injected back into the corresponding TEI text file. This means that we must first collect all the distinct items group IDs from the database, which gives us the list of the works; and then process items grouped by each of these group IDs, in the order defined by their sort key (which reflects their original order).

So, our procedure would essentially be:

for each group ID in the list {

- open the _targetDocument_;
- assume that we must output `w` elements in the text when either _targetDocument_ has at least 1 `w`, or any of the corresponding items has a non-empty layer part; this should output `w` only for those documents which originally had it, and additionally for those documents which did not have them, but are now required to have it because layers were added. Note that when importing, all the texts were treated as if they had `w` elements, because this allows adding layers when needed;
- for each `div1`/`div2` in _targetDocument_, remove all its children except for the eventual `head` child at its head; this is because we're going to replace the content with `p` and `l` elements from our items;
- for each _item_ having the current group ID, sorted by sortKey {

  - locate the corresponding `div` element from its `@xml:id` (assuming that every `div`, either `div1` or `div2`, had a document-wide unique ID; this id has been imported in the item's title), and log an error if not found (this should not happen);
  - get the item's text part; if not found, log an error and continue with the next item (this should not happen);
  - build and append to the target `div` element the `l`/`p` elements from its rows and tiles, with their attributes which have been preserved in each tile metadata; the content of these elements is just the joined text of all the row's tiles when we must not output `w` elements; else it's a set of `w` elements, one for each tile in the row;

  } end item for

} end group for

---

## (B) Exporting Apparatus

### B.1. Procedure

The procedure is similar to exporting text.

for each group ID in the list {

- open the _targetDocument_;
- for each `div1` in _targetDocument_, remove all its children except for the eventual `head` child at its head; this is because we're going to replace the content with `app` elements from our items;
- for each _item_ having the current group ID, sorted by sortKey {

  - get the item's parts; if it has no apparatus parts, just skip to the next item;
  - locate the corresponding `div1` element from its `@xml:id` (as got from the item's title), and log an error if not found (this should not happen);
  - build and append to the target `div1` element the `app` elements from the apparatus parts;

  } end item for

} end group for

Building `app` elements implies a number of details, which are listed in the following sections.

### B.2. Multiple Apparatuses

We may have from **1 to 3 apparatus layer parts**, according to their role: default (null role), `ancient`, `margin`.

As for TEI, there is no specific order to follow when producing sequences of `app` elements. We have two options:

1. *just order the elements in a logical way*, i.e. first all the default entries, then all the ancient entries, and finally all the margin entries. This would probably produce an ordering which is different from the original one, making diffing more difficult.

2. alternatively, *if* the original `app` elements do have a systematic ordering, we can try to *reproduce the original sort order* by collecting all the fragments from each layer and sorting them all together; but I'm not sure about the sort order definition. From what I can see, it seems that ordering follows word IDs, so e.g. `#d003w18` appears before `#d003w29`; but there are a number of details to be defined here, e.g.:

- I assume I have to take into account `@from` only.
- I assume I have to segment the IDs for sorting them. If `d003w2` comes before `d003w11`, this means splitting the ID into a string and a number, and sorting them into two different stages. Yet, can I assume that the ID structure is always like alphanumeric prefix + `w` + digit(s) (i.e. matching a regular expression like `^(?<p>.+)w(?<n>\d+)$`, where the named group `p` is the prefix and the named group `n` is the number)?

In any case, diffing issues will appear whatever option we choose, because in a number of cases we have merged the overlapping entries, and this will of course produce a different XML code at least for those cases.

### B.3. Sparse Fragments

When the selection of text is not contiguous (what in TEI is encoded with a `@loc` rather than with a `@from`-`@to` pair), we have sparse fragments grouped under a same group ID. The importer performs this splitting automatically: when it finds a `@loc`, it makes a copy of the fragment for each of its target locations, grouping all these fragments under the same group ID.

Once in the editor, operators can edit the fragments at will; so we cannot know what an operator may have done to each of the fragments belonging to the same group. He may have edited them to more specifically assign each portion of data to a more precise selection of the text, thus refactoring the whole group, or even making the group itself no more useful, as far as now each entry is refactored to refer to its specific text portion; or he may have deleted and merged some of them; or he may just have left them untouched.

When exporting, we must define a specific policy for such sparse fragments. Here we have two options, and the choice between them depends on a) legacy software requirements and b) the desired TEI schema:

1. keep the sparse fragments as separated entries, just as they are in the database. This anyway once in TEI would lose their connection, unless we are sure that operators have dealt with each sparse fragment by refactoring it.

2. try to regroup the sparse fragments entries into a unique `app` entry, merging their locations into a single `@loc` attribute.

Option #1 is the simplest, and does not pose technical issues. We just output the entries as they are, ignoring the fact that they once belonged to a same unit.

In option #2, a first issue is posed by the fact that the database model here is potentially more granular than the XML model, as in a fragment we can assign a group ID to each single entry, whereas here we want to group several `app` elements into a single one, which implies that all the entries in each `app` element must belong to the same group ID. It could not be the case that in the same `app` there is an entry belonging to a group and another not belonging to it, because in this scenario we could not merge the whole `app` including them.

So, if we are going to regroup, we should assume that if any of the entries in the fragment belongs to a group, the *whole fragment* belongs to that group. Otherwise, trying to dissect its contents into distinct `app` elements might produce incorrect and/or confusing results. Thus, operators working with groups should be aware of this limitation, which is artificial for the Cadmus model, but required for compatibility with the desired output.

With these assumptions, we define a merge procedure which takes into account different scenarios:

- *scenario 1*: *all the entries in all the sparse fragments are identical*. This is the situation after the import procedure, where the original `app` entry has been split into a set of grouped sparse fragments. In this case, we can simply collect the locations from all the fragments and just output one of them with these locations. All the other fragments, having identical entries, will just be dropped.

- *scenario 2*: scenario 1 is not true. This means that some editing has occurred. We can safely assume that this was *not* a complete refactoring, which separated the fragments so that the group is no more useful; because in this case the operator should have removed the connection among the fragments, by deleting their group ID. So, we can merge the fragments into one by *collecting* all their locations (into `@loc`) and *concatenating* all their entries in the order they are defined in each of the fragments, sorted by their location (in ascending order).

### B.4. Converting Coordinates

For both `@from`/`@to` and `@loc`, all the fragments coordinates must be converted back into word IDs. To this end, we must get the item's tiled text part (there must be one, otherwise we log an error), and retrieve from it the IDs of the words at the start and end of the range.

For instance, this is an apparatus fragment with 2 entries:

```json
{
  "location": "3.1",
  "tag": "d001",
  "entries": [
    {
      "type": 0,
      "tag": null,
      "value": "siluestrem",
      "normValue": null,
      "isAccepted": true,
      "groupId": null,
      "witnesses": [
        {
          "value": "lw1-16",
          "note": null
        },
        {
          "value": "lw1-21",
          "note": null
        }
      ],
      "authors": [],
      "note": null
    },
    {
      "type": 0,
      "tag": null,
      "value": "agrestem",
      "normValue": "AGRESTEM#d001w9",
      "isAccepted": false,
      "groupId": null,
      "witnesses": [],
      "authors": [
        {
          "value": "lb1-50",
          "note": "`` 9, 4, 85,"
        },
        {
          "value": "lb1-25",
          "note": "`` SI 244"
        }
      ],
      "note": null
    }
  ]
},
```

The corresponding text part contains (among others) this word (at row 3, tile 1, as specified by the above fragment's `location`):

```json
{
  "y": 3,
  "tiles": [
    {
      "x": 1,
      "data": {
        "id": "d001w9",
        "text": "siluestrem"
      }
    }
  ]
}
```

Now, this word's ID is `d001w9`. This was imported with the word itself inside the tile, and will now be used in the XML output.

### B.5. Merging Notes

The note is a semantically unique field, but it must be split into 4 elements. To this end, it has been imported as divided into 4 optional sections, using the backtick as the separator character:

1. `add` `@type`=`abstract`
2. `note` `@type`=`operation`
3. `note` `@type`=`details`
4. `add` `@type`=`intertext`

Where there is no backtick, it's implied that we just have section 1.

There are 2 types of notes:

- *general* notes. These are direct children of the `app` element.
- notes to *witnesses* or *authors*. These are children of the element representing the apparatus fragment's entry (e.g. `lem`, `rdg`), and have a `@target` attribute with the ID of their target witness(es) or author(s).

As for formatting:

- underscore(s) for Markdown italic and bold become `emph` elements with a `style` attribute equal to `font-style:italic` or `font-weight:bold`.
- each newline in the note's text is rendered as an empty `lb` element.

### B.6. Building XML from Fragment

Once defined the general points listed above, we can provide more detailed directions for the export process of a single fragment.

A *fragment* (corresponding to the TEI `app` element) is a collection of entries, so we can define its export in terms of the export of a single *entry*, repeated any number of times.

I repeat here the entry's model:

```txt
- type
- tag
- groupId
- value
- normValue
- isAccepted
- note
- witnesses (each with value and optional note)
- authors (each with value and optional note)
```

- entries have a **type**, and their output differs according to it. The only types used in the TEI documents are `replacement` and `note`.
  - *replacement*: if the replacement is accepted, it's `lem`; else it's `rdg`.
  - *note*: the note is processed into 1 or more `add` or `note` elements, according to what specified above.

- **value**: the value (if any) is the text value of the `lem`/`rdg` element.
- **normValue**: the normalized value(s) (if any) are put in 1 or more `ident` elements, children of `lem`/`rdg`; there can be 1 or more values, separated by space. When a value ends with `#` followed by an ID, this ID becomes the `@n` attribute of `ident`. For instance, for this entry:

```json
{
  "type": 0,
  "tag": null,
  "value": "agrestem",
  "normValue": "AGRESTEM#d001w9",
  "isAccepted": false,
  "groupId": null,
  "witnesses": [],
  "authors": [
    {
      "value": "lb1-50",
      "note": "`` 9, 4, 85,"
    },
    {
      "value": "lb1-25",
      "note": "`` SI 244"
    }
  ],
  "note": null
}
```

the normalized value is:

```xml
<ident n="d001w9">AGRESTEM</ident>
```

- **witnesses** and **authors**: these are array of items, each with a value and an optional note. Their values are put into `@wit` and `@source`, respectively; the notes are children of the `lem`/`rdg` element, with a `@target` attribute equal to `#` + the value. For instance, an entry like the *agrestem* variant shown above becomes:

```xml
<rdg source="#lb1-50 #lb1-25">agrestem
    <note type="details" target="#lb1-50"> 9, 4, 85,</note>
    <note type="details" target="#lb1-25"> SI 244</note>
</rdg>
```

- **tag**: composed by the original `app` element's `div1` ID + space + the value of its optional `@type`. The latter is put in the `@type` attribute of the `app` element. It is copied unchanged when found, and has no real usage in Cadmus except for preserving the attribute value.
