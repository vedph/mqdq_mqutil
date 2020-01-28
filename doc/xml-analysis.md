# XML Documents Analysis

Currently this analysis relies on the MQDQ text files.

As a primary inspection, I have conducted two scans using software tools created by myself (they belong to a powerful conversion system codenamed *Proteus*). Their output is represented by:

- [characters](mqdq-txt-chars.tsv): a list of all the character codes occurring in the documents, grouped into two sets: one for elements and another for attributes. This lists only the character codes found either in attributes values or in elements children of type text or CDATA, recursively.
- [XML structure](mqdq-txt-report.html): a tree-shaped report representing the XML structure of all the MQDQ TEI text documents (currently 665). Here, for brevity I have replaced the full TEI namespace with nothing, and the full XML namespace with an `xml:` prefix.

In the XML structure report, the tree-shaped map of all the XML elements and their attributes (starting with `@`) and children elements is shown, as follows:

- the frequency is displayed between square brackets.
- the count of attribute types is displayed between square brackets, prefixed by `@`.
- the count of children text nodes is displayed between square brackets, prefixed by `T`.
- "leaf" elements, i.e. elements without any children elements, are marked by a left-pointing triangle. This triangle is empty for empty elements.
- each node is suffixed with its full XPath.
- you can click the `+` button to expand or collapse a branch.

## Observations On Text Structure

These are the relevant traits of TEI text documents (ignoring their headers):

- all poetical texts, ultimately structured into lines.
- fully rooted under `text/body/div1`.
- `div1` may contain as direct children:
  - `div2`: see below.
  - `head`: header, can include `abbr`, `bibl` (always with a `source` attribute), `title`, all "leaf" elements.
  - `l`: a line of text. This can include the text, or `w` children elements, one for each graphical word.
  - `lb` (empty).
  - `p`: this seems to be used (a) instead of `speaker`, to avoid introducing more nesting via its required `sp`eech parent element, or (b) for a non-metrical text. In the latter case, it may include `w`ords children elements (for the apparatus).

In turn, `div2` may include:

- `head`: header, can include `abbr`, `title`, all "leaf" elements.
- `l`: as above.
- `lb`: as above.
- `p`: as above.

**Attributes** are (`*` marks required attributes):

- `div1`:
  - `xml:id`*
  - `decls`
  - `met`: metre.
  - `type`*
- `div2`:
  - `xml:id`*
  - `met`: metre.
  - `type`*
- `l`:
  - `xml:id`*
  - `met`
  - `n`*: line number in the source text. This is not necessarily progressive.
  - `part`
  - `rend`
- `p`:
  - `xml:id`*
  - `met`
  - `n`*
- `w`:
  - `xml:id`*
  - `real`
- `bibl`:
  - `source`*
