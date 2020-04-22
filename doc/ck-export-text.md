# Checking Export Text

The text is exported into a copy of each original text document. Its header is left untouched; the contents of all its `div` elements (except the eventual initial `head` child, and other `div` children) are removed; then, new XML branches are injected into each `div`.

## Log

You should check this log for errors (just find the string `[ERR]` inside it). No error is expected. The log just lists the files processed, preceded by their total count.

## Checking

To check the results, the easiest way is just comparing documents with a diff tool.

For instance, I'm using the Diff tool which comes with Oxygen. In this case, I recommend to load both documents, and then under the `Compare` menu select `Format and Indent Both Files` before comparing. This removes unrelevant differences connected to formatting, allowing for a much clearer comparison.
