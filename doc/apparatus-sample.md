# Apparatus Sample

Document `VERG-eclo-app.xml`.

## Element app number 1

```xml
<app from="#d001w9" to="#d001w13" type="margin-note">
    <lem source="#lb1-56" type="ancient-note">
        <add type="abstract"><emph style="font-style:italic">Meditaris</emph> cantas, uel <emph style="font-style:italic">melitaris</emph>, -<emph style="font-style:italic">l</emph>- pro -<emph style="font-style:italic">d</emph>-, ut idem sit tropus.</add>
    </lem>
</app>
```

Here is the corresponding log:

```txt
[INF] Parsing C:\Users\dfusi\Desktop\mqdq\VERG-eclo-app.xml
[INF] ==Parsing div1 #d001 at line 358
[INF] --Parsing app #1@370
[INF] Fragment location: 3.1-3.5
[INF] Item ID set to 93202
[INF] -Parsing lem@371
[INF] Completed fragment 3.1-3.5 [1]: replacement: * (_Meditaris_ cantas, uel _melitaris_, -_l_- pro -_d_-, ut idem sit tropus.) lb1-56
```

The log starts with the input filename, and records each `div` and `app` element with its line number. Each app corresponds to a fragment; thus, the log first of all records its location, as derived from its attributes.

Also, given that this is the first part, a corresponding item to contain it is created and the part's item ID is set to its ID. Then, the log starts parsing the `app` element's content; here it just finds a `lem` element.

Finally, once all the content elements have been processed, providing a number of various entries in the fragment, the fragment itself is logged as completed with its location, count of entries, and entries summary.

If we look at the first part output in the dump JSON file, we find no such fragment. This is because it is a `margin-note` fragment, and as such it belongs to another layer, i.e. another part, which gets output after the first one.

In fact, immediately after the first part we find another one, whose role is `fr.net.fusisoft.apparatus:margin` (in Cadmus, layer parts have as role the type ID of their fragment model, followed by a colon plus a role ID when present).

If we look at the source text document, the base text portion is verse 2:

```xml
<l xml:id="d001l3" n="2">
    <w xml:id="d001w9">siluestrem</w>
    <w xml:id="d001w10">tenui</w>
    <w xml:id="d001w11">musam</w>
    <w xml:id="d001w12">meditaris</w>
    <w xml:id="d001w13">auena:</w>
</l>
```

The above apparatus refers to the whole verse, from `siluestrem` to `auena`. The corresponding tiled-text is a row (Y=3 i.e. the third line, because line 1 is the speaker's name) with 5 tiles, one for each word:

```json
{
    "y": 3,
    "tiles": [
    {
        "x": 1,
        "data": { "id": "d001w9", "text": "siluestrem" }
    },
    {
        "x": 2,
        "data": { "id": "d001w10", "text": "tenui" }
    },
    {
        "x": 3,
        "data": { "id": "d001w11", "text": "musam" }
    },
    {
        "x": 4,
        "data": { "id": "d001w12", "text": "meditaris" }
    },
    {
        "x": 5,
        "data": { "id": "d001w13", "text": "auena:" }
    }
    ],
    "data": { "_name": "l", "id": "d001l3", "n": "2"  }
}
```

Thus, we expect that the fragment location to be `3.1-3.5`, i.e. line 3, words from 1 to 5. In fact, the first fragment in the output part is:

```json
{
    "location": "3.1-3.5",
    "tag": "d001 margin-note",
    "entries": [
    {
        "type": 3,
        "tag": "ancient-note",
        "value": null,
        "normValue": null,
        "isAccepted": false,
        "groupId": null,
        "witnesses": [],
        "authors": [
        {
            "value": "lb1-56",
            "note": null
        }
        ],
        "note": "_Meditaris_ cantas, uel _melitaris_, -_l_- pro -_d_-, ut idem sit tropus."
    }
    ]
}
```

Here:

- the location is as expected.
- the tag is the combination of the `div1`'s ID (`d001`) and the `app`'s type (`margin-note`).
- the apparatus entries contain a single entry: its type is `3` (=note), as for any entry in the margin notes apparatus (unless they are provided with a value, which anyway I suppose should not happen).
- there are no witnesses, and a single author `lb1-56`.
- the note's text contains only section 1 (which corresponds to the `add` element with `@type`=`abstract`), and thus has no section separators. It is transformed so that the formatting is expressed with Markdown (text between underscores is italic). Thus, the original XML element:

```xml
<add type="abstract"><emph style="font-style:italic">Meditaris</emph> cantas, uel <emph style="font-style:italic">melitaris</emph>, -<emph style="font-style:italic">l</emph>- pro -<emph style="font-style:italic">d</emph>-, ut idem sit tropus.</add>
```

becomes:

```txt
_Meditaris_ cantas, uel _melitaris_, -_l_- pro -_d_-, ut idem sit tropus.
```
